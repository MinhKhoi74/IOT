using System.Text.Json.Serialization;

namespace SmartParking.DTOs.Momo
{
    public class MomoCreatePaymentRequestDto
    {
        public string OrderInfo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? OrderId { get; set; }
        public string? RequestId { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string ExtraData { get; set; } = string.Empty;
        /// <summary>
        /// Payment method: "Wallet" (default) or "ATM"
        /// </summary>
        public string PaymentMethod { get; set; } = "Wallet";
    }

    public class MomoCreatePaymentPayloadDto
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string IpnUrl { get; set; } = string.Empty;
        public string Lang { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public bool AutoCapture { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public string OrderGroupId { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        /// <summary>
        /// Payment method limitation: "ATM" or empty (all methods)
        /// </summary>
        public string? Limit { get; set; }
    }

    public class MomoCreatePaymentResultDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public MomoCreatePaymentPayloadDto Payload { get; set; } = new();
        public string Signature { get; set; } = string.Empty;
        public string RawSignature { get; set; } = string.Empty;
        public bool IsSandbox { get; set; }
        public bool IsLiveRequest { get; set; }
        public int? ResultCode { get; set; }
        public string? Message { get; set; }
        public long? ResponseTime { get; set; }
        public string? PayUrl { get; set; }
        public string? Deeplink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? DeeplinkMiniApp { get; set; }
        public string? OrderId { get; set; }
        public string? RequestId { get; set; }
        /// <summary>
        /// Payment method: "Wallet" or "ATM" (informational only, used by client for UI)
        /// </summary>
        public string? PaymentMethod { get; set; }
    }

    public class MomoPaymentNotificationDto
    {
        [JsonPropertyName("partnerCode")]
        public string PartnerCode { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("orderInfo")]
        public string OrderInfo { get; set; } = string.Empty;

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; } = string.Empty;

        [JsonPropertyName("transId")]
        public long TransId { get; set; }

        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("payType")]
        public string PayType { get; set; } = string.Empty;

        [JsonPropertyName("responseTime")]
        public long ResponseTime { get; set; }

        [JsonPropertyName("extraData")]
        public string ExtraData { get; set; } = string.Empty;

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }
}
