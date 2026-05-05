using SmartParking.Models.Enums;
using SmartParking.Models.Identity;

namespace SmartParking.Models
{
    public class ElectronicTicket
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Biển số xe
        /// </summary>
        public string LicensePlate { get; set; }

        /// <summary>
        /// Mã vé duy nhất (ví dụ: ETICKET-20260425-001)
        /// </summary>
        public string TicketCode { get; set; }

        /// <summary>
        /// Thời gian check-in
        /// </summary>
        public DateTime CheckInDateTime { get; set; }

        /// <summary>
        /// Thời gian check-out (nullable - chưa checkout)
        /// </summary>
        public DateTime? CheckOutDateTime { get; set; }

        /// <summary>
        /// Tên bãi đỗ xe
        /// </summary>
        public string ParkingLotName { get; set; }

        /// <summary>
        /// Tên chi nhánh
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// ID của user (nullable - sẽ được match bằng license plate sau)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Số tiền phải trả
        /// </summary>
        public decimal? FeeAmount { get; set; }

        /// <summary>
        /// Trạng thái vé
        /// </summary>
        public ElectronicTicketStatus Status { get; set; }

        /// <summary>
        /// Phương thức thanh toán
        /// </summary>
        public PaymentMethod? PaymentMethod { get; set; }

        /// <summary>
        /// Thời gian tạo vé
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Thời gian gửi vé đến user
        /// </summary>
        public DateTime? SentToUserAt { get; set; }

        /// <summary>
        /// Số lần cố gắng match license plate với user
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// Navigation property - User đã đăng ký xe
        /// </summary>
        public ApplicationUser? User { get; set; }
    }
}
