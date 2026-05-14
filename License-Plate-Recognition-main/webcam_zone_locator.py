#!/usr/bin/env python3
"""
Camera module for locating checked-in vehicles inside a parking zone.

Example:
    python webcam_zone_locator.py --ip 192.168.1.21:8080 --camera-id A_COL_1 --parking-lot A --zone A --column 1 --location-name "Bai A - Cot 1"
"""

import argparse
import json
import logging
import os
import signal
import sys
import threading
import time
from collections import deque
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"
os.environ["OPENCV_VIDEOIO_DEBUG"] = "0"

import cv2
import torch

import config
import function.helper as helper
import function.utils_rotate as utils_rotate
from service import ZoneLocatorService

logger = logging.getLogger("parking_service.zone_camera")
stop_requested = False
display_queue = deque(maxlen=1)  # (frame_count, frame_resized, fps, capture_ts)
ai_overlay_queue = deque(maxlen=1)  # (frame_count, results, ai_ts)
recent_event_lines = deque(maxlen=8)
latest_capture_frame_count = 0
latest_capture_ts = 0.0
api_server = None
STALE_FRAME_SECONDS = 2.0


def is_client_disconnect_error(exc):
    """Return True for normal browser/client disconnects from MJPEG streams."""
    if isinstance(exc, (BrokenPipeError, ConnectionResetError, ConnectionAbortedError)):
        return True
    if isinstance(exc, TimeoutError):
        return True
    if isinstance(exc, OSError):
        if getattr(exc, "winerror", None) in (10038, 10053, 10054, 10060):
            return True
        if getattr(exc, "errno", None) in (32, 54, 10038, 10053, 10054, 10060):
            return True
    return False


class QuietThreadingHTTPServer(ThreadingHTTPServer):
    def handle_error(self, request, client_address):
        _, exc, _ = sys.exc_info()
        if is_client_disconnect_error(exc):
            return
        super().handle_error(request, client_address)


def handle_signal(sig, frame):
    global stop_requested
    stop_requested = True


def parse_camera_source(args) -> str:
    if args.source:
        return args.source

    ip = args.ip
    port = args.port
    if ":" in ip:
        host, raw_port = ip.rsplit(":", 1)
        ip = host
        try:
            port = int(raw_port)
        except ValueError:
            port = args.port
    return f"http://{ip}:{port}/video"


def detect_plate_boxes(model, frame, max_detections: int):
    detections = model(frame, size=getattr(config, "PLATE_DETECTOR_IMGSZ", 640))
    try:
        det = detections.xyxy[0]
        boxes = det.detach().cpu().numpy().tolist() if det is not None else []
    except Exception:
        boxes = detections.pandas().xyxy[0].values.tolist()

    boxes.sort(key=lambda item: float(item[4]), reverse=True)
    if max_detections > 0:
        boxes = boxes[:max_detections]
    return boxes


def read_plate_from_crop(ocr_model, crop):
    for cc, ct in getattr(config, "OCR_DESKEW_COMBOS", [(0, 0), (1, 0)]):
        text = helper.read_plate(ocr_model, utils_rotate.deskew(crop, cc, ct))
        if text != "unknown":
            return text
    return "unknown"


def clamp_box(box, frame):
    x0 = max(0, int(box[0]))
    y0 = max(0, int(box[1]))
    x1 = min(frame.shape[1], int(box[2]))
    y1 = min(frame.shape[0], int(box[3]))
    if x1 <= x0 or y1 <= y0:
        return None
    return x0, y0, x1, y1


def build_observation_key(bbox, frame):
    x0, y0, x1, y1 = bbox
    height, width = frame.shape[:2]
    grid_x = max(1, width // 12)
    grid_y = max(1, height // 8)
    center_x = (x0 + x1) // 2
    center_y = (y0 + y1) // 2
    return f"cell-{center_x // grid_x}-{center_y // grid_y}"


def capture_thread(cap, resize_scale):
    global latest_capture_frame_count, latest_capture_ts

    logger.info("[Capture Thread] Started")
    frame_count = 0
    prev_time = 0.0

    while not stop_requested:
        ok, frame = cap.read()
        if not ok:
            time.sleep(0.01)
            continue

        if resize_scale and resize_scale != 1.0:
            frame = cv2.resize(frame, (None, None), fx=resize_scale, fy=resize_scale)

        frame_count += 1
        capture_ts = time.time()
        latest_capture_frame_count = frame_count
        latest_capture_ts = capture_ts

        fps = 1 / (capture_ts - prev_time) if prev_time > 0 else 0
        prev_time = capture_ts
        display_queue.append((frame_count, frame, fps, capture_ts))


def ai_thread(detector, ocr, service, args):
    logger.info("[AI Thread] Started")
    last_processed_frame = -1
    last_ocr_ts = 0.0

    while not stop_requested:
        time.sleep(0.03)
        if not service.is_scanning_active():
            if display_queue:
                frame_count, _, _, _ = display_queue[-1]
                ai_overlay_queue.append((frame_count, [], time.time()))
            continue

        if not display_queue:
            continue

        frame_count, frame, _, _ = display_queue[-1]
        if frame_count == last_processed_frame:
            continue
        last_processed_frame = frame_count
        now = time.time()

        do_ocr = (now - last_ocr_ts) >= getattr(config, "OCR_MIN_INTERVAL_ACTIVE", 0.15)
        if do_ocr:
            last_ocr_ts = now

        try:
            boxes = detect_plate_boxes(detector, frame, args.max_detections)
        except Exception as exc:
            logger.warning("Detection failed: %s", exc)
            ai_overlay_queue.append((frame_count, [], now))
            continue

        ocr_count = 0
        frame_results = []
        for box in boxes:
            bbox = clamp_box(box, frame)
            if bbox is None:
                continue

            x0, y0, x1, y1 = bbox
            confidence = float(box[4])
            crop = frame[y0:y1, x0:x1]
            plate = "unknown"
            result = {"action": "detected", "plate": plate, "confidence": confidence}

            if do_ocr and ocr_count < args.max_ocr:
                ocr_count += 1
                plate = read_plate_from_crop(ocr, crop)
                if plate != "unknown":
                    result = service.process_observation(
                        plate_number=plate,
                        confidence=confidence,
                        crop_image_array=crop,
                        full_frame_array=frame,
                        send_api=not args.no_send,
                        observation_key=build_observation_key(bbox, frame),
                    )
                    if result.get("action") in ("sent", "cooldown", "finalized", "locked", "reset"):
                        recent_event_lines.appendleft(
                            f"{result.get('plate')} - {result.get('action')} - {result.get('buffer_status', '')}"
                        )

            frame_results.append({
                "id": f"{frame_count}-{result.get('plate', plate)}-{result.get('action', 'detected')}-{now}",
                "plate": result.get("plate", plate),
                "confidence": float(result.get("confidence", confidence) or 0),
                "bbox": {
                    "x": int(x0),
                    "y": int(y0),
                    "width": int(x1 - x0),
                    "height": int(y1 - y0),
                },
                "action": result.get("action", "detected"),
                "bufferStatus": result.get("buffer_status", ""),
            })

        ai_overlay_queue.append((frame_count, frame_results, now))


def draw_result(frame, bbox, plate, action, confidence):
    x0, y0, x1, y1 = bbox
    color = (0, 180, 255)
    if action == "sent":
        color = (0, 220, 0)
    elif action in ("cooldown", "buffered"):
        color = (255, 180, 0)

    cv2.rectangle(frame, (x0, y0), (x1, y1), color, 2)
    label = f"{plate} {confidence:.2f} {action}"
    cv2.putText(
        frame,
        label,
        (x0, max(22, y0 - 8)),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.55,
        color,
        2,
    )


def build_waiting_frame(message):
    frame = cv2.UMat(360, 640, cv2.CV_8UC3).get()
    frame[:] = (18, 24, 38)
    cv2.putText(frame, "Smart Parking Zone Camera", (28, 145), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)
    cv2.putText(frame, message, (28, 195), cv2.FONT_HERSHEY_SIMPLEX, 0.65, (0, 255, 255), 2)
    cv2.putText(frame, "Waiting for Python zone locator frames", (28, 240), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (180, 190, 210), 1)
    return frame


def build_overlay_frame(frame, fps, capture_ts, location_name):
    display = frame.copy()
    latency_ms = int((time.time() - capture_ts) * 1000) if capture_ts else 0

    cv2.putText(display, location_name, (10, 28), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
    cv2.putText(display, f"FPS: {int(fps)}", (10, 56), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 255), 2)
    cv2.putText(display, f"Latency: {latency_ms}ms", (10, 82), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 0), 2)
    service = getattr(api_server, "zone_service", None) if api_server else None
    if service:
        status = service.get_scan_status()
        if status.get("scanning"):
            scan_text = f"Scanning: {int(status.get('scanRemainingSeconds', 0))}s | Locked: {len(status.get('lockedPlates', []))}"
            scan_color = (0, 255, 0)
        else:
            scan_text = f"Recognition paused: {int(status.get('pauseRemainingSeconds', 0))}s"
            scan_color = (0, 165, 255)
        cv2.putText(display, scan_text, (10, 108), cv2.FONT_HERSHEY_SIMPLEX, 0.58, scan_color, 2)

    if ai_overlay_queue:
        _, results, _ = ai_overlay_queue[-1]
    else:
        results = []

    for item in results:
        bbox = item.get("bbox", {})
        x0 = int(bbox.get("x", 0))
        y0 = int(bbox.get("y", 0))
        x1 = x0 + int(bbox.get("width", 0))
        y1 = y0 + int(bbox.get("height", 0))
        draw_result(
            display,
            (x0, y0, x1, y1),
            item.get("plate", "unknown"),
            item.get("action", "detected"),
            float(item.get("confidence", 0) or 0),
        )

    start_y = 138
    for idx, line in enumerate(list(recent_event_lines)[:5]):
        cv2.putText(display, line[:80], (10, start_y + idx * 24), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 255, 255), 2)

    return display


def latest_detection_payload():
    service = getattr(api_server, "zone_service", None) if api_server else None
    service_event = service.get_last_event() if service and hasattr(service, "get_last_event") else None
    scan_status = service.get_scan_status() if service and hasattr(service, "get_scan_status") else None
    locked_detections = service.get_locked_detections() if service and hasattr(service, "get_locked_detections") else []
    if not ai_overlay_queue:
        return {
            "results": [],
            "count": 0,
            "timestamp": None,
            "lastEvent": service_event,
            "scanStatus": scan_status,
            "lockedDetections": locked_detections,
        }

    _, latest_detection_results, latest_detection_ts = ai_overlay_queue[-1]
    return {
        "results": latest_detection_results,
        "count": len(latest_detection_results),
        "timestamp": latest_detection_ts,
        "scanStatus": scan_status,
        "lockedDetections": locked_detections,
        "lastEvent": next(
            (
                {
                    "id": item.get("id"),
                    "plate": item.get("plate"),
                    "confidence": item.get("confidence", 0),
                    "action": item.get("action", ""),
                    "success": item.get("action") == "sent",
                    "message": item.get("bufferStatus", ""),
                    "timestamp": latest_detection_ts,
                }
                for item in latest_detection_results
                if item.get("action") in ("sent", "finalized", "cooldown")
            ),
            service_event,
        ),
    }


def make_api_handler(jpeg_quality, stream_fps):
    frame_delay = 1 / max(1, stream_fps)

    class ZoneCameraApiHandler(BaseHTTPRequestHandler):
        protocol_version = "HTTP/1.1"

        def log_message(self, format, *args):
            return

        def send_cors_headers(self):
            self.send_header("Access-Control-Allow-Origin", "*")
            self.send_header("Access-Control-Allow-Methods", "GET, OPTIONS")
            self.send_header("Access-Control-Allow-Headers", "Content-Type")

        def send_json_response(self, status_code, payload):
            body = json.dumps(payload).encode("utf-8")
            try:
                self.send_response(status_code)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(body)))
                self.send_cors_headers()
                self.end_headers()
                self.wfile.write(body)
            except Exception as exc:
                if not is_client_disconnect_error(exc):
                    raise
                return

        def do_OPTIONS(self):
            try:
                self.send_response(204)
                self.send_cors_headers()
                self.send_header("Content-Length", "0")
                self.end_headers()
            except Exception as exc:
                if not is_client_disconnect_error(exc):
                    raise

        def do_GET(self):
            path = urlparse(self.path).path
            if path == "/api/health":
                frame_age_ms = int((time.time() - latest_capture_ts) * 1000) if latest_capture_ts else None
                self.send_json_response(200, {
                    "ok": True,
                    "frameAvailable": bool(display_queue) and (frame_age_ms is None or frame_age_ms <= int(STALE_FRAME_SECONDS * 1000)),
                    "frameCount": latest_capture_frame_count,
                    "lastFrameAgeMs": frame_age_ms,
                    "captureStale": frame_age_ms is not None and frame_age_ms > int(STALE_FRAME_SECONDS * 1000),
                })
                return

            if path == "/api/detection":
                self.send_json_response(200, latest_detection_payload())
                return

            if path == "/api/stream":
                try:
                    self.send_response(200)
                    self.send_header("Content-Type", "multipart/x-mixed-replace; boundary=frame")
                    self.send_header("Cache-Control", "no-cache, no-store, must-revalidate")
                    self.send_header("Pragma", "no-cache")
                    self.send_header("X-Accel-Buffering", "no")
                    self.send_cors_headers()
                    self.end_headers()
                except Exception as exc:
                    if not is_client_disconnect_error(exc):
                        raise
                    return

                while not stop_requested:
                    if not display_queue:
                        frame = build_waiting_frame("Waiting for camera frame...")
                    else:
                        frame_count, raw_frame, fps, capture_ts = display_queue[-1]
                        frame_age = time.time() - capture_ts if capture_ts else 0
                        if frame_age > STALE_FRAME_SECONDS:
                            frame = build_waiting_frame("Camera frame is stale. Check IP Webcam.")
                        else:
                            frame = build_overlay_frame(raw_frame, fps, capture_ts, getattr(self.server, "location_name", "Zone Camera"))

                    ok, encoded = cv2.imencode(".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), int(jpeg_quality)])
                    if not ok:
                        continue

                    jpg = encoded.tobytes()
                    try:
                        self.wfile.write(b"--frame\r\n")
                        self.wfile.write(b"Content-Type: image/jpeg\r\n")
                        self.wfile.write(f"Content-Length: {len(jpg)}\r\n\r\n".encode("ascii"))
                        self.wfile.write(jpg)
                        self.wfile.write(b"\r\n")
                        self.wfile.flush()
                        time.sleep(frame_delay)
                    except Exception as exc:
                        if not is_client_disconnect_error(exc):
                            raise
                        break
                return

            try:
                self.send_response(404)
                self.send_cors_headers()
                self.send_header("Content-Length", "0")
                self.end_headers()
            except Exception as exc:
                if not is_client_disconnect_error(exc):
                    raise

    return ZoneCameraApiHandler


def start_api_server(host, port, jpeg_quality, stream_fps, location_name, zone_service):
    global api_server
    api_server = QuietThreadingHTTPServer((host, port), make_api_handler(jpeg_quality, stream_fps))
    api_server.location_name = location_name
    api_server.zone_service = zone_service
    server_t = threading.Thread(target=api_server.serve_forever, daemon=True, name="ZoneCameraApiServer")
    server_t.start()
    logger.info("[API] Stream: http://%s:%s/api/stream", host, port)
    logger.info("[API] Detection: http://%s:%s/api/detection", host, port)
    logger.info("[API] Health: http://%s:%s/api/health", host, port)
    return api_server


def main():
    global stop_requested

    parser = argparse.ArgumentParser(description="SmartParking zone locator camera")
    parser.add_argument("--source", type=str, default="", help="Direct OpenCV source. Overrides --ip/--port.")
    parser.add_argument("-i", "--ip", type=str, default="192.168.1.20", help="IP Webcam host or host:port")
    parser.add_argument("--port", type=int, default=8080, help="IP Webcam port")
    parser.add_argument("-r", "--resize", type=float, default=config.CAMERA_RESIZE_SCALE)
    parser.add_argument("--camera-id", required=True)
    parser.add_argument("--parking-lot", default="")
    parser.add_argument("--zone", default="")
    parser.add_argument("--column", default="")
    parser.add_argument("--location-name", required=True)
    parser.add_argument("--max-detections", type=int, default=getattr(config, "ZONE_MAX_DETECTIONS_PER_FRAME", 10))
    parser.add_argument("--max-ocr", type=int, default=getattr(config, "ZONE_MAX_OCR_PLATES_PER_FRAME", 5))
    parser.add_argument("--no-send", action="store_true")
    parser.add_argument("--headless", action="store_true")
    parser.add_argument("--api-server", action="store_true")
    parser.add_argument("--api-host", type=str, default="0.0.0.0")
    parser.add_argument("--api-port", type=int, default=5101)
    parser.add_argument("--jpeg-quality", type=int, default=60)
    parser.add_argument("--stream-fps", type=int, default=8)
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format="%(message)s", handlers=[logging.StreamHandler(sys.stdout)])
    signal.signal(signal.SIGINT, handle_signal)

    service = ZoneLocatorService(
        camera_id=args.camera_id,
        location_name=args.location_name,
        parking_lot_code=args.parking_lot or None,
        zone_code=args.zone or None,
        column_code=args.column or None,
    )

    source = parse_camera_source(args)
    cap = cv2.VideoCapture(source)
    if not cap.isOpened():
        logger.error("Failed to open camera: %s", source)
        sys.exit(1)

    logger.info("Zone locator started: %s | %s", args.camera_id, args.location_name)
    capture_t = threading.Thread(
        target=capture_thread,
        args=(cap, args.resize),
        daemon=True,
        name="ZoneCaptureThread",
    )
    capture_t.start()

    if args.api_server:
        start_api_server(args.api_host, args.api_port, args.jpeg_quality, args.stream_fps, args.location_name, service)

    logger.info("Loading YOLO models...")
    detector = torch.hub.load("yolov5", "custom", path=config.PLATE_DETECTOR_MODEL, force_reload=True, source="local")
    ocr = torch.hub.load("yolov5", "custom", path=config.OCR_MODEL, force_reload=True, source="local")
    ocr.conf = config.OCR_CONFIDENCE
    logger.info("Models loaded")

    ai_t = threading.Thread(
        target=ai_thread,
        args=(detector, ocr, service, args),
        daemon=True,
        name="ZoneAIThread",
    )
    ai_t.start()

    try:
        while not stop_requested:
            if args.headless:
                time.sleep(0.2)
                continue

            if not display_queue:
                time.sleep(0.01)
                continue

            _, frame, fps, capture_ts = display_queue[-1]
            display = build_overlay_frame(frame, fps, capture_ts, args.location_name)
            cv2.imshow("SmartParking Zone Locator", display)
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                break
            if key == ord("s"):
                logger.info("Stats: %s", service.get_stats())
    finally:
        stop_requested = True
        cap.release()
        cv2.destroyAllWindows()
        if api_server:
            api_server.shutdown()
            api_server.server_close()
        capture_t.join(timeout=2)
        ai_t.join(timeout=2)
        logger.info("Zone locator stopped. Stats: %s", service.get_stats())


if __name__ == "__main__":
    main()
