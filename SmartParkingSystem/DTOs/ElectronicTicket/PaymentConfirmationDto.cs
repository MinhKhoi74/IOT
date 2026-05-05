using SmartParking.Models.Enums;

namespace SmartParking.DTOs.ElectronicTicket
{
    public class PaymentConfirmationDto
    {
        /// <summary>
        /// ID của vé điện tử
        /// </summary>
        public Guid TicketId { get; set; }

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        public decimal PaymentAmount { get; set; }

        /// <summary>
        /// Phương thức thanh toán
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// Ghi chú của staff (tùy chọn)
        /// </summary>
        public string? Notes { get; set; }
    }
}
