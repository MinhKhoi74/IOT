namespace SmartParking.DTOs.Parking
{
    public class ParkingHistoryItemDto
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal? FeeAmount { get; set; }
        public string FeeStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CheckInStationId { get; set; } = string.Empty;
        public string CheckInImageBase64 { get; set; } = string.Empty;
        public string CheckOutStationId { get; set; } = string.Empty;
        public string CheckOutImageBase64 { get; set; } = string.Empty;
        public Guid? VehicleId { get; set; }
        public string? UserId { get; set; }
    }
}
