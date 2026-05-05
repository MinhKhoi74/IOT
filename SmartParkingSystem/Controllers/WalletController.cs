using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.Momo;
using SmartParking.DTOs.Wallet;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IMomoService _momoService;

        public WalletController(IWalletService walletService, IMomoService momoService)
        {
            _walletService = walletService;
            _momoService = momoService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyWallet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return Ok(await _walletService.GetMyWalletAsync(userId));
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int take = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return Ok(await _walletService.GetTransactionsAsync(userId, take));
        }

        [HttpGet("payment-methods")]
        public IActionResult GetPaymentMethods()
        {
            var methods = _momoService.GetAvailablePaymentMethods();
            var settings = _momoService.GetSettings();
            
            return Ok(new
            {
                enabled = settings.IsEnabled,
                methods,
                atmEnabled = settings.EnableAtmPayment,
                environment = settings.Environment
            });
        }

        [HttpPost("top-up/demo")]
        public async Task<IActionResult> DemoTopUp([FromBody] DemoTopUpRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transaction = await _walletService.CreditAsync(
                userId,
                request.Amount,
                "TopUp",
                request.Description ?? "Demo top-up into SmartParking wallet",
                "DemoTopUp",
                Guid.NewGuid().ToString("N"));

            var wallet = await _walletService.GetMyWalletAsync(userId);
            var settings = _momoService.GetSettings();

            return Ok(new WalletTopUpResponseDto
            {
                Wallet = wallet,
                Transaction = _walletService.MapTransaction(transaction),
                MomoSandboxInfo = new MomoSandboxInfoDto
                {
                    Environment = settings.Environment,
                    Endpoint = settings.Endpoint,
                    PartnerCode = settings.PartnerCode,
                    RequestType = settings.RequestType,
                    Note = "Demo top-up credits the internal wallet directly. The sandbox config below is ready for MoMo request signing."
                }
            });
        }

        [HttpPost("top-up/momo")]
        public async Task<IActionResult> CreateMomoTopUp([FromBody] MomoTopUpRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var extraData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["target"] = "wallet-topup",
                    ["userId"] = userId
                })));

            var payment = await _momoService.CreatePaymentAsync(new MomoCreatePaymentRequestDto
            {
                Amount = request.Amount,
                OrderId = $"topup-{userId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                RequestId = $"topup-{Guid.NewGuid():N}",
                OrderInfo = request.Description ?? $"Nap tien vi SmartParking {request.Amount:N0} VND",
                ExtraData = extraData,
                PaymentMethod = request.PaymentMethod ?? "Wallet"
            });

            return Ok(payment);
        }

        [HttpPost("top-up/momo-preview-atm")]
        public IActionResult PreviewAtmPayload([FromBody] MomoTopUpRequestDto request)
        {
            // For testing ATM payment preview without authentication
            var payload = _momoService.BuildCreatePaymentRequest(new MomoCreatePaymentRequestDto
            {
                Amount = request.Amount,
                OrderInfo = request.Description ?? $"Test ATM payment {request.Amount:N0} VND",
                PaymentMethod = "ATM",
                OrderId = $"test-atm-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                RequestId = $"test-atm-{Guid.NewGuid():N}"
            });

            return Ok(new
            {
                paymentMethod = "ATM",
                payload = payload.Payload
            });
        }

        [AllowAnonymous]
        [HttpPost("top-up/momo-debug")]
        public async Task<IActionResult> DebugMomoPayment([FromBody] MomoTopUpRequestDto request)
        {
            // Debug endpoint - shows full request/response
            var userId = "debug-user";
            var extraData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["target"] = "wallet-topup",
                    ["userId"] = userId
                })));

            // Try different payment methods: Wallet (all methods), ATM (ATM only)
            var results = new List<object>();

            foreach (var method in new[] { "Wallet", "ATM" })
            {
                try
                {
                    var createRequest = new MomoCreatePaymentRequestDto
                    {
                        Amount = request.Amount,
                        OrderId = $"debug-{method}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                        RequestId = $"debug-{Guid.NewGuid():N}",
                        OrderInfo = $"Test {method} - {request.Amount:N0} VND",
                        ExtraData = extraData,
                        PaymentMethod = method
                    };

                    var built = _momoService.BuildCreatePaymentRequest(createRequest);
                    var payment = await _momoService.CreatePaymentAsync(createRequest);

                    results.Add(new
                    {
                        method,
                        requestType = built.Payload.RequestType,
                        limit = built.Payload.Limit,
                        resultCode = payment.ResultCode,
                        message = payment.Message,
                        payUrl = payment.PayUrl,
                        qrCodeUrl = payment.QrCodeUrl
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new { method, error = ex.Message });
                }
            }

            return Ok(new { availableMethods = results });
        }

        [HttpPost("top-up/momo-preview")]
        public IActionResult PreviewMomoPayload([FromBody] MomoTopUpRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var payload = _momoService.BuildCreatePaymentRequest(new MomoCreatePaymentRequestDto
            {
                Amount = request.Amount,
                OrderInfo = $"Top up SmartParking wallet for user {userId}",
                PaymentMethod = request.PaymentMethod ?? "Wallet"
            });

            return Ok(new
            {
                payload = payload.Payload,
                note = "This shows the payload that will be sent to Momo API. Check 'limit' field - it should be 'ATM' for ATM payments.",
                paymentMethod = request.PaymentMethod ?? "Wallet"
            });
        }
    }
}
