namespace SmartParking.Models
{
    public class VehicleLocationDetection
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public Guid? VehicleId { get; set; }
        public string? UserId { get; set; }
        public int? CheckInOutId { get; set; }

        public string CameraId { get; set; } = string.Empty;
        public string? ParkingLotCode { get; set; }
        public string? ZoneCode { get; set; }
        public string? ColumnCode { get; set; }
        public string LocationName { get; set; } = string.Empty;

        public float Confidence { get; set; }
        public string? ImageBase64 { get; set; }
        public string? FullFrameImageBase64 { get; set; }
        public DateTime DetectedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsLatest { get; set; } = true;

        public string Status { get; set; } = "KnownCheckedIn";
        public string Severity { get; set; } = "Info";
        public string Message { get; set; } = string.Empty;

        public Vehicle? Vehicle { get; set; }
        public Identity.ApplicationUser? User { get; set; }
        public CheckInOut? CheckInOut { get; set; }
    }
}
