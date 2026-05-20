using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using SmartParking.DTOs;
using SmartParking.DTOs.Momo;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/monthly-passes")]
    [Authorize]
    public class MonthlyPassesController : ControllerBase
    {
        private readonly IMonthlyPassService _monthlyPassService;
        private readonly IMomoService _momoService;

        public MonthlyPassesController(IMonthlyPassService monthlyPassService, IMomoService momoService)
        {
            _monthlyPassService = monthlyPassService;
            _momoService = momoService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _monthlyPassService.GetAllAsync());
        }

        [HttpGet("price")]
        public async Task<IActionResult> GetPrice()
        {
            return Ok(new MonthlyPassPriceDto
            {
                MonthlyAmount = await _monthlyPassService.GetMonthlyAmountAsync()
            });
        }

        [HttpPut("price")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePrice([FromBody] MonthlyPassPriceDto request)
        {
            try
            {
                var monthlyAmount = await _monthlyPassService.SetMonthlyAmountAsync(request.MonthlyAmount);
                return Ok(new MonthlyPassPriceDto { MonthlyAmount = monthlyAmount });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upsert([FromBody] MonthlyPassUpsertRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                return Ok(await _monthlyPassService.UpsertAsync(request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterForCurrentUser([FromBody] MonthlyPassUpsertRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }

                return Ok(await _monthlyPassService.RegisterForUserAsync(userId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("momo")]
        public async Task<IActionResult> CreateMomoPayment([FromBody] MonthlyPassMomoPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var amount = await _monthlyPassService.CalculateAmountAsync(request.ValidFrom, request.ValidTo);
            request.Amount = amount;

            var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["target"] = "monthly-pass",
                    ["userId"] = userId,
                    ["licensePlate"] = request.LicensePlate.Trim().ToUpperInvariant(),
                    ["ownerName"] = request.OwnerName.Trim(),
                    ["ownerPhone"] = request.OwnerPhone?.Trim() ?? string.Empty,
                    ["validFrom"] = request.ValidFrom.ToString("O"),
                    ["validTo"] = request.ValidTo.ToString("O"),
                    ["amount"] = amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                })));

            var payment = await _momoService.CreatePaymentAsync(new MomoCreatePaymentRequestDto
            {
                Amount = amount,
                OrderId = $"monthly-{userId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                RequestId = $"monthly-{Guid.NewGuid():N}",
                OrderInfo = $"Mua ve thang SmartParking cho xe {request.LicensePlate.Trim().ToUpperInvariant()}",
                ExtraData = extraData,
                PaymentMethod = request.PaymentMethod ?? "Wallet"
            });

            return Ok(payment);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _monthlyPassService.DeleteAsync(id);
            return NoContent();
        }
    }
}
