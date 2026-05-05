"""
Smart Parking - Voting Buffer & Cooldown Manager

Flow (buffer fixed window):
- Tích lũy đúng N mẫu gần nhất (mặc định N=10)
- Khi đủ N mẫu: chọn plate có tần suất >= min_occurrences (mặc định 7/10)
- Nếu không có plate nào đạt: xóa buffer và bắt đầu lại
"""

import logging
import re
import time
from collections import Counter, OrderedDict
from typing import Dict, List, Optional, Tuple

import config

logger = logging.getLogger("parking_service.voting_buffer")


def _levenshtein_distance(s1: str, s2: str) -> int:
    if len(s1) < len(s2):
        return _levenshtein_distance(s2, s1)
    if len(s2) == 0:
        return len(s1)
    previous_row = range(len(s2) + 1)
    for i, c1 in enumerate(s1):
        current_row = [i + 1]
        for j, c2 in enumerate(s2):
            insertions = previous_row[j + 1] + 1
            deletions = current_row[j] + 1
            substitutions = previous_row[j] + (c1 != c2)
            current_row.append(min(insertions, deletions, substitutions))
        previous_row = current_row
    return previous_row[-1]


def _similarity_percentage(s1: str, s2: str) -> float:
    if not s1 or not s2:
        return 0.0
    distance = _levenshtein_distance(s1, s2)
    max_length = max(len(s1), len(s2))
    similarity = (1 - distance / max_length) * 100
    return max(0.0, min(100.0, similarity))


class PlateVotingBuffer:
    def __init__(self, buffer_size: int = config.PLATE_BUFFER_SIZE):
        self.buffer_size = int(buffer_size)
        self.window_seconds = float(getattr(config, "PLATE_BUFFER_WINDOW_SECONDS", 0.0) or 0.0)
        self.min_occurrences = int(getattr(config, "PLATE_VOTE_MIN_OCCURRENCES", 7) or 7)
        self.min_ratio = float(getattr(config, "PLATE_VOTE_MIN_RATIO", 0.7) or 0.7)
        self.buffer: List[Tuple[float, str, float]] = []  # [(ts, plate, confidence), ...]
        self.frame_count = 0

    def add_plate(self, plate_number: str, confidence: float) -> None:
        if not plate_number or plate_number == "unknown":
            return

        plate_number = self._normalize_plate(plate_number)
        if not self._is_valid_format(plate_number):
            logger.debug(f"Filtered out invalid format: {plate_number}")
            return

        self.buffer.append((time.time(), plate_number, float(confidence)))
        self.frame_count += 1
        self._cleanup()

    def is_buffer_full(self) -> bool:
        self._cleanup()
        return len(self.buffer) >= self.buffer_size

    def get_best_candidate(self) -> Optional[Dict[str, object]]:
        """
        Returns candidate best by frequency (exact string).
        Keys: plate, count, ratio, avg_conf, is_vn
        """
        self._cleanup()
        return self._get_best_vote()

    def get_consecutive_candidate(self, required: int = 4) -> Optional[Dict[str, object]]:
        """
        Returns candidate when the last `required` samples are identical (exact string).
        Useful for "4-in-a-row" fast finalize.
        """
        required = int(required)
        if required <= 0:
            return None

        self._cleanup()
        if len(self.buffer) < required:
            return None

        tail = self.buffer[-required:]
        plates = [p for _, p, _ in tail]
        if not plates:
            return None

        first = plates[0]
        if any(p != first for p in plates[1:]):
            return None

        avg_conf = sum(float(conf) for _, _, conf in tail) / float(required)
        return {
            "plate": str(first),
            "count": required,
            "avg_conf": float(avg_conf),
            "ratio": 1.0,
            "is_vn": bool(self._is_valid_vietnam_plate(first)),
        }

    def get_early_stop_candidate(self, min_occurrences: int) -> Optional[Dict[str, object]]:
        """
        Early-stop for vote window:
        - If any plate reaches `min_occurrences` within the current batch (before buffer is full),
          it is guaranteed to satisfy min_occurrences in the final fixed window size.
        """
        min_occurrences = int(min_occurrences)
        if min_occurrences <= 0:
            return None

        self._cleanup()
        if not self.buffer:
            return None

        observations = [(plate, float(conf)) for _, plate, conf in self.buffer]
        counts = Counter([plate for plate, _ in observations])
        best_plate, best_count = counts.most_common(1)[0]
        if int(best_count) < min_occurrences:
            return None

        best_items = [(p, c) for p, c in observations if p == best_plate]
        avg_conf = sum(c for _, c in best_items) / max(1, len(best_items))
        total = len(observations)
        ratio = float(best_count) / float(total) if total else 0.0
        return {
            "plate": str(best_plate),
            "count": int(best_count),
            "avg_conf": float(avg_conf),
            "ratio": float(ratio),
            "is_vn": bool(self._is_valid_vietnam_plate(best_plate)),
        }

    def get_buffer_status(self) -> str:
        self._cleanup()
        return f"{len(self.buffer)}/{self.buffer_size} samples"

    def clear(self) -> None:
        self.buffer.clear()

    def get_stats(self) -> Dict[str, object]:
        self._cleanup()
        plates_only = [p for _, p, _ in self.buffer]
        return {
            "frame_count": self.frame_count,
            "buffer_size": len(self.buffer),
            "plates": plates_only,
            "total_frames_processed": self.frame_count,
            "window_seconds": self.window_seconds,
        }

    def _cleanup(self) -> None:
        if not self.buffer:
            return

        # Optional time window cleanup (disabled when window_seconds <= 0)
        if self.window_seconds and self.window_seconds > 0:
            now = time.time()
            cutoff = now - self.window_seconds
            self.buffer = [item for item in self.buffer if item[0] >= cutoff]

        # Keep newest N observations (fixed-size window)
        if len(self.buffer) > self.buffer_size:
            self.buffer = self.buffer[-self.buffer_size :]

    def _get_best_vote(self) -> Optional[Dict[str, object]]:
        if not self.buffer:
            return None

        observations = [(plate, float(conf)) for _, plate, conf in self.buffer]
        total = len(observations)
        if total <= 0:
            return None

        counts = Counter([plate for plate, _ in observations])
        best_plate, best_count = counts.most_common(1)[0]
        best_items = [(p, c) for p, c in observations if p == best_plate]
        avg_conf = sum(c for _, c in best_items) / max(1, len(best_items))
        ratio = best_count / total
        is_vn = self._is_valid_vietnam_plate(best_plate)

        return {
            "plate": str(best_plate),
            "count": int(best_count),
            "avg_conf": float(avg_conf),
            "ratio": float(ratio),
            "is_vn": bool(is_vn),
        }

    @staticmethod
    def _normalize_plate(plate: str) -> str:
        return str(plate).strip().upper().replace(" ", "")

    @staticmethod
    def _is_valid_vietnam_plate(plate: str) -> bool:
        try:
            pattern = config.VIETNAM_PLATE_REGEX
            return bool(re.match(pattern, str(plate)))
        except Exception:
            return False

    @staticmethod
    def _is_valid_format(plate: str) -> bool:
        if not plate:
            return False
        plate_len = len(plate)
        if plate_len < 7 or plate_len > 11:
            return False
        has_letter = any(c.isalpha() for c in plate)
        has_digit = any(c.isdigit() for c in plate)
        return has_letter and has_digit

    @staticmethod
    def _is_same_vehicle(plate_a: str, plate_b: str, threshold: float = 80.0) -> bool:
        if not plate_a or not plate_b:
            return False
        similarity = _similarity_percentage(plate_a, plate_b)
        return similarity >= threshold


class CooldownManager:
    def __init__(self, cooldown_seconds: int = config.COOLDOWN_SECONDS):
        self.cooldown_seconds = int(cooldown_seconds)
        self.cooldown_list: Dict[str, float] = OrderedDict()  # plate -> timestamp

    def can_send(self, plate_number: str) -> Tuple[bool, str]:
        if not plate_number or plate_number == "unknown":
            return False, "Invalid plate"

        current_time = time.time()
        if plate_number in self.cooldown_list:
            last_sent_time = self.cooldown_list[plate_number]
            time_elapsed = current_time - last_sent_time
            if time_elapsed < self.cooldown_seconds:
                remaining = self.cooldown_seconds - time_elapsed
                return False, f"Cooldown ({remaining:.1f}s remaining)"
        return True, "OK"

    def add_to_cooldown(self, plate_number: str) -> None:
        self.cooldown_list[plate_number] = time.time()
        self._cleanup_expired()

    def _cleanup_expired(self) -> None:
        current_time = time.time()
        expired_plates = [
            plate
            for plate, sent_time in self.cooldown_list.items()
            if current_time - sent_time >= self.cooldown_seconds + 5
        ]
        for plate in expired_plates:
            del self.cooldown_list[plate]
            logger.debug(f"Removed from cooldown: {plate}")

    def get_stats(self) -> Dict[str, object]:
        return {
            "total_in_cooldown": len(self.cooldown_list),
            "plates": list(self.cooldown_list.keys()),
            "cooldown_seconds": self.cooldown_seconds,
        }

    def clear_all(self) -> None:
        self.cooldown_list.clear()
        logger.info("Cooldown list cleared")
