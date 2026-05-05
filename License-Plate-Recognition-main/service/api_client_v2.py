"""
Smart Parking - Parking Service (Enhanced with Redis support)
Hỗ trợ cả checkin/checkout endpoints mới (JSON-based)

Sử dụng:
- POST /api/parking/check-in  (mới - JSON)
- POST /api/parking/check-out (mới - JSON)
"""

import requests
import logging
import time
from typing import Tuple, Optional
from datetime import datetime
import json

logger = logging.getLogger("parking_service.api_client_v2")


class ParkingAPIClientV2:
    """
    Enhanced API Client với hỗ trợ checkin/checkout riêng biệt.
    Sử dụng JSON payloads thay vì multipart/form-data.
    """
    
    def __init__(self, api_url: str = "http://localhost:5000/api/parking", 
                 timeout: int = 10):
        self.api_url = api_url
        self.timeout = timeout
        self.session = requests.Session()
    
    def check_in(self, plate_number: str, station_id: str = "STATION_01",
                 confidence: float = 0.9) -> Tuple[bool, dict]:
        """
        Gửi checkin request (xe vào bãi)
        
        Args:
            plate_number: Biển số xe (VD: "59F1-68955")
            station_id: ID trạm vào (mặc định: "STATION_01")
            confidence: Độ tin cậy nhận dạng (0-1)
        
        Returns:
            (success: bool, response: dict)
        """
        try:
            payload = {
                "plateNumber": plate_number.upper().strip(),
                "stationId": station_id,
                "confidence": float(confidence)
            }
            
            response = self.session.post(
                f"{self.api_url}/check-in",
                json=payload,
                timeout=self.timeout
            )
            
            success = response.status_code == 200
            data = self._parse_response(response)
            
            if success:
                logger.info(f"✓ Checkin success: {plate_number} - {datetime.now().strftime('%H:%M:%S')}")
            else:
                logger.warning(f"✗ Checkin failed: {plate_number} - {data.get('message', 'Unknown error')}")
            
            return success, data
            
        except Exception as ex:
            logger.error(f"❌ Checkin exception: {plate_number} - {str(ex)}")
            return False, {'error': str(ex)}
    
    def check_out(self, plate_number: str, station_id: str = "STATION_02") -> Tuple[bool, dict]:
        """
        Gửi checkout request (xe ra khỏi bãi)
        
        Args:
            plate_number: Biển số xe (VD: "59F1-68955")
            station_id: ID trạm ra (mặc định: "STATION_02")
        
        Returns:
            (success: bool, response: dict với fee information)
        """
        try:
            payload = {
                "plateNumber": plate_number.upper().strip(),
                "stationId": station_id
            }
            
            response = self.session.post(
                f"{self.api_url}/check-out",
                json=payload,
                timeout=self.timeout
            )
            
            success = response.status_code == 200
            data = self._parse_response(response)
            
            if success:
                fee = data.get('feeAmount', 0)
                duration = data.get('durationMinutes', 0)
                fee_text = f"{fee:,.0f}đ" if fee > 0 else "FREE"
                logger.info(f"✓ Checkout success: {plate_number} - Duration: {duration}min - Fee: {fee_text}")
            else:
                logger.warning(f"✗ Checkout failed: {plate_number} - {data.get('message', 'Unknown error')}")
            
            return success, data
            
        except Exception as ex:
            logger.error(f"❌ Checkout exception: {plate_number} - {str(ex)}")
            return False, {'error': str(ex)}
    
    def _parse_response(self, response: requests.Response) -> dict:
        """Parse API response"""
        try:
            return response.json()
        except:
            return {'raw': response.text, 'status_code': response.status_code}
    
    def health_check(self) -> bool:
        """Kiểm tra backend availability"""
        try:
            response = self.session.get(
                f"{self.api_url}/../health",  # Assume there's a health endpoint
                timeout=self.timeout
            )
            return response.status_code == 200
        except:
            return False


# === Usage Example ===
if __name__ == "__main__":
    client = ParkingAPIClientV2()
    
    # Test Checkin
    print("=== Testing Checkin ===")
    success, result = client.check_in("59F1-68955")
    print(f"Success: {success}")
    print(f"Result: {json.dumps(result, indent=2, ensure_ascii=False)}")
    
    # Test Checkout
    print("\n=== Testing Checkout ===")
    success, result = client.check_out("59F1-68955")
    print(f"Success: {success}")
    print(f"Result: {json.dumps(result, indent=2, ensure_ascii=False)}")
