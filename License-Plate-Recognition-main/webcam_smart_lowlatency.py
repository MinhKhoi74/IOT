#!/usr/bin/env python3
"""
Smart Parking AI Service - Optimized for Low-Latency Display
Multi-threaded: Camera capture (fast) + AI processing (background)

Hiển thị camera real-time với latency thấp
Xử lý AI ở background thread (không ảnh hưởng display)

Usage:
    # IPWebcam (default)
    python webcam_smart_lowlatency.py
    
    # IPWebcam with specific IP/Port
    python webcam_smart_lowlatency.py -i 192.168.1.20 --port 8080
    
    # IPWebcam with both IP and port in --ip argument
    python webcam_smart_lowlatency.py --ip 192.168.1.20:8080
    dotnet run --urls "http://localhost:5000"
"""

import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
os.environ['OPENCV_VIDEOIO_DEBUG'] = '0'
import warnings
warnings.filterwarnings('ignore')

import cv2
import torch
import argparse
import time
import signal
import sys
import logging
import threading
import json
from collections import deque
from datetime import datetime
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

torch.set_printoptions(sci_mode=False)
torch.cuda.empty_cache() if torch.cuda.is_available() else None

import function.utils_rotate as utils_rotate
import function.helper as helper
from service import SmartParkingService
import config

# =====================
# Global Variables
# =====================
parking_service: SmartParkingService = None
logger = logging.getLogger(__name__)

# Frame queue for display + AI overlay (latest only)
display_queue = deque(maxlen=1)  # (frame_count, frame_resized, fps, capture_ts)
ai_overlay_queue = deque(maxlen=1)  # (frame_count, results, ai_ts)

stop_event = threading.Event()
api_server = None
latest_capture_frame_count = 0
latest_capture_ts = 0.0
last_vehicle_event = None


def signal_handler(sig, frame):
    """Handle Ctrl+C."""
    logger.info("\n[INTERRUPT] Shutting down...")
    stop_event.set()
    if parking_service:
        parking_service.shutdown()
    cv2.destroyAllWindows()
    time.sleep(0.5)
    sys.exit(0)


# =====================
# Thread 1: Frame Capture (Low-Latency)
# =====================

def capture_thread(vid, resize_scale):
    """
    Capture frames from camera with low latency.
    Priority: Hiển thị frame nhanh nhất!
    """
    global latest_capture_frame_count, latest_capture_ts

    logger.info("[Capture Thread] Started")
    frame_count = 0
    prev_time = 0
    
    while not stop_event.is_set():
        ret, frame = vid.read()
        
        if not ret:
            logger.warning("[Capture] Failed to read frame")
            time.sleep(0.01)
            continue
        
        frame_count += 1
        capture_ts = time.time()
        latest_capture_frame_count = frame_count
        latest_capture_ts = capture_ts
        
        # === Resize ===
        frame_resized = cv2.resize(frame, (None, None),
                                  fx=resize_scale, fy=resize_scale)
        
        # === Calculate FPS ===
        current_time = time.time()
        fps = 1 / (current_time - prev_time) if prev_time > 0 else 0
        prev_time = current_time
        
        # === Put to queue (for display) ===
        # Ghi chú: Vẽ sẽ được thực hiện ở main display loop, không ở đây
        display_queue.append((frame_count, frame_resized, fps, capture_ts))


# =====================
# Thread 2: AI Processing (Background)
# =====================

def ai_thread(yolo_LP_detect, yolo_license_plate, no_send):
    """
    Process AI plate detection in background.
    Priority: Xử lý có thể chậm, không ảnh hưởng hiển thị!
    """
    global last_vehicle_event

    logger.info("[AI Thread] Started")
    last_processed_frame = -1
    last_logged_plate = None
    ocr_interval_active = getattr(config, 'OCR_MIN_INTERVAL_ACTIVE', 0.15)
    ocr_interval_locked = getattr(config, 'OCR_MIN_INTERVAL_LOCKED', 1.0)
    plate_absence_reset_seconds = getattr(config, 'PLATE_ABSENCE_RESET_SECONDS', 1.2)
    max_detections = getattr(config, 'MAX_DETECTIONS_PER_FRAME', 5)
    max_ocr_active = getattr(config, 'MAX_OCR_PLATES_PER_FRAME_ACTIVE', 2)
    max_ocr_locked = getattr(config, 'MAX_OCR_PLATES_PER_FRAME_LOCKED', 1)
    detector_imgsz = getattr(config, 'PLATE_DETECTOR_IMGSZ', 640)
    ocr_deskew_combos = getattr(config, 'OCR_DESKEW_COMBOS', [(0, 0), (0, 1), (1, 0), (1, 1)])
    last_ocr_ts = 0.0
    last_plate_seen_ts = time.time()
    
    while not stop_event.is_set():
        # Loop through recent frames
        # (có thể mất thời gian nên dùng copy)
        time.sleep(0.05)  # Không process quá nhanh
        
        try:
            # Detect plates ===
            # Lấy frame mới nhất từ display_queue
            if not display_queue:
                continue
            
            frame_count, frame_resized, _, _ = display_queue[-1]
            if frame_count == last_processed_frame:
                continue
            last_processed_frame = frame_count
            
            # === Detect Plates ===
            plates = yolo_LP_detect(frame_resized, size=detector_imgsz)
            try:
                det = plates.xyxy[0]
                list_plates = det.detach().cpu().numpy().tolist() if det is not None else []
            except Exception:
                # Fallback (slower)
                list_plates = plates.pandas().xyxy[0].values.tolist()
            
            now_ts = time.time()
            if list_plates:
                last_plate_seen_ts = now_ts
            else:
                # Nếu đã chốt xong và biển số biến mất khỏi khung hình 1 thời gian -> bật scan lại
                if (now_ts - last_plate_seen_ts) >= plate_absence_reset_seconds:
                    if getattr(parking_service, 'scanning_enabled', True):
                        parking_service.voting_buffer.clear()
                    else:
                        parking_service.scanning_enabled = True
                        parking_service.voting_buffer.clear()
                ai_overlay_queue.append((frame_count, [], now_ts))
                continue
            
            # Sort by confidence (highest first)
            try:
                list_plates.sort(key=lambda p: float(p[4]), reverse=True)
            except Exception:
                pass
            
            if max_detections and max_detections > 0:
                list_plates = list_plates[:max_detections]
            
            scanning_enabled = bool(getattr(parking_service, 'scanning_enabled', True))
            ocr_interval = ocr_interval_active if scanning_enabled else ocr_interval_locked
            do_ocr_now = (now_ts - last_ocr_ts) >= ocr_interval
            if do_ocr_now:
                last_ocr_ts = now_ts
            
            max_ocr_per_frame = max_ocr_active if scanning_enabled else max_ocr_locked
            ocr_done = 0
            
            results = []
            
            # === Process Each Plate ===
            for _, plate in enumerate(list_plates):
                try:
                    x = int(plate[0])
                    y = int(plate[1])
                    w = int(plate[2] - plate[0])
                    h = int(plate[3] - plate[1])
                    plate_confidence = float(plate[4])

                    # Clamp bbox to frame bounds (avoid empty crops)
                    x0 = max(0, x)
                    y0 = max(0, y)
                    x1 = min(frame_resized.shape[1], x + w)
                    y1 = min(frame_resized.shape[0], y + h)
                    if x1 <= x0 or y1 <= y0:
                        continue
                    x, y = x0, y0
                    w, h = x1 - x0, y1 - y0
                    crop_img = frame_resized[y0:y1, x0:x1]
                    
                    # === Recognize Plate ===
                    lp = "unknown"
                    api_result = {
                        'action': 'detected' if scanning_enabled else 'skipped',
                        'plate': 'unknown',
                        'confidence': plate_confidence,
                        'api_sent': False,
                        'api_response': {},
                        'buffer_status': f"LOCKED ({parking_service.last_finalized_plate})" if not scanning_enabled else ''
                    }
                    
                    allow_ocr = do_ocr_now and (ocr_done < max_ocr_per_frame)
                    if allow_ocr:
                        ocr_done += 1
                        for cc, ct in ocr_deskew_combos:
                                lp_try = helper.read_plate(
                                    yolo_license_plate,
                                    utils_rotate.deskew(crop_img, cc, ct)
                                )
                                if lp_try != "unknown":
                                    lp = lp_try
                                    should_log = True
                                    if last_logged_plate:
                                        try:
                                            should_log = not parking_service.voting_buffer._is_same_vehicle(
                                                lp,
                                                last_logged_plate,
                                                threshold=config.FUZZY_MATCH_THRESHOLD
                                            )
                                        except Exception:
                                            should_log = (lp != last_logged_plate)
                                    if should_log:
                                        logger.info(f"[AI] Detected: {lp} ({plate_confidence:.2f})")
                                        last_logged_plate = lp

                                    # Only process if scanning enabled OR plate changed vs last finalized
                                    if not scanning_enabled and parking_service.last_finalized_plate:
                                        is_same = parking_service.voting_buffer._is_same_vehicle(
                                            lp,
                                            parking_service.last_finalized_plate,
                                            threshold=config.FUZZY_MATCH_THRESHOLD
                                        )
                                        if is_same:
                                            api_result['action'] = 'skipped'
                                            api_result['buffer_status'] = f"LOCKED ({parking_service.last_finalized_plate})"
                                        else:
                                            api_result = parking_service.process_frame(
                                                plate_number=lp,
                                                confidence=plate_confidence,
                                                crop_image_array=frame_resized.copy(),
                                                frame_index=frame_count,
                                                send_api=(not no_send)
                                            )
                                    else:
                                        api_result = parking_service.process_frame(
                                            plate_number=lp,
                                            confidence=plate_confidence,
                                            crop_image_array=frame_resized.copy(),
                                            frame_index=frame_count,
                                            send_api=(not no_send)
                                        )
                                    break
                        # end deskew combos
                    else:
                        if scanning_enabled:
                            api_result['action'] = 'throttled'
                            api_result['buffer_status'] = 'OCR throttled'
                    
                    results.append({
                        'bbox': (x, y, w, h),
                        'plate': lp,
                        'confidence': plate_confidence,
                        'api_result': api_result
                    })

                    action = api_result.get('action', '')
                    event_plate = api_result.get('plate') or lp
                    event_confidence = float(api_result.get('confidence', plate_confidence) or 0)
                    if event_plate != "unknown" and action in ('sent', 'finalized', 'skipped', 'canceled'):
                        event_label = 'Checkout' if parking_service.station_id == config.STATION_EXIT else 'Check-in'
                        last_vehicle_event = {
                            'id': f"{frame_count}-{event_plate}-{action}-{time.time()}",
                            'plate': event_plate,
                            'confidence': event_confidence,
                            'action': action,
                            'success': action in ('sent', 'finalized'),
                            'message': api_result.get('buffer_status', '')
                            or (f'{event_label} recorded' if action in ('sent', 'finalized') else f'{event_label} not recorded: {action}'),
                            'timestamp': time.time(),
                        }
                
                except Exception as e:
                    logger.error(f"[AI] Error: {str(e)}")
                    continue
            
            # === Store results for display thread ===
            ai_overlay_queue.append((frame_count, results, time.time()))
        
        except Exception as e:
            logger.error(f"[AI Thread] Error: {str(e)}")
            continue


def build_overlay_frame(frame_resized, fps, capture_ts, station):
    """Return the latest frame with FPS, latency, station, and AI overlays."""
    frame_for_display = frame_resized.copy()

    cv2.putText(frame_for_display, f"FPS: {int(fps)}",
               (7, 30), cv2.FONT_HERSHEY_SIMPLEX,
               0.7, (0, 255, 255), 2)
    cv2.putText(frame_for_display, "[Live]",
               (7, 60), cv2.FONT_HERSHEY_SIMPLEX,
               0.7, (0, 255, 0), 2)

    latency_ms = (time.time() - capture_ts) * 1000 if capture_ts else 0
    cv2.putText(frame_for_display, f"Latency: {int(latency_ms)}ms",
               (7, 90), cv2.FONT_HERSHEY_SIMPLEX,
               0.7, (255, 255, 0), 2)
    cv2.putText(frame_for_display, f"Station: {station.upper()}",
               (7, 120), cv2.FONT_HERSHEY_SIMPLEX,
               0.7, (0, 255, 255), 2)

    if ai_overlay_queue:
        _, ai_results, _ = ai_overlay_queue[-1]
    else:
        ai_results = []

    for result in ai_results:
        x, y, w, h = result['bbox']
        lp = result['plate']
        confidence = result['confidence']
        api_result = result.get('api_result', {})

        action = api_result.get('action', 'detected')
        cv2.rectangle(frame_for_display, (x, y), (x+w, y+h),
                     color=(0, 0, 225), thickness=2)

        if lp and lp != "unknown":
            cv2.putText(frame_for_display, lp,
                       (x, y + h + 20),
                       cv2.FONT_HERSHEY_SIMPLEX,
                       0.8, (0, 255, 0), 2)

        buffer_status = api_result.get('buffer_status', '')
        if action == 'detected':
            continue
        if action == 'sent':
            status_text = "SENT"
            status_color = (36, 255, 12)
        elif action == 'finalized':
            status_text = "OK"
            status_color = (0, 165, 255)
        elif action == 'buffered':
            status_text = f"[{buffer_status}]" if buffer_status else "[BUFFERING]"
            status_color = (255, 200, 0)
        elif action == 'canceled':
            status_text = buffer_status or "X CANCELED"
            status_color = (0, 0, 255)
        elif action == 'cooldown':
            status_text = "COOLDOWN"
            status_color = (0, 0, 255)
        elif action == 'skipped':
            status_text = buffer_status or "SKIP"
            status_color = (128, 128, 128)
        elif action == 'throttled':
            status_text = buffer_status or "[OCR]"
            status_color = (255, 200, 0)
        else:
            status_text = f"[{action}]"
            status_color = (255, 200, 0)

        cv2.putText(frame_for_display, status_text,
                   (x, max(20, y - 10)),
                   cv2.FONT_HERSHEY_SIMPLEX,
                   0.7, status_color, 2)

    return frame_for_display


def latest_detection_payload():
    """Return JSON-safe detection data for the frontend polling endpoint."""
    if not ai_overlay_queue:
        return {'results': [], 'count': 0, 'lastEvent': last_vehicle_event}

    _, ai_results, ai_ts = ai_overlay_queue[-1]
    results = []
    for result in ai_results:
        x, y, w, h = result.get('bbox', (0, 0, 0, 0))
        api_result = result.get('api_result', {})
        results.append({
            'plate': result.get('plate', 'unknown'),
            'confidence': float(result.get('confidence', 0)),
            'bbox': {
                'x': int(x),
                'y': int(y),
                'width': int(w),
                'height': int(h),
            },
            'action': api_result.get('action', 'detected'),
            'bufferStatus': api_result.get('buffer_status', ''),
        })

    return {
        'results': results,
        'count': len(results),
        'timestamp': ai_ts,
        'lastEvent': last_vehicle_event,
    }


def build_waiting_frame(message):
    frame = cv2.UMat(360, 640, cv2.CV_8UC3).get()
    frame[:] = (18, 24, 38)
    cv2.putText(frame, "Smart Parking Camera",
               (28, 150), cv2.FONT_HERSHEY_SIMPLEX,
               0.9, (255, 255, 255), 2)
    cv2.putText(frame, message,
               (28, 200), cv2.FONT_HERSHEY_SIMPLEX,
               0.7, (0, 255, 255), 2)
    cv2.putText(frame, "Check IP Webcam and backend logs",
               (28, 245), cv2.FONT_HERSHEY_SIMPLEX,
               0.6, (180, 190, 210), 1)
    return frame


def make_api_handler(station, jpeg_quality, stream_fps):
    frame_delay = 1 / max(1, stream_fps)

    class CameraApiHandler(BaseHTTPRequestHandler):
        protocol_version = 'HTTP/1.1'

        def log_message(self, format, *args):
            return

        def send_cors_headers(self):
            self.send_header('Access-Control-Allow-Origin', '*')
            self.send_header('Access-Control-Allow-Methods', 'GET, OPTIONS')
            self.send_header('Access-Control-Allow-Headers', 'Content-Type')

        def send_json_response(self, status_code, payload):
            body = json.dumps(payload).encode('utf-8')
            self.send_response(status_code)
            self.send_header('Content-Type', 'application/json')
            self.send_header('Content-Length', str(len(body)))
            self.send_cors_headers()
            self.end_headers()
            self.wfile.write(body)

        def do_OPTIONS(self):
            self.send_response(204)
            self.send_cors_headers()
            self.send_header('Content-Length', '0')
            self.end_headers()

        def do_GET(self):
            path = urlparse(self.path).path
            if path == '/api/health':
                self.send_json_response(200, {
                    'ok': True,
                    'frameAvailable': bool(display_queue),
                    'frameCount': latest_capture_frame_count,
                    'lastFrameAgeMs': int((time.time() - latest_capture_ts) * 1000)
                    if latest_capture_ts else None,
                })
                return

            if path == '/api/detection':
                self.send_json_response(200, latest_detection_payload())
                return

            if path == '/api/stream':
                self.send_response(200)
                self.send_header('Content-Type', 'multipart/x-mixed-replace; boundary=frame')
                self.send_header('Cache-Control', 'no-cache, no-store, must-revalidate')
                self.send_header('Pragma', 'no-cache')
                self.send_header('X-Accel-Buffering', 'no')
                self.send_cors_headers()
                self.end_headers()

                last_streamed_frame = -1
                while not stop_event.is_set():
                    if not display_queue:
                        frame = build_waiting_frame("Waiting for camera frame...")
                    else:
                        frame_count, frame_resized, fps, capture_ts = display_queue[-1]
                        if frame_count == last_streamed_frame:
                            time.sleep(frame_delay)
                            continue
                        last_streamed_frame = frame_count
                        frame = build_overlay_frame(frame_resized, fps, capture_ts, station)

                    ok, encoded = cv2.imencode(
                        '.jpg',
                        frame,
                        [int(cv2.IMWRITE_JPEG_QUALITY), int(jpeg_quality)]
                    )
                    if not ok:
                        continue

                    jpg = encoded.tobytes()
                    try:
                        self.wfile.write(b'--frame\r\n')
                        self.wfile.write(b'Content-Type: image/jpeg\r\n')
                        self.wfile.write(f'Content-Length: {len(jpg)}\r\n\r\n'.encode('ascii'))
                        self.wfile.write(jpg)
                        self.wfile.write(b'\r\n')
                        self.wfile.flush()
                        time.sleep(frame_delay)
                    except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
                        break
                return

            self.send_response(404)
            self.send_cors_headers()
            self.send_header('Content-Length', '0')
            self.end_headers()

    return CameraApiHandler


def start_api_server(host, port, station, jpeg_quality, stream_fps):
    global api_server
    handler = make_api_handler(station, jpeg_quality, stream_fps)
    api_server = ThreadingHTTPServer((host, port), handler)
    server_t = threading.Thread(
        target=api_server.serve_forever,
        daemon=True,
        name="CameraApiServer"
    )
    server_t.start()
    logger.info(f"[API] Stream: http://{host}:{port}/api/stream")
    logger.info(f"[API] Detection: http://{host}:{port}/api/detection")
    logger.info(f"[API] Health: http://{host}:{port}/api/health")
    return api_server


# =====================
# Main Display Loop
# =====================

def main():
    """Main entry point."""
    global parking_service, logger
    
    # =====================
    # Arguments
    # =====================
    ap = argparse.ArgumentParser(
        description='Smart Parking AI - Low Latency Display'
    )
    
    # === WiFi Options (ipwebcam) ===
    ap.add_argument('-p', '--phone', action='store_true',
                   help='Use ipwebcam from phone')
    ap.add_argument('-i', '--ip', type=str, default='192.168.1.20',
                   help='Phone IP address')
    ap.add_argument('--port', type=int, default=8080,
                   help='Phone camera port')
    
    # === Display & Processing ===
    ap.add_argument('-s', '--scale', type=float, default=0.5,
                   help='Display scale (0-1)')
    ap.add_argument('-r', '--resize', type=float, default=config.CAMERA_RESIZE_SCALE,
                    help='Processing scale (0.2-0.4)')
    ap.add_argument('--station', type=str, choices=['entrance', 'exit'],
                   default='entrance', help='Station type')
    ap.add_argument('--simulate', action='store_true',
                   help='Simulate API')
    ap.add_argument('--no-send', action='store_true',
                   help='Do not send API')
    ap.add_argument('--api-server', action='store_true',
                   help='Expose MJPEG stream and detection JSON endpoints')
    ap.add_argument('--api-host', type=str, default='0.0.0.0',
                   help='API server host')
    ap.add_argument('--api-port', type=int, default=5001,
                   help='API server port')
    ap.add_argument('--headless', action='store_true',
                   help='Run without OpenCV display window')
    ap.add_argument('--jpeg-quality', type=int, default=60,
                   help='MJPEG JPEG quality (lower is faster)')
    ap.add_argument('--stream-fps', type=int, default=8,
                   help='MJPEG stream FPS for browser')
    
    args = ap.parse_args()
    
    # =====================
    # Setup logging
    # =====================
    # Clean any existing handlers from root logger to prevent duplicates
    root_logger = logging.getLogger()
    for handler in root_logger.handlers[:]:
        root_logger.removeHandler(handler)
    
    logging.basicConfig(
        level=logging.WARNING,  # Only show warnings+ from root
        format='%(message)s',
        handlers=[logging.StreamHandler()]
    )

    
    # === Handle --phone (backward compatibility) ===
    if args.phone:
        args.camera = 'wifi'
        logger.info("[DEPRECATED] Use --camera wifi instead of --phone")
    
    # =====================
    # Config
    # =====================
    if args.simulate:
        config.SIMULATE_API = True
    
    station_id = config.STATION_ENTRANCE if args.station == 'entrance' else config.STATION_EXIT
    
    # =====================
    # Initialize Service
    # =====================
    parking_service = SmartParkingService(station_id=station_id)
    logger = logging.getLogger(__name__)
    logger.setLevel(logging.INFO)

    # Health check (match webcam_smart behavior)
    if not args.no_send and not args.simulate:
        logger.info("Checking backend connection...")
        logger.info(f"Backend URL: {config.BACKEND_API_URL}")
        is_healthy = parking_service.health_check()
        if not is_healthy:
            logger.warning("")
            logger.warning("TROUBLESHOOTING:")
            logger.warning("1. Ensure backend is running: dotnet run --project SmartParkingSystem")
            logger.warning("2. Check backend is listening on port 5000")
            logger.warning("3. Try: http://localhost:5000/api/health")
            logger.warning("")
            logger.warning("For now, continuing with simulated responses...")
            logger.warning("")
    
    # =====================
    # Load Models
    # =====================
    logger.info("Loading YOLO models...")
    try:
        yolo_LP_detect = torch.hub.load('yolov5', 'custom',
                                        path=config.PLATE_DETECTOR_MODEL,
                                        force_reload=True, source='local')
        yolo_license_plate = torch.hub.load('yolov5', 'custom',
                                           path=config.OCR_MODEL,
                                           force_reload=True, source='local')
        yolo_license_plate.conf = config.OCR_CONFIDENCE
        logger.info("✓ Models loaded")
    except Exception as e:
        logger.error(f"Failed to load models: {str(e)}")
        sys.exit(1)
    
    # =====================
    # Camera
    # =====================
    logger.info("Initializing camera...")
    
    # === Camera URL (ipwebcam) ===
    # Parse IP + Port (support both --ip 192.168.1.20 --port 8080 and --ip 192.168.1.20:8080)
    ip = args.ip
    port = args.port
    
    if ':' in args.ip:
        parts = args.ip.rsplit(':', 1)
        ip = parts[0]
        try:
            port = int(parts[1])
        except:
            port = args.port
    
    camera_source = f'http://{ip}:{port}/video'
    logger.info(f"[IPWebcam] {camera_source}")
    
    # === Open camera ===
    vid = cv2.VideoCapture(camera_source)
    if not vid.isOpened():
        logger.error(f"Failed to open camera: {camera_source}")
        sys.exit(1)
    
    logger.info("✓ Camera ready")
    
    # =====================
    # Signal Handler
    # =====================
    signal.signal(signal.SIGINT, signal_handler)
    
    # =====================
    # Start Threads
    # =====================
    logger.info("Starting threads...")
    
    # Capture thread (priority: display)
    capture_t = threading.Thread(
        target=capture_thread,
        args=(vid, args.resize),
        daemon=True,
        name="CaptureThread"
    )
    
    # AI thread (priority: background)
    ai_t = threading.Thread(
        target=ai_thread,
        args=(yolo_LP_detect, yolo_license_plate, args.no_send),
        daemon=True,
        name="AIThread"
    )
    
    capture_t.start()
    ai_t.start()

    if args.api_server:
        start_api_server(
            args.api_host,
            args.api_port,
            args.station,
            args.jpeg_quality,
            args.stream_fps
        )
    
    logger.info("=" * 70)
    if args.headless:
        logger.info("Headless mode enabled. Press Ctrl+C to quit.")
    else:
        logger.info("Press 'q' to quit, 's' for stats")
    logger.info("=" * 70)
    
    # =====================
    # Main Display Loop (Low-Latency!)
    # =====================
    try:
        while not stop_event.is_set():
            if args.headless:
                time.sleep(0.2)
                continue

            # Get latest frame
            if not display_queue:
                time.sleep(0.01)
                continue
            
            frame_count, frame_resized, fps, capture_ts = display_queue[-1]
            frame_for_display = build_overlay_frame(
                frame_resized,
                fps,
                capture_ts,
                args.station
            )
            
            # === Draw FPS ===
            cv2.putText(frame_for_display, f"FPS: {int(fps)}",
                       (7, 30), cv2.FONT_HERSHEY_SIMPLEX,
                       0.7, (0, 255, 255), 2)
            cv2.putText(frame_for_display, "[Live]",
                       (7, 60), cv2.FONT_HERSHEY_SIMPLEX,
                       0.7, (0, 255, 0), 2)
            
            # === Draw latency (capture -> display) ===
            latency_ms = (time.time() - capture_ts) * 1000 if capture_ts else 0
            cv2.putText(frame_for_display, f"Latency: {int(latency_ms)}ms",
                       (7, 90), cv2.FONT_HERSHEY_SIMPLEX,
                       0.7, (255, 255, 0), 2)
            cv2.putText(frame_for_display, f"Station: {args.station.upper()}",
                       (7, 120), cv2.FONT_HERSHEY_SIMPLEX,
                       0.7, (0, 255, 255), 2)
            
            # === Draw AI results from queue (if available) ===
            if ai_overlay_queue:
                _, ai_results, _ = ai_overlay_queue[-1]
            else:
                ai_results = []
            for result in ai_results:
                x, y, w, h = result['bbox']
                lp = result['plate']
                confidence = result['confidence']
                api_result = result.get('api_result', {})
                
                # Draw rectangle (match webcam_smart style)
                action = api_result.get('action', 'detected')
                cv2.rectangle(frame_for_display, (x, y), (x+w, y+h),
                             color=(0, 0, 225), thickness=2)
                
                # Draw plate (below bbox)
                if lp and lp != "unknown":
                    cv2.putText(frame_for_display, lp,
                               (x, y + h + 20),
                               cv2.FONT_HERSHEY_SIMPLEX,
                               0.8, (0, 255, 0), 2)
                
                # Draw status
                buffer_status = api_result.get('buffer_status', '')
                if action == 'detected':
                    continue
                if action == 'sent':
                    status_text = "✓ SENT"
                    status_color = (36, 255, 12)  # Green
                elif action == 'finalized':
                    status_text = "✓ OK"
                    status_color = (0, 165, 255)  # Orange
                elif action == 'buffered':
                    status_text = f"[{buffer_status}]" if buffer_status else "[BUFFERING]"
                    status_color = (255, 200, 0)  # Cyan
                elif action == 'canceled':
                    status_text = buffer_status or "X CANCELED"
                    status_color = (0, 0, 255)  # Red
                elif action == 'cooldown':
                    status_text = "⏳ COOLDOWN"
                    status_color = (0, 0, 255)  # Red
                elif action == 'skipped':
                    status_text = buffer_status or "✖ SKIP"
                    status_color = (128, 128, 128)  # Gray
                elif action == 'throttled':
                    status_text = buffer_status or "[OCR]"
                    status_color = (255, 200, 0)  # Cyan
                else:
                    status_text = f"[{action}]"
                    status_color = (255, 200, 0)  # Cyan
                
                cv2.putText(frame_for_display, status_text,
                           (x, max(20, y - 10)),
                           cv2.FONT_HERSHEY_SIMPLEX,
                           0.7, status_color, 2)
            
            # === Display (scale down) ===
            frame_display = cv2.resize(frame_for_display, (None, None),
                                      fx=args.scale, fy=args.scale)
            
            cv2.imshow(
                f'Smart Parking - Low Latency [Capture: fast, AI: background]',
                frame_display
            )
            
            # === Keys ===
            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                logger.info("Quit by user")
                break
            elif key == ord('s'):
                parking_service.log_stats()
    
    except KeyboardInterrupt:
        logger.info("Interrupted")
    
    finally:
        # =====================
        # Cleanup
        # =====================
        logger.info("Cleaning up...")
        stop_event.set()
        
        vid.release()
        cv2.destroyAllWindows()

        if api_server:
            api_server.shutdown()
            api_server.server_close()
        
        if parking_service:
            parking_service.shutdown()
        
        # Wait for threads
        capture_t.join(timeout=2)
        ai_t.join(timeout=2)
        
        logger.info("=" * 70)
        logger.info("Application terminated")
        logger.info("=" * 70)


if __name__ == '__main__':
    main()
