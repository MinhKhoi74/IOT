import json
import os
import time
from dataclasses import dataclass
from datetime import datetime
from typing import Any, Dict, Optional


@dataclass(frozen=True)
class JsonStateStoreConfig:
    path: str
    lock_timeout_seconds: float = 3.0
    lock_retry_interval_seconds: float = 0.05


class JsonStateStore:
    """
    Simple JSON state store shared between multiple processes.
    Uses a lock file (create-exclusive) to coordinate cross-process writes.
    """

    def __init__(self, cfg: JsonStateStoreConfig):
        self._cfg = cfg
        self._lock_path = f"{cfg.path}.lock"
        self._ensure_parent_dir()
        self._ensure_file()

    def read(self) -> Dict[str, Any]:
        self._ensure_file()
        try:
            with open(self._cfg.path, "r", encoding="utf-8") as f:
                data = json.load(f)
            if isinstance(data, dict):
                return data
        except Exception:
            pass
        return self._default_state()

    def update(self, fn) -> Dict[str, Any]:
        """
        Lock -> read -> mutate -> write -> unlock.
        `fn(state)` should return mutated state (or mutate in place and return it).
        """
        self._acquire_lock()
        try:
            state = self.read()
            new_state = fn(state) or state
            if not isinstance(new_state, dict):
                new_state = state
            new_state["updated_at"] = datetime.now().isoformat(timespec="seconds")
            self._atomic_write(new_state)
            return new_state
        finally:
            self._release_lock()

    def _default_state(self) -> Dict[str, Any]:
        return {
            "active_plates": [],
            "checkins": {},
            "updated_at": None,
        }

    def _ensure_parent_dir(self) -> None:
        parent = os.path.dirname(os.path.abspath(self._cfg.path))
        if parent and not os.path.isdir(parent):
            os.makedirs(parent, exist_ok=True)

    def _ensure_file(self) -> None:
        if os.path.isfile(self._cfg.path):
            return
        state = self._default_state()
        self._atomic_write(state)

    def _atomic_write(self, data: Dict[str, Any]) -> None:
        tmp_path = f"{self._cfg.path}.tmp"
        with open(tmp_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        os.replace(tmp_path, self._cfg.path)

    def _acquire_lock(self) -> None:
        deadline = time.time() + float(self._cfg.lock_timeout_seconds)
        while True:
            try:
                fd = os.open(self._lock_path, os.O_CREAT | os.O_EXCL | os.O_RDWR)
                os.close(fd)
                return
            except FileExistsError:
                if time.time() >= deadline:
                    raise TimeoutError(f"State lock timeout: {self._lock_path}")
                time.sleep(float(self._cfg.lock_retry_interval_seconds))

    def _release_lock(self) -> None:
        try:
            os.remove(self._lock_path)
        except Exception:
            pass

