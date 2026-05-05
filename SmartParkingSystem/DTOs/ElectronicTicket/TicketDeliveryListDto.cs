using SmartParking.Models.Enums;

namespace SmartParking.DTOs.ElectronicTicket
{
    /// <summary>
    /// DTO cho danh sách gửi xe (delivery list)
    /// </summary>
    public class TicketDeliveryListDto
    {
        public Guid Id { get; set; }

        public string TicketCode { get; set; }

        public string LicensePlate { get; set; }

        public DateTime CheckInDateTime { get; set; }

        public DateTime? CheckOutDateTime { get; set; }

        public string ParkingLotName { get; set; }

        public string BranchName { get; set; }

        public string? UserName { get; set; }

        public string? UserPhone { get; set; }

        public decimal? FeeAmount { get; set; }

        public ElectronicTicketStatus Status { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        /// <summary>
        /// Thời gian ở (tính bằng phút)
        /// </summary>
        public int? DurationMinutes { get; set; }
    }
}
