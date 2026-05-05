using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers;

[ApiController]
[Route("api/camera")]
[Authorize(Roles = "Staff")]
public class CameraController : ControllerBase
{
    private readonly ICameraStreamService _cameraStreamService;

    public CameraController(ICameraStreamService cameraStreamService)
    {
        _cameraStreamService = cameraStreamService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] CameraStartHttpRequest request, CancellationToken cancellationToken)
    {
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

    private string? GetBearerToken()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";

        return authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authHeader[bearerPrefix.Length..].Trim()
            : null;
    }
}

public class CameraStartHttpRequest
{
    public string CameraIp { get; set; } = string.Empty;
    public int CameraPort { get; set; }
    public string ApiHost { get; set; } = "localhost";
    public int ApiPort { get; set; } = 5001;
    public string StationMode { get; set; } = "entrance";
}
