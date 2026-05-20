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
        private readonly IVehicleLocationService _vehicleLocationService;
        private readonly IBranchAuthorizationService _branchAuthorizationService;
        private readonly ILogger<ParkingController> _logger;

        public ParkingController(
            ICheckInService checkInService,
            ICheckOutService checkOutService,
            IParkingHistoryService parkingHistoryService,
            IVehicleLocationService vehicleLocationService,
            IBranchAuthorizationService branchAuthorizationService,
            ILogger<ParkingController> logger)
        {
            _checkInService = checkInService;
            _checkOutService = checkOutService;
            _parkingHistoryService = parkingHistoryService;
            _vehicleLocationService = vehicleLocationService;
            _branchAuthorizationService = branchAuthorizationService;
            _logger = logger;
        }

        [HttpPost("check-in")]
        [Authorize(Roles = "Staff,Admin,Manager")]
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

            request.BranchId = await ResolveBranchScopeAsync(request.BranchId);
            var result = await _checkInService.ProcessCheckInAsync(request);

            _logger.LogInformation(
                "[CheckIn] Result - Success: {Success}, ErrorCode: {ErrorCode}, Message: {Message}",
                result.Success,
                result.ErrorCode,
                result.Message);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("check-out")]
        [Authorize(Roles = "Staff,Admin,Manager")]
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

            request.BranchId = await ResolveBranchScopeAsync(request.BranchId);
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
        [Authorize(Roles = "Staff,Admin,Manager")]
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

            var branchScope = await ResolveBranchScopeAsync();
            if (branchScope.HasValue)
            {
                var session = await _parkingHistoryService.GetSessionDetailAsync(checkOutId, branchScope);
                if (session is null)
                    return NotFound(new { message = "Parking session not found in your branch" });
            }

            var result = await _checkOutService.ConfirmPendingPaymentAsync(checkOutId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("history/{plate}")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetCheckInOutHistory(string plate)
        {
            if (string.IsNullOrEmpty(plate))
            {
                return BadRequest(new { message = "Bien so xe khong duoc de trong" });
            }

            return Ok(await _parkingHistoryService.GetHistoryByPlateAsync(plate, await ResolveBranchScopeAsync()));
        }

        [HttpGet("history")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string? plateNumber = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                var history = await _parkingHistoryService.GetHistoryAsync(await ResolveBranchScopeAsync());
                
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
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLatestCheckIn([FromQuery] Guid? branchId = null)
        {
            var latest = await _parkingHistoryService.GetLatestCheckInAsync(await ResolveBranchScopeAsync(branchId));
            return latest is null ? NotFound(new { message = "No check-in records found" }) : Ok(latest);
        }

        [HttpGet("latest-check-out")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLatestCheckOut([FromQuery] Guid? branchId = null)
        {
            var latest = await _parkingHistoryService.GetLatestCheckOutAsync(await ResolveBranchScopeAsync(branchId));
            return latest is null ? NotFound(new { message = "No check-out records found" }) : Ok(latest);
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDashboard([FromQuery] Guid? branchId = null)
        {
            return Ok(await _parkingHistoryService.GetDashboardAsync(await ResolveBranchScopeAsync(branchId)));
        }

        [HttpGet("history/{id:int}/detail")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetSessionDetail(int id)
        {
            var session = await _parkingHistoryService.GetSessionDetailAsync(id, await ResolveBranchScopeAsync());
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

        [HttpPost("location-detection")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(VehicleLocationDetectionResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessLocationDetection([FromBody] VehicleLocationDetectionRequest request)
        {
            if (!ModelState.IsValid)
            {
                LogValidationErrors("LocationDetection");
                return ValidationProblem(ModelState);
            }

            request.BranchId = await ResolveBranchScopeAsync(request.BranchId);
            var result = await _vehicleLocationService.ProcessDetectionAsync(request);
            return Ok(result);
        }

        [HttpPost("location-detections/batch")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(VehicleLocationDetectionBatchResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessLocationDetectionBatch([FromBody] VehicleLocationDetectionBatchRequest request)
        {
            if (!ModelState.IsValid)
            {
                LogValidationErrors("LocationDetectionBatch");
                return ValidationProblem(ModelState);
            }

            request.BranchId = await ResolveBranchScopeAsync(request.BranchId);
            var result = await _vehicleLocationService.ProcessDetectionBatchAsync(request);
            return Ok(result);
        }

        [HttpGet("vehicle-locations/me")]
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> GetMyVehicleLocations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            return Ok(await _vehicleLocationService.GetMyLatestLocationsAsync(userId));
        }

        [HttpGet("vehicle-location-alerts")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetVehicleLocationAlerts([FromQuery] int take = 50)
        {
            return Ok(await _vehicleLocationService.GetRecentAlertsAsync(take, await ResolveBranchScopeAsync()));
        }

        [HttpGet("vehicle-location-alerts/{id:int}")]
        [Authorize(Roles = "Staff,Admin,Manager")]
        [Produces("application/json")]
        public async Task<IActionResult> GetVehicleLocationAlert(int id)
        {
            var alert = await _vehicleLocationService.GetLocationAlertAsync(id, await ResolveBranchScopeAsync());
            return alert is null ? NotFound(new { message = "Vehicle location alert not found" }) : Ok(alert);
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

        private async Task<Guid?> ResolveBranchScopeAsync(Guid? requestedBranchId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User not found");

            return await _branchAuthorizationService.GetBranchScopeAsync(userId, User.IsInRole("Admin"), requestedBranchId);
        }
    }
}
