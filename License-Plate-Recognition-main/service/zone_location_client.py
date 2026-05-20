"""
HTTP client for in-parking zone camera detections.
"""

import base64
import logging
import os
from datetime import datetime
from typing import Dict, List, Optional, Tuple

import cv2
import requests

import config

logger = logging.getLogger("parking_service.zone_location_client")


class ZoneLocationAPIClient:
    def __init__(self, api_url: str = config.BACKEND_API_URL, timeout: int = config.API_TIMEOUT):
        self.api_url = api_url.rstrip("/")
        self.timeout = timeout
        self.session = requests.Session()
        self.api_token = os.environ.get("SMARTPARKING_API_TOKEN", "").strip()
        self.branch_id = os.environ.get("SMARTPARKING_BRANCH_ID", "").strip()

    def send_location_detection(
        self,
        plate_number: str,
        camera_id: str,
        location_name: str,
        confidence: float,
        crop_image_array,
        full_frame_array=None,
        parking_lot_code: Optional[str] = None,
        zone_code: Optional[str] = None,
        column_code: Optional[str] = None,
        detected_at: Optional[datetime] = None,
    ) -> Tuple[bool, dict]:
        payload = {
            "plateNumber": (plate_number or "").upper().strip(),
            "cameraId": camera_id,
            "parkingLotCode": parking_lot_code,
            "zoneCode": zone_code,
            "columnCode": column_code,
            "locationName": location_name,
            "confidence": round(float(confidence), 4),
            "branchId": self.branch_id or None,
            "imageBase64": self._array_to_base64(
                crop_image_array,
                int(getattr(config, "ZONE_CROP_JPEG_QUALITY", 85)),
            ),
            "fullFrameImageBase64": self._array_to_base64(
                full_frame_array,
                int(getattr(config, "ZONE_FULL_FRAME_JPEG_QUALITY", 65)),
            ),
            "detectedAt": (detected_at or datetime.now()).isoformat(),
        }

        headers = {}
        if self.api_token:
            headers["Authorization"] = f"Bearer {self.api_token}"

        try:
            endpoint = f"{self.api_url}/location-detection"
            response = self.session.post(endpoint, json=payload, headers=headers, timeout=self.timeout)
            data = self._parse_response(response)
            success = 200 <= response.status_code < 300
            if success:
                logger.info(
                    "Location sent: %s | status=%s | location=%s",
                    plate_number,
                    data.get("status"),
                    location_name,
                )
            else:
                logger.warning("Location API error [%s]: %s | %s", response.status_code, plate_number, data)
            return success, data
        except requests.exceptions.RequestException as exc:
            logger.error("Location API request failed for %s: %s", plate_number, exc)
            return False, {"error": str(exc)}

    def send_location_detection_batch(
        self,
        detections: List[Dict],
        camera_id: str,
        location_name: str,
        parking_lot_code: Optional[str] = None,
        zone_code: Optional[str] = None,
        column_code: Optional[str] = None,
        batch_started_at: Optional[datetime] = None,
        batch_ended_at: Optional[datetime] = None,
    ) -> Tuple[bool, dict]:
        payload = {
            "cameraId": camera_id,
            "parkingLotCode": parking_lot_code,
            "zoneCode": zone_code,
            "columnCode": column_code,
            "locationName": location_name,
            "batchStartedAt": (batch_started_at or datetime.now()).isoformat(),
            "batchEndedAt": (batch_ended_at or datetime.now()).isoformat(),
            "branchId": self.branch_id or None,
            "detections": detections,
        }

        headers = {}
        if self.api_token:
            headers["Authorization"] = f"Bearer {self.api_token}"

        try:
            endpoint = f"{self.api_url}/location-detections/batch"
            response = self.session.post(endpoint, json=payload, headers=headers, timeout=self.timeout)
            data = self._parse_response(response)
            success = 200 <= response.status_code < 300
            if success:
                logger.info("Location batch sent: %s plate(s) | location=%s", len(detections), location_name)
            else:
                logger.warning("Location batch API error [%s]: %s", response.status_code, data)
            return success, data
        except requests.exceptions.RequestException as exc:
            logger.error("Location batch API request failed: %s", exc)
            return False, {"error": str(exc)}

    def health_check(self) -> bool:
        try:
            base_url = self.api_url.replace("/parking", "")
            response = self.session.get(f"{base_url}/health", timeout=2)
            return response.status_code < 500
        except Exception:
            return False

    @staticmethod
    def _array_to_base64(image_array, quality: int) -> Optional[str]:
        if image_array is None:
            return None
        try:
            ok, encoded = cv2.imencode(".jpg", image_array, [cv2.IMWRITE_JPEG_QUALITY, quality])
            if not ok:
                return None
            return base64.b64encode(bytes(encoded)).decode("utf-8")
        except Exception as exc:
            logger.error("Image encode failed: %s", exc)
            return None

    @staticmethod
    def _parse_response(response) -> dict:
        try:
            return response.json()
        except Exception:
            return {"status_code": response.status_code, "text": response.text[:200]}
