using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/parking")]
    public class ParkingController : ControllerBase
    {
        private readonly ICheckInService _checkInService;
        private readonly ICheckOutService _checkOutService;
        private readonly IParkingHistoryService _parkingHistoryService;
        private readonly ILogger<ParkingController> _logger;

        public ParkingController(
            ICheckInService checkInService,
            ICheckOutService checkOutService,
            IParkingHistoryService parkingHistoryService,
            ILogger<ParkingController> logger)
        {
            _checkInService = checkInService;
            _checkOutService = checkOutService;
            _parkingHistoryService = parkingHistoryService;
            _logger = logger;
        }

        [HttpPost("check-in")]
        [Authorize(Roles = "Staff,Admin")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CheckInResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CheckInResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            if (!ModelState.IsValid)
            {
                LogValidationErrors("CheckIn");
                return ValidationProblem(ModelState);
            }

            _logger.LogInformation(
                "[CheckIn] Received - Plate: {Plate}, Station: {Station}, Confidence: {Confidence}",
                request.PlateNumber,
                request.StationId,
                request.Confidence);

            var result = await _checkInService.ProcessCheckInAsync(request);

            _logger.LogInformation(
                "[CheckIn] Result - Success: {Success}, ErrorCode: {ErrorCode}, Message: {Message}",
                result.Success,
                result.ErrorCode,
                result.Message);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("check-out")]
        [Authorize(Roles = "Staff,Admin")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CheckOutResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CheckOutResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
        {
            if (!ModelState.IsValid)
            {
                LogValidationErrors("CheckOut");
                return ValidationProblem(ModelState);
            }

            _logger.LogInformation(
                "[CheckOut] Received - Plate: {Plate}, Station: {Station}",
                request.PlateNumber,
                request.StationId);

            var result = await _checkOutService.ProcessCheckOutAsync(request);

            _logger.LogInformation(
                "[CheckOut] Result - Success: {Success}, ErrorCode: {ErrorCode}, Message: {Message}",
                result.Success,
                result.ErrorCode,
                result.Message);

            var shouldReturnOk =
                result.Success ||
                result.RequiresPaymentAction ||
                string.Equals(result.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase);

            return shouldReturnOk ? Ok(result) : BadRequest(result);
        }

        [HttpPost("check-out/{checkOutId:int}/confirm-payment")]
        [Authorize(Roles = "Staff,Admin")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CheckOutResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CheckOutResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmCheckOutPayment(int checkOutId, [FromBody] ConfirmCheckOutPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                LogValidationErrors("ConfirmCheckOutPayment");
                return ValidationProblem(ModelState);
            }

            var result = await _checkOutService.ConfirmPendingPaymentAsync(checkOutId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("history/{plate}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetCheckInOutHistory(string plate)
        {
            if (string.IsNullOrEmpty(plate))
            {
                return BadRequest(new { message = "Bien so xe khong duoc de trong" });
            }

            return Ok(await _parkingHistoryService.GetHistoryByPlateAsync(plate));
        }

        [HttpGet("history")]
        [Produces("application/json")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string? plateNumber = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var history = await _parkingHistoryService.GetMyHistoryAsync(userId);
                
                // Apply filters
                if (!string.IsNullOrEmpty(plateNumber))
                    history = history.Where(h => h.LicensePlate.Contains(plateNumber, StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
                    history = history.Where(h => h.CheckInTime >= start).ToList();
                
                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
                    history = history.Where(h => h.CheckInTime <= end.AddDays(1)).ToList();
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("latest-check-in")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLatestCheckIn()
        {
            var latest = await _parkingHistoryService.GetLatestCheckInAsync();
            return latest is null ? NotFound(new { message = "No check-in records found" }) : Ok(latest);
        }

        [HttpGet("latest-check-out")]
        [Authorize(Roles = "Staff,Admin")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLatestCheckOut()
        {
            var latest = await _parkingHistoryService.GetLatestCheckOutAsync();
            return latest is null ? NotFound(new { message = "No check-out records found" }) : Ok(latest);
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "Staff,Admin")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDashboard()
        {
            return Ok(await _parkingHistoryService.GetDashboardAsync());
        }

        [HttpGet("history/{id:int}/detail")]
        [Authorize(Roles = "Staff,Admin")]
        [Produces("application/json")]
        public async Task<IActionResult> GetSessionDetail(int id)
        {
            var session = await _parkingHistoryService.GetSessionDetailAsync(id);
            return session is null ? NotFound(new { message = "Parking session not found" }) : Ok(session);
        }

        [HttpGet("history/me")]
        [Produces("application/json")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return Ok(await _parkingHistoryService.GetMyHistoryAsync(userId));
        }

        private void LogValidationErrors(string actionName)
        {
            var validationErrors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            foreach (var (field, errors) in validationErrors)
            {
                _logger.LogError("[{Action}] Validation error on {Field}: {Errors}", actionName, field, string.Join(" | ", errors));
            }
        }
    }
}
