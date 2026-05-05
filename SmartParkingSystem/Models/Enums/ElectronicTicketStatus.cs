namespace SmartParking.Models.Enums
{
    public enum ElectronicTicketStatus
    {
        /// <summary>
        /// Vừa tạo, chưa gửi cho user
        /// </summary>
        Created = 0,

        /// <summary>
        /// Đã gửi vé cho user thông qua ứng dụng
        /// </summary>
        Sent = 1,

        /// <summary>
        /// Chờ thanh toán
        /// </summary>
        Pending = 2,

        /// <summary>
        /// Ví không đủ số dư, cần thanh toán bằng tiền mặt
        /// </summary>
        PartialPayment = 3,

        /// <summary>
        /// Đã thanh toán bằng tiền mặt
        /// </summary>
        PaidCash = 4,

        /// <summary>
        /// Đã thanh toán bằng ví
        /// </summary>
        PaidWallet = 5,

        /// <summary>
        /// Hoàn thành (đã checkout)
        /// </summary>
        Completed = 6
    }
}
