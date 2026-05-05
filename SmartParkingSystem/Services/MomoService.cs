using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SmartParking.Configurations;
using SmartParking.DTOs.Momo;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class MomoService : IMomoService
    {
        private readonly MomoSettings _settings;
        private readonly HttpClient _httpClient;

        public MomoService(IConfiguration configuration, HttpClient httpClient)
        {
            _settings = configuration.GetSection("MomoSettings").Get<MomoSettings>() ?? new MomoSettings();
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMilliseconds(Math.Max(_settings.RequestTimeout, 30000));
        }

        public MomoSettings GetSettings() => _settings;

        public List<string> GetAvailablePaymentMethods()
        {
            var methods = new List<string> { "Wallet" };
            if (_settings.EnableAtmPayment)
            {
                methods.Add("ATM");
            }
            return methods;
        }

        public MomoCreatePaymentResultDto BuildCreatePaymentRequest(MomoCreatePaymentRequestDto request)
        {
            var orderId = request.OrderId ?? Guid.NewGuid().ToString("N");
            var requestId = request.RequestId ?? orderId;
            var amount = decimal.ToInt64(decimal.Round(request.Amount, 0, MidpointRounding.AwayFromZero));
            var redirectUrl = string.IsNullOrWhiteSpace(request.RedirectUrl) ? _settings.ReturnUrl : request.RedirectUrl;
            var notifyUrl = string.IsNullOrWhiteSpace(request.NotifyUrl) ? _settings.NotifyUrl : request.NotifyUrl;
            var extraData = request.ExtraData ?? string.Empty;
            
            // Determine request type based on payment method
            var requestType = DetermineRequestType(request.PaymentMethod);
            var limit = DeterminePaymentLimit(request.PaymentMethod);

            var rawSignature =
                $"accessKey={_settings.AccessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={notifyUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={request.OrderInfo}" +
                $"&partnerCode={_settings.PartnerCode}" +
                $"&redirectUrl={redirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={requestType}";
            
            // Note: limit is NOT included in signature, only in payload
            // MoMo API doesn't expect limit parameter in signature calculation

            var signature = SignSha256(rawSignature, _settings.SecretKey);

            var payload = new MomoCreatePaymentPayloadDto
            {
                PartnerCode = _settings.PartnerCode,
                PartnerName = _settings.PartnerName,
                StoreId = _settings.StoreId,
                RequestId = requestId,
                Amount = amount,
                OrderId = orderId,
                OrderInfo = request.OrderInfo,
                RedirectUrl = redirectUrl,
                IpnUrl = notifyUrl,
                Lang = _settings.Lang,
                RequestType = requestType,
                AutoCapture = _settings.AutoCapture,
                ExtraData = extraData,
                OrderGroupId = string.Empty,
                Signature = signature,
                Limit = limit
            };

            return new MomoCreatePaymentResultDto
            {
                Endpoint = _settings.Endpoint,
                Payload = payload,
                Signature = signature,
                RawSignature = rawSignature,
                IsSandbox = _settings.IsSandbox,
                IsLiveRequest = false,
                OrderId = orderId,
                RequestId = requestId,
                PaymentMethod = request.PaymentMethod
            };
        }

        private string DetermineRequestType(string paymentMethod)
        {
            // Use different request types based on payment method
            if (string.Equals(paymentMethod, "ATM", StringComparison.OrdinalIgnoreCase))
            {
                return _settings.AtmRequestType;  // "payWithATM"
            }
            // Default for Wallet and other methods
            return _settings.RequestType;  // "payWithMethod"
        }

        private string? DeterminePaymentLimit(string paymentMethod)
        {
            // Use "limit" parameter to restrict payment methods on Momo's side
            if (string.Equals(paymentMethod, "ATM", StringComparison.OrdinalIgnoreCase))
            {
                return "ATM";  // Only show ATM option
            }
            return null;  // Show all available methods
        }

        public async Task<MomoCreatePaymentResultDto> CreatePaymentAsync(MomoCreatePaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            var built = BuildCreatePaymentRequest(request);
            
            // Build payload with conditional limit field
            var payloadDict = new Dictionary<string, object?>
            {
                { "partnerCode", built.Payload.PartnerCode },
                { "partnerName", built.Payload.PartnerName },
                { "storeId", built.Payload.StoreId },
                { "requestId", built.Payload.RequestId },
                { "amount", built.Payload.Amount },
                { "orderId", built.Payload.OrderId },
                { "orderInfo", built.Payload.OrderInfo },
                { "redirectUrl", built.Payload.RedirectUrl },
                { "ipnUrl", built.Payload.IpnUrl },
                { "lang", built.Payload.Lang },
                { "requestType", built.Payload.RequestType },
                { "autoCapture", built.Payload.AutoCapture },
                { "extraData", built.Payload.ExtraData },
                { "orderGroupId", built.Payload.OrderGroupId },
                { "signature", built.Payload.Signature }
            };
            
            // Add limit only if specified (for ATM payment restriction)
            if (!string.IsNullOrEmpty(built.Payload.Limit))
            {
                payloadDict["limit"] = built.Payload.Limit;
            }

            var payload = payloadDict;

            using var response = await _httpClient.PostAsJsonAsync(built.Endpoint, payload, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"MoMo API returned status {response.StatusCode}: {responseText}",
                    null,
                    response.StatusCode);
            }

            using var json = JsonDocument.Parse(responseText);
            var root = json.RootElement;

            built.IsLiveRequest = true;
            built.ResultCode = TryGetInt(root, "resultCode");
            built.Message = TryGetString(root, "message");
            built.ResponseTime = TryGetLong(root, "responseTime");
            built.PayUrl = TryGetString(root, "payUrl");
            built.Deeplink = TryGetString(root, "deeplink");
            built.QrCodeUrl = TryGetString(root, "qrCodeUrl");
            built.DeeplinkMiniApp = TryGetString(root, "deeplinkMiniApp");
            built.OrderId = TryGetString(root, "orderId") ?? built.OrderId;
            built.RequestId = TryGetString(root, "requestId") ?? built.RequestId;

            return built;
        }

        public bool VerifyNotificationSignature(MomoPaymentNotificationDto notification)
        {
            var rawSignature =
                $"accessKey={_settings.AccessKey}" +
                $"&amount={notification.Amount}" +
                $"&extraData={notification.ExtraData}" +
                $"&message={notification.Message}" +
                $"&orderId={notification.OrderId}" +
                $"&orderInfo={notification.OrderInfo}" +
                $"&orderType={notification.OrderType}" +
                $"&partnerCode={notification.PartnerCode}" +
                $"&payType={notification.PayType}" +
                $"&requestId={notification.RequestId}" +
                $"&responseTime={notification.ResponseTime}" +
                $"&resultCode={notification.ResultCode}" +
                $"&transId={notification.TransId}";

            var expected = SignSha256(rawSignature, _settings.SecretKey);
            return string.Equals(expected, notification.Signature, StringComparison.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> DecodeExtraData(string extraData)
        {
            if (string.IsNullOrWhiteSpace(extraData))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var jsonBytes = Convert.FromBase64String(extraData);
                var json = Encoding.UTF8.GetString(jsonBytes);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ??
                       new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string SignSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string? TryGetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.GetString()
                : null;
        }

        private static int? TryGetInt(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var intValue)
                ? intValue
                : null;
        }

        private static long? TryGetLong(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var longValue)
                ? longValue
                : null;
        }
    }
}
