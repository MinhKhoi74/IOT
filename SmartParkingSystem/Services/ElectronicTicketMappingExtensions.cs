using SmartParking.DTOs.ElectronicTicket;
using SmartParking.Models;

namespace SmartParking.Services
{
    public static class ElectronicTicketMappingExtensions
    {
        public static ElectronicTicketDetailDto ToDetailDto(this ElectronicTicket ticket)
        {
            var duration = ticket.CheckOutDateTime.HasValue && ticket.CheckInDateTime != default
                ? (decimal?)(ticket.CheckOutDateTime.Value - ticket.CheckInDateTime).TotalHours
                : (decimal?)null;

            return new ElectronicTicketDetailDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                LicensePlate = ticket.LicensePlate,
                CheckInDateTime = ticket.CheckInDateTime,
                CheckOutDateTime = ticket.CheckOutDateTime,
                ParkingLotName = ticket.ParkingLotName,
                BranchName = ticket.BranchName,
                UserId = ticket.UserId,
                FeeAmount = ticket.FeeAmount,
                Status = ticket.Status,
                PaymentMethod = ticket.PaymentMethod,
                CreatedAt = ticket.CreatedAt,
                SentToUserAt = ticket.SentToUserAt,
                DurationHours = duration.HasValue ? (decimal)duration : null
            };
        }

        public static ElectronicTicketSummaryDto ToSummaryDto(this ElectronicTicket ticket)
        {
            return new ElectronicTicketSummaryDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                LicensePlate = ticket.LicensePlate,
                CheckInDateTime = ticket.CheckInDateTime,
                ParkingLotName = ticket.ParkingLotName,
                Status = ticket.Status,
                PaymentMethod = ticket.PaymentMethod,
                FeeAmount = ticket.FeeAmount
            };
        }

        public static TicketDeliveryListDto ToDeliveryListDto(this ElectronicTicket ticket, string? userName = null, string? userPhone = null)
        {
            var durationMinutes = ticket.CheckOutDateTime.HasValue && ticket.CheckInDateTime != default
                ? (int)(ticket.CheckOutDateTime.Value - ticket.CheckInDateTime).TotalMinutes
                : (int?)null;

            return new TicketDeliveryListDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                LicensePlate = ticket.LicensePlate,
                CheckInDateTime = ticket.CheckInDateTime,
                CheckOutDateTime = ticket.CheckOutDateTime,
                ParkingLotName = ticket.ParkingLotName,
                BranchName = ticket.BranchName,
                UserName = userName,
                UserPhone = userPhone,
                FeeAmount = ticket.FeeAmount,
                Status = ticket.Status,
                PaymentMethod = ticket.PaymentMethod,
                DurationMinutes = durationMinutes
            };
        }
    }
}
