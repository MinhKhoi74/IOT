namespace SmartParking.DTOs.Parking
{
    public class ParkingDashboardDto
    {
        public int ActiveVehicleCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<ParkingHistoryItemDto> Sessions { get; set; } = [];
    }
}
