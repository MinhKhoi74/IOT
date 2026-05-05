using SmartParking.Models.Enums;

namespace SmartParking.DTOs.ElectronicTicket
{
    public class ElectronicTicketDetailDto
    {
        public Guid Id { get; set; }

        public string TicketCode { get; set; }

        public string LicensePlate { get; set; }

        public DateTime CheckInDateTime { get; set; }

        public DateTime? CheckOutDateTime { get; set; }

        public string ParkingLotName { get; set; }

        public string BranchName { get; set; }

        public string? UserId { get; set; }

        public decimal? FeeAmount { get; set; }

        public ElectronicTicketStatus Status { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? SentToUserAt { get; set; }

        /// <summary>
        /// Thời gian ở (tính bằng giờ) - nullable nếu chưa checkout
        /// </summary>
        public decimal? DurationHours { get; set; }
    }
}
