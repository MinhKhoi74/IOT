using SmartParking.Models.Enums;

namespace SmartParking.DTOs.ElectronicTicket
{
    public class ElectronicTicketSummaryDto
    {
        public Guid Id { get; set; }

        public string TicketCode { get; set; }

        public string LicensePlate { get; set; }

        public DateTime CheckInDateTime { get; set; }

        public string ParkingLotName { get; set; }

        public ElectronicTicketStatus Status { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        public decimal? FeeAmount { get; set; }
    }
}
