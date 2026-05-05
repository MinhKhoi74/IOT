import config
from service.smart_parking_service import SmartParkingService


def create_checkin_service(state_file_path: str | None = None) -> SmartParkingService:
    return SmartParkingService(station_id=config.STATION_ENTRANCE, state_file_path=state_file_path)

