using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartParking.DTOs
{
    public class VehicleLocationDetectionRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [JsonPropertyName("plateNumber")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [JsonPropertyName("cameraId")]
        public string CameraId { get; set; } = string.Empty;

        [JsonPropertyName("parkingLotCode")]
        public string? ParkingLotCode { get; set; }

        [JsonPropertyName("zoneCode")]
        public string? ZoneCode { get; set; }

        [JsonPropertyName("columnCode")]
        public string? ColumnCode { get; set; }

        [Required]
        [StringLength(250)]
        [JsonPropertyName("locationName")]
        public string LocationName { get; set; } = string.Empty;

        [Range(0, 1)]
        [JsonPropertyName("confidence")]
        public float Confidence { get; set; } = 0.9f;

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }

        [JsonPropertyName("fullFrameImageBase64")]
        public string? FullFrameImageBase64 { get; set; }

        [JsonPropertyName("detectedAt")]
        public DateTime? DetectedAt { get; set; }
    }

    public class VehicleLocationDetectionBatchRequest
    {
        [Required]
        [StringLength(100)]
        [JsonPropertyName("cameraId")]
        public string CameraId { get; set; } = string.Empty;

        [JsonPropertyName("parkingLotCode")]
        public string? ParkingLotCode { get; set; }

        [JsonPropertyName("zoneCode")]
        public string? ZoneCode { get; set; }

        [JsonPropertyName("columnCode")]
        public string? ColumnCode { get; set; }

        [Required]
        [StringLength(250)]
        [JsonPropertyName("locationName")]
        public string LocationName { get; set; } = string.Empty;

        [JsonPropertyName("batchStartedAt")]
        public DateTime? BatchStartedAt { get; set; }

        [JsonPropertyName("batchEndedAt")]
        public DateTime? BatchEndedAt { get; set; }

        [Required]
        [MinLength(1)]
        [JsonPropertyName("detections")]
        public List<VehicleLocationDetectionBatchItem> Detections { get; set; } = new();
    }

    public class VehicleLocationDetectionBatchItem
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [JsonPropertyName("plateNumber")]
        public string PlateNumber { get; set; } = string.Empty;

        [Range(0, 1)]
        [JsonPropertyName("confidence")]
        public float Confidence { get; set; } = 0.9f;

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }

        [JsonPropertyName("fullFrameImageBase64")]
        public string? FullFrameImageBase64 { get; set; }

        [JsonPropertyName("detectedAt")]
        public DateTime? DetectedAt { get; set; }
    }

    public class VehicleLocationDetectionBatchResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("processedCount")]
        public int ProcessedCount { get; set; }

        [JsonPropertyName("results")]
        public List<VehicleLocationDetectionResult> Results { get; set; } = new();
    }

    public class VehicleLocationDetectionResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("detection")]
        public VehicleLocationDto? Detection { get; set; }
    }

    public class VehicleLocationDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("licensePlate")]
        public string LicensePlate { get; set; } = string.Empty;

        [JsonPropertyName("vehicleId")]
        public Guid? VehicleId { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("ownerName")]
        public string? OwnerName { get; set; }

        [JsonPropertyName("checkInOutId")]
        public int? CheckInOutId { get; set; }

        [JsonPropertyName("cameraId")]
        public string CameraId { get; set; } = string.Empty;

        [JsonPropertyName("parkingLotCode")]
        public string? ParkingLotCode { get; set; }

        [JsonPropertyName("zoneCode")]
        public string? ZoneCode { get; set; }

        [JsonPropertyName("columnCode")]
        public string? ColumnCode { get; set; }

        [JsonPropertyName("locationName")]
        public string LocationName { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }

        [JsonPropertyName("fullFrameImageBase64")]
        public string? FullFrameImageBase64 { get; set; }

        [JsonPropertyName("detectedAt")]
        public DateTime DetectedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("isLatest")]
        public bool IsLatest { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
