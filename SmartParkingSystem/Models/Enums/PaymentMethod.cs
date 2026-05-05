namespace SmartParking.Models.Enums
{
    public enum PaymentMethod
    {
        /// <summary>
        /// Chưa chọn phương thức thanh toán
        /// </summary>
        None = 0,

        /// <summary>
        /// Thanh toán bằng tiền mặt
        /// </summary>
        Cash = 1,

        /// <summary>
        /// Thanh toán bằng ví điện tử
        /// </summary>
        Wallet = 2
    }
}
