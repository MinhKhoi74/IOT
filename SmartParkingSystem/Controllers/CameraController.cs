using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers;

[ApiController]
[Route("api/camera")]
[Authorize(Roles = "Staff,Admin,Manager")]
public class CameraController : ControllerBase
{
    private readonly ICameraStreamService _cameraStreamService;
    private readonly IBranchAuthorizationService _branchAuthorizationService;

    public CameraController(
        ICameraStreamService cameraStreamService,
        IBranchAuthorizationService branchAuthorizationService)
    {
        _cameraStreamService = cameraStreamService;
        _branchAuthorizationService = branchAuthorizationService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] CameraStartHttpRequest request, CancellationToken cancellationToken)
    {
        var branchId = await ResolveBranchScopeAsync(request.BranchId);
        var stationMode = string.Equals(request.StationMode, "exit", StringComparison.OrdinalIgnoreCase)
            ? "exit"
            : "entrance";

        var status = await _cameraStreamService.StartAsync(
            new CameraStartRequest(
                request.CameraIp,
                request.CameraPort,
                request.ApiHost,
                request.ApiPort,
                stationMode,
                branchId,
                GetBearerToken()
            ),
            cancellationToken
        );

        return Ok(status);
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(CancellationToken cancellationToken)
    {
        var status = await _cameraStreamService.StopAsync(cancellationToken);
        return Ok(status);
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var status = await _cameraStreamService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("zone/start")]
    public async Task<IActionResult> StartZone([FromBody] ZoneCameraStartHttpRequest request, CancellationToken cancellationToken)
    {
        var branchId = await ResolveBranchScopeAsync(request.BranchId);
        var status = await _cameraStreamService.StartZoneAsync(
            new ZoneCameraStartRequest(
                request.CameraIp,
                request.CameraPort,
                request.ApiHost,
                request.ApiPort,
                request.CameraId,
                request.LocationName,
                request.ParkingLotCode,
                request.ZoneCode,
                request.ColumnCode,
                branchId,
                GetBearerToken()
            ),
            cancellationToken
        );

        return Ok(status);
    }

    [HttpPost("zone/stop")]
    public async Task<IActionResult> StopZone([FromBody] ZoneCameraStopHttpRequest request, CancellationToken cancellationToken)
    {
        var status = await _cameraStreamService.StopZoneAsync(request.CameraId, request.ApiPort, cancellationToken);
        return Ok(status);
    }

    [HttpGet("zone/status")]
    public async Task<IActionResult> ZoneStatus([FromQuery] string cameraId, [FromQuery] int apiPort, CancellationToken cancellationToken)
    {
        var status = await _cameraStreamService.GetZoneStatusAsync(cameraId, apiPort, cancellationToken);
        return Ok(status);
    }

    private string? GetBearerToken()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";

        return authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authHeader[bearerPrefix.Length..].Trim()
            : null;
    }

    private async Task<Guid?> ResolveBranchScopeAsync(Guid? requestedBranchId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User not found");

        return await _branchAuthorizationService.GetBranchScopeAsync(userId, User.IsInRole("Admin"), requestedBranchId);
    }
}

public class CameraStartHttpRequest
{
    public string CameraIp { get; set; } = string.Empty;
    public int CameraPort { get; set; }
    public string ApiHost { get; set; } = "localhost";
    public int ApiPort { get; set; } = 5001;
    public string StationMode { get; set; } = "entrance";
    public Guid? BranchId { get; set; }
}

public class ZoneCameraStartHttpRequest
{
    public string CameraIp { get; set; } = string.Empty;
    public int CameraPort { get; set; }
    public string ApiHost { get; set; } = "localhost";
    public int ApiPort { get; set; } = 5101;
    public string CameraId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? ParkingLotCode { get; set; }
    public string? ZoneCode { get; set; }
    public string? ColumnCode { get; set; }
    public Guid? BranchId { get; set; }
}

public class ZoneCameraStopHttpRequest
{
    public string CameraId { get; set; } = string.Empty;
    public int ApiPort { get; set; }
}
