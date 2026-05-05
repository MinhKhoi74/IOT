using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.DTOs.Momo;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/payments/momo")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMomoService _momoService;
        private readonly ICheckOutService _checkOutService;
        private readonly IWalletService _walletService;
        private readonly ApplicationDBContext _context;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IMomoService momoService,
            ICheckOutService checkOutService,
            IWalletService walletService,
            ApplicationDBContext context,
            ILogger<PaymentsController> logger)
        {
            _momoService = momoService;
            _checkOutService = checkOutService;
            _walletService = walletService;
            _context = context;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("ipn")]
        public async Task<IActionResult> ReceiveIpn([FromBody] MomoPaymentNotificationDto notification)
        {
            try
            {
                if (!_momoService.VerifyNotificationSignature(notification))
                {
                    _logger.LogWarning("MoMo IPN signature invalid for order {OrderId}", notification.OrderId);
                    return NoContent();
                }

                if (notification.ResultCode != 0)
                {
                    _logger.LogInformation("MoMo IPN non-success result for order {OrderId}: {ResultCode}", notification.OrderId, notification.ResultCode);
                    return NoContent();
                }

                var extraData = _momoService.DecodeExtraData(notification.ExtraData);
                if (!extraData.TryGetValue("target", out var target))
                {
                    _logger.LogWarning("MoMo IPN missing target in extraData for order {OrderId}", notification.OrderId);
                    return NoContent();
                }

                if (string.Equals(target, "checkout", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleCheckoutPaymentAsync(extraData);
                }
                else if (string.Equals(target, "wallet-topup", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleWalletTopUpAsync(notification, extraData);
                }
                else
                {
                    _logger.LogWarning("MoMo IPN target not supported: {Target}", target);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MoMo IPN for order {OrderId}", notification.OrderId);
            }

            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("return")]
        public IActionResult Return([FromQuery] string? orderId, [FromQuery] int? resultCode, [FromQuery] string? message)
        {
            return Ok(new
            {
                success = resultCode == 0,
                orderId,
                resultCode,
                message,
                note = "Redirect tu MoMo sau khi nguoi dung hoan tat thanh toan. Trang thai chinh thuc nen dua vao IPN."
            });
        }

        [AllowAnonymous]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(
            [FromQuery] string target,
            [FromQuery] int? checkOutId,
            [FromQuery] string? orderId)
        {
            if (string.Equals(target, "checkout", StringComparison.OrdinalIgnoreCase))
            {
                if (checkOutId == null)
                {
                    return BadRequest(new { message = "checkOutId is required" });
                }

                var session = await _context.CheckInOuts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == checkOutId.Value);

                if (session == null)
                {
                    return NotFound(new { message = $"Khong tim thay checkout {checkOutId}" });
                }

                return Ok(new
                {
                    target = "checkout",
                    checkOutId = session.Id,
                    plate = session.LicensePlate,
                    amount = session.FeeAmount,
                    durationMinutes = session.DurationMinutes,
                    paymentStatus = session.PaymentStatus,
                    paymentMethod = session.PaymentMethod,
                    paidAt = session.PaidAt,
                    status = session.Status,
                    isPaid = string.Equals(session.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                });
            }

            if (string.Equals(target, "wallet-topup", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return BadRequest(new { message = "orderId is required" });
                }

                var transaction = await _context.WalletTransactions
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(x => x.ReferenceType == "MomoTopUp" && x.ReferenceId == orderId);

                if (transaction == null)
                {
                    return Ok(new
                    {
                        target = "wallet-topup",
                        orderId,
                        isPaid = false,
                        paymentStatus = "Pending"
                    });
                }

                return Ok(new
                {
                    target = "wallet-topup",
                    orderId,
                    isPaid = true,
                    paymentStatus = "Paid",
                    amount = transaction.Amount,
                    createdAt = transaction.CreatedAt,
                    balanceAfter = transaction.BalanceAfter
                });
            }

            return BadRequest(new { message = "target is invalid" });
        }

        private async Task HandleCheckoutPaymentAsync(Dictionary<string, string> extraData)
        {
            if (!extraData.TryGetValue("checkOutId", out var checkOutIdText) || !int.TryParse(checkOutIdText, out var checkOutId))
            {
                return;
            }

            var checkout = await _context.CheckInOuts.FirstOrDefaultAsync(x => x.Id == checkOutId);
            if (checkout == null || string.Equals(checkout.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _checkOutService.ConfirmPendingPaymentAsync(checkOutId, new ConfirmCheckOutPaymentRequest
            {
                PaymentMethod = "Momo"
            });
        }

        private async Task HandleWalletTopUpAsync(MomoPaymentNotificationDto notification, Dictionary<string, string> extraData)
        {
            if (!extraData.TryGetValue("userId", out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var alreadyCredited = await _context.WalletTransactions.AnyAsync(x =>
                x.ReferenceType == "MomoTopUp" &&
                x.ReferenceId == notification.OrderId);

            if (alreadyCredited)
            {
                return;
            }

            await _walletService.CreditAsync(
                userId,
                notification.Amount,
                "TopUp",
                $"Nap tien qua MoMo cho don {notification.OrderId}",
                "MomoTopUp",
                notification.OrderId);
        }
    }
}
