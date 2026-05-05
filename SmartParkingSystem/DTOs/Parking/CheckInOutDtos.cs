using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SmartParking.DTOs.Momo;

namespace SmartParking.DTOs
{
    public class CheckInRequest
    {
        [Required(ErrorMessage = "Bien so xe khong duoc de trong")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Bien so phai tu 3-50 ky tu")]
        [JsonPropertyName("plateNumber")]
        public string PlateNumber { get; set; } = string.Empty;

        [JsonPropertyName("stationId")]
        public string? StationId { get; set; }

        [Range(0, 1, ErrorMessage = "Confidence phai tu 0 den 1")]
        [JsonPropertyName("confidence")]
        public float Confidence { get; set; } = 0.9f;

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }
    }

    public class CheckInResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("checkInId")]
        public int? CheckInId { get; set; }

        [JsonPropertyName("checkInTime")]
        public DateTime? CheckInTime { get; set; }
    }

    public class CheckOutRequest
    {
        [Required(ErrorMessage = "Bien so xe khong duoc de trong")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Bien so phai tu 3-50 ky tu")]
        [JsonPropertyName("plateNumber")]
        public string PlateNumber { get; set; } = string.Empty;

        [JsonPropertyName("stationId")]
        public string? StationId { get; set; }

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }
    }

    public class CheckOutResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("checkOutId")]
        public int? CheckOutId { get; set; }

        [JsonPropertyName("checkOutTime")]
        public DateTime? CheckOutTime { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int? DurationMinutes { get; set; }

        [JsonPropertyName("feeAmount")]
        public decimal? FeeAmount { get; set; }

        [JsonPropertyName("paymentStatus")]
        public string? PaymentStatus { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("walletBalanceAfter")]
        public decimal? WalletBalanceAfter { get; set; }

        [JsonPropertyName("requiresPaymentAction")]
        public bool RequiresPaymentAction { get; set; }

        [JsonPropertyName("paymentOptions")]
        public List<CheckOutPaymentOptionDto>? PaymentOptions { get; set; }
    }

    public class ConfirmCheckOutPaymentRequest
    {
        [Required]
        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class CheckOutPaymentOptionDto
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("momo")]
        public MomoCreatePaymentResultDto? Momo { get; set; }
    }
}
