using Microsoft.AspNetCore.SignalR;
using SmartParking.DTOs.ElectronicTicket;
using SmartParking.Services.Interfaces;
using SmartParking.SignalR;

namespace SmartParking.Services
{
    public class ElectronicTicketNotificationService : IElectronicTicketNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ElectronicTicketNotificationService> _logger;

        public ElectronicTicketNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<ElectronicTicketNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendTicketNotificationAsync(string userId, ElectronicTicketDetailDto ticketDto)
        {
            try
            {
                var message = new
                {
                    type = "ticket_created",
                    title = "Vé gửi xe",
                    message = $"Vé gửi xe tại {ticketDto.ParkingLotName} - Biển số: {ticketDto.LicensePlate}",
                    ticket = ticketDto,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.User(userId)
                    .SendAsync("ReceiveTicket", message);

                _logger.LogInformation($"Ticket notification sent to user: {userId}, Ticket: {ticketDto.TicketCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending ticket notification: {ex.Message}");
            }
        }

        public async Task SendPaymentConfirmationAsync(string userId, Guid ticketId, string message)
        {
            try
            {
                var notification = new
                {
                    type = "payment_confirmed",
                    title = "Thanh toán thành công",
                    message = message,
                    ticketId = ticketId,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.User(userId)
                    .SendAsync("ReceivePaymentConfirmation", notification);

                _logger.LogInformation($"Payment confirmation sent to user: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending payment confirmation: {ex.Message}");
            }
        }

        public async Task SendCashPaymentRequiredNotificationAsync(string userId, ElectronicTicketDetailDto ticketDto)
        {
            try
            {
                var message = new
                {
                    type = "cash_payment_required",
                    title = "Cần thanh toán tiền mặt",
                    message = $"Ví không đủ tiền. Vui lòng thanh toán {ticketDto.FeeAmount} đồng bằng tiền mặt.",
                    ticket = ticketDto,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.User(userId)
                    .SendAsync("ReceiveCashPaymentRequired", message);

                _logger.LogInformation($"Cash payment required notification sent to user: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending cash payment required notification: {ex.Message}");
            }
        }
    }
}
