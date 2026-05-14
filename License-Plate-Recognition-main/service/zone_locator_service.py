"""
Voting/cooldown service for cameras inside parking zones.
Unlike the barrier service, this tracks multiple plates independently.
"""

import logging
import threading
import time
from datetime import datetime
from typing import Any, Dict, Optional

import config
from service.voting_buffer import PlateVotingBuffer
from service.zone_location_client import ZoneLocationAPIClient

logger = logging.getLogger("parking_service.zone_locator")


class ZoneLocatorService:
    def __init__(
        self,
        camera_id: str,
        location_name: str,
        parking_lot_code: Optional[str] = None,
        zone_code: Optional[str] = None,
        column_code: Optional[str] = None,
    ):
        self.camera_id = camera_id
        self.location_name = location_name
        self.parking_lot_code = parking_lot_code
        self.zone_code = zone_code
        self.column_code = column_code
        self.api_client = ZoneLocationAPIClient()
        self.buffers: Dict[str, PlateVotingBuffer] = {}
        self.last_sent_at: Dict[str, float] = {}
        self.lock = threading.RLock()
        self.cooldown_seconds = float(getattr(config, "ZONE_DETECTION_COOLDOWN_SECONDS", 45))
        self.buffer_size = int(getattr(config, "ZONE_PLATE_BUFFER_SIZE", 5))
        self.min_occurrences = int(getattr(config, "ZONE_PLATE_VOTE_MIN_OCCURRENCES", 3))
        self.scan_window_seconds = float(getattr(config, "ZONE_SCAN_WINDOW_SECONDS", 10))
        self.scan_pause_seconds = float(getattr(config, "ZONE_SCAN_PAUSE_SECONDS", 180))
        self.scan_started_at = time.time()
        self.pause_until = 0.0
        self.locked_plates: set[str] = set()
        self.pending_batch: Dict[str, Dict[str, Any]] = {}
        self.batch_status = "collecting"
        self.last_event: Optional[Dict[str, Any]] = None
        self.stats = {
            "observations": 0,
            "finalized": 0,
            "sent": 0,
            "batch_sent": 0,
            "cooldown_skipped": 0,
            "api_failed": 0,
        }

    def is_scanning_active(self) -> bool:
        with self.lock:
            self._advance_cycle_locked()
            return time.time() >= self.pause_until

    def get_scan_status(self) -> Dict[str, Any]:
        with self.lock:
            self._advance_cycle_locked()
            now = time.time()
            scanning = now >= self.pause_until
            return {
                "scanning": scanning,
                "scanWindowSeconds": self.scan_window_seconds,
                "scanRemainingSeconds": max(0, self.scan_window_seconds - (now - self.scan_started_at)) if scanning else 0,
                "pauseSeconds": self.scan_pause_seconds,
                "pauseRemainingSeconds": max(0, self.pause_until - now),
                "lockedPlates": sorted(self.locked_plates),
                "pendingCount": len(self.pending_batch),
                "batchStatus": self.batch_status,
            }

    def get_locked_detections(self) -> list[Dict[str, Any]]:
        with self.lock:
            return [
                {
                    "plateNumber": plate,
                    "confidence": item.get("confidence", 0),
                    "detectedAt": item.get("detectedAt"),
                    "status": self.batch_status if self.batch_status in ("sending", "sent", "failed") else "locked",
                }
                for plate, item in sorted(self.pending_batch.items())
            ]

    def process_observation(
        self,
        plate_number: str,
        confidence: float,
        crop_image_array,
        full_frame_array=None,
        send_api: bool = True,
        observation_key: Optional[str] = None,
    ) -> Dict[str, Any]:
        normalized = PlateVotingBuffer._normalize_plate(plate_number)
        result: Dict[str, Any] = {
            "action": "buffered",
            "plate": normalized,
            "confidence": confidence,
            "buffer_status": "",
            "api_sent": False,
            "api_response": {},
        }

        if not normalized or normalized == "UNKNOWN":
            result["action"] = "ignored"
            result["buffer_status"] = "unknown"
            return result

        with self.lock:
            self._advance_cycle_locked()
            if time.time() < self.pause_until:
                result["action"] = "paused"
                result["buffer_status"] = f"scan pause {self.pause_until - time.time():.0f}s"
                return result

            self.stats["observations"] += 1
            if normalized in self.locked_plates:
                result["action"] = "locked"
                result["buffer_status"] = "locked in current scan"
                return result

            buffer_key = observation_key or normalized
            buffer = self.buffers.get(buffer_key)
            if buffer is None:
                buffer = PlateVotingBuffer(buffer_size=self.buffer_size)
                self.buffers[buffer_key] = buffer

            buffer.add_plate(normalized, confidence)
            result["buffer_status"] = buffer.get_buffer_status()

            # Zone cameras require 5 identical OCR reads in a row within a 7-sample window.
            # If the window fills without that streak, discard it and start a fresh read cycle.
            candidate = buffer.get_consecutive_candidate(required=self.min_occurrences)
            if candidate is None:
                if buffer.is_buffer_full():
                    buffer.clear()
                    result["action"] = "reset"
                    result["buffer_status"] = f"reset after {self.buffer_size} samples without {self.min_occurrences} consecutive matches"
                return result

            best_plate = str(candidate.get("plate", normalized))
            best_conf = float(candidate.get("avg_conf", confidence))
            buffer.clear()
            self.stats["finalized"] += 1
            self.locked_plates.add(best_plate)
            self.pending_batch[best_plate] = {
                "plateNumber": best_plate,
                "confidence": round(best_conf, 4),
                "imageBase64": self.api_client._array_to_base64(
                    crop_image_array,
                    int(getattr(config, "ZONE_CROP_JPEG_QUALITY", 85)),
                ),
                "fullFrameImageBase64": self.api_client._array_to_base64(
                    full_frame_array,
                    int(getattr(config, "ZONE_FULL_FRAME_JPEG_QUALITY", 65)),
                ),
                "detectedAt": datetime.now().isoformat(),
            }
            result.update({
                "action": "locked",
                "plate": best_plate,
                "confidence": best_conf,
                "buffer_status": "locked for batch",
            })
            self.last_event = {
                "id": f"{best_plate}-locked-{time.time()}",
                "plate": best_plate,
                "confidence": best_conf,
                "action": "locked",
                "success": False,
                "message": "locked for batch",
                "timestamp": time.time(),
            }
        return result

    def _advance_cycle_locked(self) -> None:
        now = time.time()
        if now < self.pause_until:
            return
        if self.pause_until > 0 and now >= self.pause_until:
            self.pause_until = 0.0
            self.scan_started_at = now
            self.locked_plates.clear()
            self.pending_batch.clear()
            self.buffers.clear()
            self.batch_status = "collecting"
            logger.info("Zone scan cycle restarted")
            return
        if now - self.scan_started_at < self.scan_window_seconds:
            return

        batch = list(self.pending_batch.values())
        batch_started_at = datetime.fromtimestamp(self.scan_started_at)
        batch_ended_at = datetime.now()
        self.pause_until = now + self.scan_pause_seconds
        self.buffers.clear()

        if not batch:
            self.batch_status = "empty"
            logger.info("Zone scan window ended with no plates. Pausing %.0fs", self.scan_pause_seconds)
            return

        self.batch_status = "sending"
        self.stats["sent"] += len(batch)
        self.stats["batch_sent"] += 1
        logger.info(
            "Zone scan window ended. Sending %s plate(s), then pausing %.0fs",
            len(batch),
            self.scan_pause_seconds,
        )
        thread = threading.Thread(
            target=self._send_batch_async,
            args=(batch, batch_started_at, batch_ended_at),
            daemon=True,
        )
        thread.start()

    def _send_batch_async(self, batch, batch_started_at: datetime, batch_ended_at: datetime) -> None:
        success, response = self.api_client.send_location_detection_batch(
            detections=batch,
            camera_id=self.camera_id,
            location_name=self.location_name,
            parking_lot_code=self.parking_lot_code,
            zone_code=self.zone_code,
            column_code=self.column_code,
            batch_started_at=batch_started_at,
            batch_ended_at=batch_ended_at,
        )
        if not success:
            with self.lock:
                self.stats["api_failed"] += 1
                self.batch_status = "failed"
                self.last_event = {
                    "id": f"batch-failed-{time.time()}",
                    "plate": ",".join(item.get("plateNumber", "") for item in batch),
                    "confidence": 0,
                    "action": "failed",
                    "success": False,
                    "message": str(response)[:200],
                    "timestamp": time.time(),
                }
            logger.warning("Failed to send zone location batch: %s", response)
            return

        with self.lock:
            self.batch_status = "sent"
            self.last_event = {
                "id": f"batch-sent-{time.time()}",
                "plate": ",".join(item.get("plateNumber", "") for item in batch),
                "confidence": 0,
                "action": "sent",
                "success": True,
                "message": f"sent {len(batch)} plate(s)",
                "timestamp": time.time(),
            }

    def _can_send(self, plate_number: str) -> tuple[bool, str]:
        last_sent = self.last_sent_at.get(plate_number)
        if not last_sent:
            return True, "ready"
        remaining = self.cooldown_seconds - (time.time() - last_sent)
        if remaining <= 0:
            return True, "ready"
        return False, f"cooldown {remaining:.0f}s"

    def _send_async(self, plate_number: str, confidence: float, crop_image_array, full_frame_array) -> None:
        success, response = self.api_client.send_location_detection(
            plate_number=plate_number,
            camera_id=self.camera_id,
            location_name=self.location_name,
            confidence=confidence,
            crop_image_array=crop_image_array,
            full_frame_array=full_frame_array,
            parking_lot_code=self.parking_lot_code,
            zone_code=self.zone_code,
            column_code=self.column_code,
            detected_at=datetime.now(),
        )
        if not success:
            with self.lock:
                self.stats["api_failed"] += 1
                self.last_sent_at.pop(plate_number, None)
            logger.warning("Failed to send zone location: %s | %s", plate_number, response)

    def get_stats(self) -> Dict[str, Any]:
        with self.lock:
            return {
                **self.stats,
                "active_buffers": len(self.buffers),
                "cooldown_plates": len(self.last_sent_at),
                "locked_plates": len(self.locked_plates),
                "pending_batch": len(self.pending_batch),
                "scan_status": self.get_scan_status(),
            }

    def get_last_event(self) -> Optional[Dict[str, Any]]:
        with self.lock:
            return dict(self.last_event) if self.last_event else None
