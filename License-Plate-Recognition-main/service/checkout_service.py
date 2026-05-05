import config
from service.smart_parking_service import SmartParkingService


def create_checkout_service(state_file_path: str | None = None) -> SmartParkingService:
    return SmartParkingService(station_id=config.STATION_EXIT, state_file_path=state_file_path)

