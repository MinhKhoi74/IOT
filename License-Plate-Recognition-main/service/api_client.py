"""
Smart Parking - API Client
Gui JSON voi image base64 sang Backend
"""

import base64
import json
import logging
import os
import time
from typing import Optional, Tuple

import requests

import config

try:
    import socketio
    HAS_SOCKETIO = True
except ImportError:
    HAS_SOCKETIO = False

logger = logging.getLogger("parking_service.api_client")


class ParkingAPIClient:
    """
    Client gui du lieu sang Backend qua JSON.

    Payload:
    - plateNumber: string
    - stationId: string
    - confidence: float
    - imageBase64: string
    """

    def __init__(self, api_url: str = config.BACKEND_API_URL, timeout: int = config.API_TIMEOUT):
        self.api_url = api_url
        self.timeout = timeout
        self.session = requests.Session()
        self.api_token = os.environ.get("SMARTPARKING_API_TOKEN", "").strip()

    def send_plate(
        self,
        plate_number: str,
        station_id: str,
        crop_image_array,
        confidence: float,
    ) -> Tuple[bool, dict]:
        normalized_plate = (plate_number or "").upper().strip()
        station_id = station_id or ""
        action = "check-out" if "EXIT" in station_id.upper() or "02" in station_id else "check-in"

        if config.SIMULATE_API:
            success, data = self._simulate_api(normalized_plate, station_id)
            logger.info(f"✓ Simulated: {normalized_plate}")
            for handler in logging.getLogger().handlers:
                handler.flush()
            return success, data

        try:
            image_bytes = self._array_to_bytes(crop_image_array)
            if not image_bytes:
                logger.error(f"✗ Image conversion failed: {normalized_plate}")
                return False, {"error": "image_conversion"}

            image_b64 = base64.b64encode(image_bytes).decode("utf-8")
            payload = {
                "plateNumber": normalized_plate,
                "stationId": station_id,
                "confidence": round(float(confidence), 4),
                "imageBase64": image_b64,
            }

            base_url = self.api_url.replace("/parking", "")
            endpoint = f"{base_url}/parking/{action}"

            headers = {}
            if self.api_token:
                headers["Authorization"] = f"Bearer {self.api_token}"

            response = self.session.post(endpoint, json=payload, headers=headers, timeout=self.timeout)
            success = response.status_code == 200
            response_data = self._parse_response(response)

            if success:
                logger.info(f"✓ API Success: {normalized_plate} | action={action} | endpoint={endpoint}")
                for handler in logging.getLogger().handlers:
                    handler.flush()
            else:
                logger.warning(
                    f"✗ API Error [{response.status_code}]: {normalized_plate} | "
                    f"action={action} | endpoint={endpoint} | detail={self._summarize_response(response_data)}"
                )

            return success, response_data

        except requests.exceptions.Timeout:
            logger.error(f"✗ API Timeout: {normalized_plate} | action={action}")
            return False, {"error": "timeout"}

        except requests.exceptions.ConnectionError:
            logger.error("✗ Connection Error: Backend unavailable")
            return False, {"error": "connection"}

        except Exception as exc:
            logger.error(f"✗ API Error [{normalized_plate}]: {exc}")
            return False, {"error": "unknown", "message": str(exc)}

    def _array_to_bytes(self, image_array) -> Optional[bytes]:
        try:
            import cv2

            _, encoded = cv2.imencode(".jpg", image_array, [cv2.IMWRITE_JPEG_QUALITY, 85])
            return bytes(encoded)
        except Exception as exc:
            logger.error(f"✗ Image encoding error: {exc}")
            return None

    def _parse_response(self, response) -> dict:
        try:
            return response.json()
        except Exception:
            return {"status_code": response.status_code, "text": response.text[:200]}

    def _summarize_response(self, response_data: dict) -> str:
        if not isinstance(response_data, dict):
            return str(response_data)

        parts = []
        for key in ("message", "errorCode", "title", "detail", "error", "status_code"):
            value = response_data.get(key)
            if value not in (None, "", [], {}):
                parts.append(f"{key}={value}")

        errors = response_data.get("errors")
        if isinstance(errors, dict) and errors:
            compact_errors = {
                field: messages[:2] if isinstance(messages, list) else messages
                for field, messages in errors.items()
            }
            parts.append(f"errors={json.dumps(compact_errors, ensure_ascii=False)}")

        raw_text = response_data.get("text") or response_data.get("raw")
        if raw_text:
            parts.append(f"text={str(raw_text)[:200]}")

        return " | ".join(parts) if parts else json.dumps(response_data, ensure_ascii=False)[:300]

    def _simulate_api(self, plate: str, station: str) -> Tuple[bool, dict]:
        return True, {
            "status": "success",
            "message": f"Plate {plate} processed",
            "transactionId": f"TXN_{plate}_{int(time.time())}",
        }

    def health_check(self) -> bool:
        endpoints_to_try = [
            "http://localhost:5000/api/parking",
            "http://localhost:5000/api/health",
            "http://localhost:5000",
        ]

        for endpoint in endpoints_to_try:
            try:
                response = self.session.get(endpoint, timeout=2)
                if response.status_code in [200, 404, 405]:
                    logger.info("✓ Backend accessible")
                    return True
            except Exception:
                pass

        logger.warning("⚠ Could not connect to backend - will use retry logic")
        return False

    def close(self):
        self.session.close()
        logger.info("API Client closed")


def create_api_client():
    logger.info("✓ Using HTTP transport with retry logic")
    return ParkingAPIClient(config.BACKEND_API_URL)
