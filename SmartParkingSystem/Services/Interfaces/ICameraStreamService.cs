namespace SmartParking.Services.Interfaces;

public record CameraStartRequest(
    string CameraIp,
    int CameraPort,
    string ApiHost,
    int ApiPort,
    string StationMode,
    string? BackendToken
);

public record ZoneCameraStartRequest(
    string CameraIp,
    int CameraPort,
    string ApiHost,
    int ApiPort,
    string CameraId,
    string LocationName,
    string? ParkingLotCode,
    string? ZoneCode,
    string? ColumnCode,
    string? BackendToken
);

public record CameraStreamStatus(
    bool IsRunning,
    int? ProcessId,
    string StreamUrl,
    string DetectionUrl,
    string HealthUrl,
    string Message
);

public interface ICameraStreamService
{
    Task<CameraStreamStatus> StartAsync(CameraStartRequest request, CancellationToken cancellationToken = default);
    Task<CameraStreamStatus> StopAsync(CancellationToken cancellationToken = default);
    Task<CameraStreamStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CameraStreamStatus> StartZoneAsync(ZoneCameraStartRequest request, CancellationToken cancellationToken = default);
    Task<CameraStreamStatus> StopZoneAsync(string cameraId, int apiPort, CancellationToken cancellationToken = default);
    Task<CameraStreamStatus> GetZoneStatusAsync(string cameraId, int apiPort, CancellationToken cancellationToken = default);
}
