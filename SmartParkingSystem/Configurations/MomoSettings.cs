namespace SmartParking.Configurations
{
    public class MomoSettings
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public int RequestTimeout { get; set; } = 3000;
        public int RetryAttempts { get; set; } = 1;
        public bool IsEnabled { get; set; }
        public string Environment { get; set; } = "Test";
        public string NotifyUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string PartnerName { get; set; } = "SmartParking";
        public string StoreId { get; set; } = "SmartParking";
        public string Lang { get; set; } = "vi";
        public bool AutoCapture { get; set; } = true;
        public string RequestType { get; set; } = "captureWallet";
        /// <summary>
        /// Request type for ATM payment: "payWithATM"
        /// </summary>
        public string AtmRequestType { get; set; } = "payWithATM";
        /// <summary>
        /// Enable ATM payment option
        /// </summary>
        public bool EnableAtmPayment { get; set; } = false;

        public bool IsSandbox =>
            string.Equals(Environment, "Test", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment, "Sandbox", StringComparison.OrdinalIgnoreCase);
    }
}
