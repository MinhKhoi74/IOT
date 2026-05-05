using SmartParking.DTOs.ElectronicTicket;

namespace SmartParking.Services.Interfaces
{
    public interface IElectronicTicketNotificationService
    {
        /// <summary>
        /// Gửi thông báo vé điện tử cho user qua SignalR
        /// </summary>
        Task SendTicketNotificationAsync(string userId, ElectronicTicketDetailDto ticketDto);

        /// <summary>
        /// Gửi thông báo thanh toán thành công
        /// </summary>
        Task SendPaymentConfirmationAsync(string userId, Guid ticketId, string message);

        /// <summary>
        /// Gửi thông báo yêu cầu thanh toán tiền mặt
        /// </summary>
        Task SendCashPaymentRequiredNotificationAsync(string userId, ElectronicTicketDetailDto ticketDto);
    }
}
