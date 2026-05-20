namespace SmartParking.DTOs.Parking
{
    public class ParkingDashboardDto
    {
        public int ActiveVehicleCount { get; set; }
        public int? MaxVehicleCapacity { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CasualRevenue { get; set; }
        public decimal MonthlyPassRevenue { get; set; }
        public List<ParkingHistoryItemDto> Sessions { get; set; } = [];
    }
}
