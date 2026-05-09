using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartParking.DTOs
{
    public class MonthlyPassDto
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string? OwnerPhone { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
    }

    public class MonthlyPassUpsertRequest
    {
        [Required]
        [JsonPropertyName("licensePlate")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("ownerName")]
        public string OwnerName { get; set; } = string.Empty;

        [JsonPropertyName("ownerPhone")]
        public string? OwnerPhone { get; set; }

        [JsonPropertyName("validFrom")]
        public DateTime ValidFrom { get; set; } = DateTime.Today;

        [JsonPropertyName("validTo")]
        public DateTime ValidTo { get; set; } = DateTime.Today.AddMonths(1);

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public class MonthlyPassMomoPaymentRequest : MonthlyPassUpsertRequest
    {
        [Range(1000, double.MaxValue)]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; } = "Wallet";
    }
}
