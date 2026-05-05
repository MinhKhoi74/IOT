"""
Smart Parking AI Service - Core

Logic theo yêu cầu:
- Buffer cố định 10 mẫu
- Khi đủ 10 mẫu: chỉ nhận plate có tần suất >= 7/10
- Nếu không có plate nào đạt: xóa buffer và bắt đầu lại
- Checkin-lock: plate đã checkin sẽ bị bỏ qua cho tới khi checkout
"""

import logging
import threading
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, Optional, Tuple

import config
from service.api_client import create_api_client
from service.state_store import JsonStateStore, JsonStateStoreConfig
from service.voting_buffer import CooldownManager, PlateVotingBuffer

logger = logging.getLogger("parking_service")


class SmartParkingService:
    def __init__(
        self,
        station_id: str = config.STATION_ENTRANCE,
        state_file_path: Optional[str] = None,
    ):
        self._setup_logging()
        self._setup_directories()

        self.station_id = station_id
        self.voting_buffer = PlateVotingBuffer()
        self.cooldown_manager = CooldownManager()
        self.api_client = create_api_client()

        self.last_finalized_plate: Optional[str] = None
        self.scanning_enabled: bool = True

        # Shared persistent state for checkin-lock across processes
        state_path = state_file_path or getattr(config, "STATE_FILE_PATH", "logs/parking_state.json")
        self.state_store = JsonStateStore(
            JsonStateStoreConfig(
                path=state_path,
                lock_timeout_seconds=float(getattr(config, "STATE_LOCK_TIMEOUT_SECONDS", 3.0)),
                lock_retry_interval_seconds=float(getattr(config, "STATE_LOCK_RETRY_INTERVAL_SECONDS", 0.05)),
            )
        )

        self.stats = {
            "total_frames": 0,
            "plates_detected": 0,
            "buffer_finalized": 0,
            "buffer_reset": 0,
            "duplicates_skipped": 0,
            "api_sent": 0,
            "api_success": 0,
            "api_failed": 0,
        }

    def process_frame(
        self,
        plate_number: str,
        confidence: float,
        crop_image_array,
        frame_index: int = 0,
        send_api: bool = True,
    ) -> Dict[str, Any]:
        self.stats["total_frames"] += 1

        result: Dict[str, Any] = {
            "action": "buffered",
            "plate": plate_number,
            "confidence": confidence,
            "api_sent": False,
            "api_response": {},
            "buffer_status": "",
        }

        # === New vehicle detection (re-enable scan when plate differs enough) ===
        if plate_number != "unknown" and self.last_finalized_plate and not self.scanning_enabled:
            is_same = self.voting_buffer._is_same_vehicle(
                plate_number, self.last_finalized_plate, threshold=config.FUZZY_MATCH_THRESHOLD
            )
            if not is_same:
                self.scanning_enabled = True
                self.voting_buffer.clear()
            else:
                # Same plate seen again: if it is no longer active (already checked out),
                # re-enable scanning so it can be checked-in again.
                if not self._is_plate_active(self.last_finalized_plate):
                    self.scanning_enabled = True
                    self.voting_buffer.clear()

        # === Skip when locked on same vehicle in front of camera ===
        if not self.scanning_enabled and self.last_finalized_plate:
            is_same = self.voting_buffer._is_same_vehicle(
                plate_number, self.last_finalized_plate, threshold=config.FUZZY_MATCH_THRESHOLD
            )
            if is_same:
                result["action"] = "skipped"
                result["buffer_status"] = f"LOCKED ({self.last_finalized_plate})"
                return result

        # === Optional cooldown gate (disabled in config by default) ===
        if getattr(config, "COOLDOWN_ENABLED", False) and plate_number != "unknown":
            can_send, reason = self.cooldown_manager.can_send(plate_number)
            if not can_send:
                result["action"] = "cooldown"
                result["buffer_status"] = reason
                return result

        # === Add to buffer ===
        self.voting_buffer.add_plate(plate_number, confidence)
        self.stats["plates_detected"] += 1
        result["buffer_status"] = self.voting_buffer.get_buffer_status()

        min_occ = int(getattr(config, "PLATE_VOTE_MIN_OCCURRENCES", 5) or 5)

        # === Hybrid gate (fast): finalize immediately if N consecutive identical samples ===
        consecutive_required = int(getattr(config, "HYBRID_CONSECUTIVE_REQUIRED", 3) or 3)
        consecutive = self.voting_buffer.get_consecutive_candidate(required=consecutive_required)
        if consecutive:
            return self._finalize_candidate(consecutive, crop_image_array, send_api, result)

        # === Early-stop for vote (fast): finalize as soon as a plate reaches min_occurrences (5/7) ===
        early = self.voting_buffer.get_early_stop_candidate(min_occurrences=min_occ)
        if early:
            return self._finalize_candidate(early, crop_image_array, send_api, result)

        # Not enough samples yet for the 5/7 vote window
        if not self.voting_buffer.is_buffer_full():
            return result

        # === Buffer full: vote ===
        best = self.voting_buffer.get_best_candidate()
        if not best:
            self.voting_buffer.clear()
            self.stats["buffer_reset"] += 1
            result["action"] = "canceled"
            result["buffer_status"] = "RESET (empty)"
            return result

        best_plate = str(best.get("plate", "unknown"))
        best_count = int(best.get("count", 0))
        best_ratio = float(best.get("ratio", 0.0))
        best_confidence = float(best.get("avg_conf", 0.0))

        min_ratio = float(getattr(config, "PLATE_VOTE_MIN_RATIO", 0.7) or 0.7)

        # If no plate reaches 7/10 => reset buffer and start over
        if best_count < min_occ or best_ratio < min_ratio:
            self.voting_buffer.clear()
            self.stats["buffer_reset"] += 1
            result["action"] = "canceled"
            result["buffer_status"] = "RESET (no >= threshold)"
            return result

        # Finalize winner
        self.voting_buffer.clear()
        self.stats["buffer_finalized"] += 1
        result["action"] = "finalized"
        result["plate"] = best_plate
        result["confidence"] = best_confidence

        self.last_finalized_plate = best_plate
        self.scanning_enabled = False

        # === Checkin/Checkout + duplicate lock ===
        lock_enabled = bool(getattr(config, "CHECKIN_LOCK_ENABLED", True))
        event = "checkin" if self.station_id == config.STATION_ENTRANCE else "checkout"

        if lock_enabled:
            should_emit, msg = self._apply_checkin_lock(event, best_plate)
            if not should_emit:
                # Duplicate checkin => skip "ghi nhận" (không log checkin, không gửi API)
                self.stats["duplicates_skipped"] += 1
                result["action"] = "skipped"
                result["buffer_status"] = msg
                return result

        # Log to terminal in requested format
        self._log_event(event, best_plate)

        # === Send API (async) ===
        if not send_api:
            return result

        result["api_sent"] = True
        self.stats["api_sent"] += 1
        result["action"] = "sent"

        thread = threading.Thread(
            target=self._send_api_async,
            args=(best_plate, best_confidence, crop_image_array),
            daemon=True,
        )
        thread.start()
        return result

    def _finalize_candidate(
        self,
        candidate: Dict[str, object],
        crop_image_array,
        send_api: bool,
        result: Dict[str, Any],
    ) -> Dict[str, Any]:
        best_plate = str(candidate.get("plate", "unknown"))
        best_confidence = float(candidate.get("avg_conf", 0.0))

        self.voting_buffer.clear()
        self.stats["buffer_finalized"] += 1
        result["action"] = "finalized"
        result["plate"] = best_plate
        result["confidence"] = best_confidence

        self.last_finalized_plate = best_plate
        self.scanning_enabled = False

        lock_enabled = bool(getattr(config, "CHECKIN_LOCK_ENABLED", True))
        event = "checkin" if self.station_id == config.STATION_ENTRANCE else "checkout"
        if lock_enabled:
            should_emit, msg = self._apply_checkin_lock(event, best_plate)
            if not should_emit:
                self.stats["duplicates_skipped"] += 1
                result["action"] = "skipped"
                result["buffer_status"] = msg
                return result

        self._log_event(event, best_plate)

        if not send_api:
            return result

        result["api_sent"] = True
        self.stats["api_sent"] += 1
        result["action"] = "sent"
        thread = threading.Thread(
            target=self._send_api_async,
            args=(best_plate, best_confidence, crop_image_array),
            daemon=True,
        )
        thread.start()
        return result

    def _apply_checkin_lock(self, event: str, plate_number: str) -> Tuple[bool, str]:
        """
        Returns:
            (should_emit, reason)
            - should_emit=False means "bỏ qua không ghi nhận" (duplicate checkin while still active)
        """
        meta: Dict[str, Any] = {"should_emit": True, "reason": "OK"}

        def mutate(state: Dict[str, Any]) -> Dict[str, Any]:
            active = set(state.get("active_plates") or [])
            checkins = dict(state.get("checkins") or {})

            if event == "checkin":
                if plate_number in active:
                    meta["should_emit"] = False
                    meta["reason"] = f"SKIP (already checked-in: {plate_number})"
                    return state
                active.add(plate_number)
                meta["should_emit"] = True
                meta["reason"] = "OK"
            else:
                # checkout
                active.discard(plate_number)
                checkins.pop(plate_number, None)
                meta["should_emit"] = True
                meta["reason"] = "OK"

            state["active_plates"] = sorted(active)
            state["checkins"] = checkins
            return state

        self.state_store.update(mutate)
        return bool(meta["should_emit"]), str(meta["reason"])

    def _is_plate_active(self, plate_number: str) -> bool:
        try:
            state = self.state_store.read()
            active = set(state.get("active_plates") or [])
            return plate_number in active
        except Exception:
            return False

    @staticmethod
    def _format_terminal_time(dt: datetime) -> str:
        # Match example: 9/4/2026 - 14:02
        return f"{dt.day}/{dt.month}/{dt.year} - {dt.hour:02d}:{dt.minute:02d}"

    def _log_event(self, event: str, plate_number: str) -> None:
        now = datetime.now()
        time_str = self._format_terminal_time(now)

        if event == "checkin":
            # Persist checkin time to shared state
            def set_checkin_time(state: Dict[str, Any]) -> Dict[str, Any]:
                checkins = dict(state.get("checkins") or {})
                checkins[plate_number] = time_str
                state["checkins"] = checkins
                return state

            try:
                self.state_store.update(set_checkin_time)
            except Exception:
                pass
            logger.info(f"Checkin - {plate_number} - {time_str}")
            return

        # checkout
        logger.info(f"Checkout - {plate_number} - {time_str}")

    def _send_api_async(self, plate_number: str, confidence: float, crop_image_array) -> None:
        try:
            success, api_response = self.api_client.send_plate(
                plate_number=plate_number,
                station_id=self.station_id,
                crop_image_array=crop_image_array,
                confidence=confidence,
            )
            if success:
                self.stats["api_success"] += 1
                if getattr(config, "COOLDOWN_ENABLED", False):
                    self.cooldown_manager.add_to_cooldown(plate_number)
                logger.debug(f"Async API sent: {plate_number}")
            else:
                self.stats["api_failed"] += 1
                error_code = api_response.get("errorCode") if isinstance(api_response, dict) else None
                message = api_response.get("message") if isinstance(api_response, dict) else None
                logger.warning(
                    f"Async API failed: {plate_number}"
                    + (f" | errorCode={error_code}" if error_code else "")
                    + (f" | message={message}" if message else "")
                )
                if self.station_id == config.STATION_ENTRANCE:
                    self._rollback_failed_checkin(plate_number)
                result = api_response or {}
                logger.debug(f"Async API response: {result}")
        except Exception as e:
            self.stats["api_failed"] += 1
            logger.error(f"Async API error for {plate_number}: {str(e)}")

    def _rollback_failed_checkin(self, plate_number: str) -> None:
        def mutate(state: Dict[str, Any]) -> Dict[str, Any]:
            active = set(state.get("active_plates") or [])
            checkins = dict(state.get("checkins") or {})
            active.discard(plate_number)
            checkins.pop(plate_number, None)
            state["active_plates"] = sorted(active)
            state["checkins"] = checkins
            return state

        try:
            self.state_store.update(mutate)
            if self.last_finalized_plate == plate_number:
                self.scanning_enabled = True
            logger.warning(f"Rolled back local check-in state after API failure: {plate_number}")
        except Exception as rollback_error:
            logger.error(f"Failed to rollback local check-in state for {plate_number}: {rollback_error}")

    def get_stats(self) -> Dict[str, Any]:
        try:
            state = self.state_store.read()
            active_count = len(state.get("active_plates") or [])
        except Exception:
            active_count = 0
        return {
            **self.stats,
            "buffer": self.voting_buffer.get_stats(),
            "cooldown": self.cooldown_manager.get_stats(),
            "active_plates": active_count,
        }

    def log_stats(self) -> None:
        stats = self.get_stats()
        logger.info("\n" + "=" * 70)
        logger.info("Smart Parking Service Statistics:")
        logger.info(f"  Total Frames: {stats['total_frames']}")
        logger.info(f"  Plates Detected: {stats['plates_detected']}")
        logger.info(f"  Buffer Finalized: {stats['buffer_finalized']}")
        logger.info(f"  Buffer Reset: {stats['buffer_reset']}")
        logger.info(f"  Duplicates Skipped: {stats['duplicates_skipped']}")
        logger.info(f"  Active Plates: {stats['active_plates']}")
        logger.info(f"  API Sent: {stats['api_sent']}")
        logger.info(f"  API Success: {stats['api_success']}")
        logger.info(f"  API Failed: {stats['api_failed']}")
        logger.info("=" * 70 + "\n")

    def health_check(self) -> bool:
        """
        Kiểm tra backend có sống không. Nếu không kết nối được, vẫn cho phép chạy
        nhưng với warning (simulate mode fallback).
        """
        try:
            is_healthy = bool(self.api_client.health_check())
            if is_healthy:
                logger.info("✓ Backend is HEALTHY")
                return True
            else:
                logger.warning("✗ Backend UNREACHABLE - Running in degraded mode")
                logger.warning("   - Check if backend is running on http://localhost:5000")
                logger.warning("   - Using fallback: simulated responses")
                # Allow to continue but enable simulation as fallback
                if hasattr(config, 'SIMULATE_API'):
                    config.SIMULATE_API = True
                return False
        except Exception as e:
            logger.error(f"✗ Health check failed: {str(e)}")
            logger.warning("   - Will attempt to send data anyway (retry logic enabled)")
            return False

    def shutdown(self) -> None:
        logger.info("Shutting down SmartParkingService...")
        self.log_stats()
        try:
            self.api_client.close()
        except Exception:
            pass
        logger.info("Service shutdown complete")

    def _setup_directories(self) -> None:
        Path(config.LOG_DIR).mkdir(exist_ok=True)

    def _setup_logging(self) -> None:
        import sys

        log_dir = Path(config.LOG_DIR)
        log_dir.mkdir(exist_ok=True)

        # Fix Windows console encoding
        if sys.platform == "win32":
            try:
                sys.stdout.reconfigure(encoding="utf-8", errors="replace")
                sys.stderr.reconfigure(encoding="utf-8", errors="replace")
            except Exception:
                pass

        # Silence verbose loggers
        for noisy in ["yolov5", "torch", "cv2", "PIL", "urllib3", "requests"]:
            logging.getLogger(noisy).setLevel(logging.CRITICAL)

        # Configure a dedicated logger to avoid duplicated terminal output when other
        # modules/libraries add extra root handlers.
        parking_logger = logging.getLogger("parking_service")
        if getattr(parking_logger, "_configured", False):
            return

        parking_logger.setLevel(logging.DEBUG)
        parking_logger.propagate = False

        terminal_formatter = logging.Formatter("%(message)s")
        file_formatter = logging.Formatter(getattr(config, "LOG_FORMAT", "%(message)s"))

        sh = logging.StreamHandler(sys.stdout)
        sh.setLevel(logging.INFO)
        sh.setFormatter(terminal_formatter)

        fh = logging.FileHandler(log_dir / "smart_parking.log", encoding="utf-8")
        fh.setLevel(logging.DEBUG)
        fh.setFormatter(file_formatter)

        parking_logger.addHandler(sh)
        parking_logger.addHandler(fh)
        setattr(parking_logger, "_configured", True)
