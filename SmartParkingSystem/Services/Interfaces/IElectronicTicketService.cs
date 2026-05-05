using SmartParking.DTOs.ElectronicTicket;
using SmartParking.Models;

namespace SmartParking.Services.Interfaces
{
    public interface IElectronicTicketService
    {
        /// <summary>
        /// Tạo vé điện tử mới khi check-in
        /// </summary>
        Task<ElectronicTicketDetailDto> CreateTicketAsync(CreateElectronicTicketDto dto);

        /// <summary>
        /// Lấy vé theo ID (chỉ user owner hoặc admin)
        /// </summary>
        Task<ElectronicTicketDetailDto> GetTicketByIdAsync(string userId, Guid ticketId);

        /// <summary>
        /// Lấy danh sách vé của user
        /// </summary>
        Task<List<ElectronicTicketSummaryDto>> GetMyTicketsAsync(string userId);

        /// <summary>
        /// Lấy vé theo mã code
        /// </summary>
        Task<ElectronicTicketDetailDto> GetTicketByCodeAsync(string ticketCode);

        /// <summary>
        /// Lấy danh sách tất cả vé (admin)
        /// </summary>
        Task<List<ElectronicTicketDetailDto>> GetAllTicketsAsync(string? status = null, string? parkingLotName = null);

        /// <summary>
        /// Cập nhật trạng thái thanh toán của vé
        /// </summary>
        Task<bool> UpdatePaymentStatusAsync(Guid ticketId, PaymentConfirmationDto confirmationDto);

        /// <summary>
        /// Cập nhật thông tin checkout (checkout time, fee, status)
        /// </summary>
        Task<bool> UpdateCheckOutInfoAsync(Guid ticketId, DateTime checkOutTime, decimal feeAmount);

        /// <summary>
        /// Xác nhận thanh toán tiền mặt (staff)
        /// </summary>
        Task<bool> ConfirmCashPaymentAsync(Guid ticketId, decimal amount, string staffId);

        /// <summary>
        /// Tìm kiếm vé theo biển số
        /// </summary>
        Task<ElectronicTicketDetailDto> GetTicketByLicensePlateAsync(string licensePlate);

        /// <summary>
        /// Lấy danh sách vé chưa được gửi cho user (Status = Created)
        /// </summary>
        Task<List<ElectronicTicket>> GetUnsentTicketsAsync(int maxAttempts = 3);

        /// <summary>
        /// Cập nhật UserId cho vé (khi match được user từ license plate)
        /// </summary>
        Task<bool> UpdateTicketUserAsync(Guid ticketId, string userId);

        /// <summary>
        /// Đánh dấu vé đã gửi
        /// </summary>
        Task<bool> MarkTicketAsSentAsync(Guid ticketId);
        
        /// <summary>
        /// Xác nhận thanh toán tiền mặt bởi staff - hoàn thành checkout
        /// </summary>
        Task<bool> ConfirmCashPaymentAsync(Guid ticketId, Guid checkInOutId, decimal feeAmount);
    }
}
