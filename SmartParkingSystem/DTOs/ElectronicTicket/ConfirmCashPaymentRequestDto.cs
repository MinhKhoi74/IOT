using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartParking.DTOs.ElectronicTicket
{
    public class ConfirmCashPaymentRequestDto
    {
        [Required(ErrorMessage = "TicketId không được để trống")]
        [JsonPropertyName("ticketId")]
        public Guid TicketId { get; set; }

        [Required(ErrorMessage = "CheckInOutId không được để trống")]
        [JsonPropertyName("checkInOutId")]
        public Guid CheckInOutId { get; set; }

        [Required(ErrorMessage = "FeeAmount không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "FeeAmount phải lớn hơn 0")]
        [JsonPropertyName("feeAmount")]
        public decimal FeeAmount { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
