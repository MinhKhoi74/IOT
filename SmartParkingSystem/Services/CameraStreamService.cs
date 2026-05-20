using System.Diagnostics;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services;

public class CameraStreamService : ICameraStreamService, IDisposable
{
    private readonly ILogger<CameraStreamService> _logger;
    private readonly string _solutionRoot;
    private readonly string _pythonProjectDir;
    private readonly string _pythonScriptPath;
    private readonly string _zonePythonScriptPath;
    private readonly string _pythonExecutable;
    private readonly object _sync = new();
    private readonly Dictionary<string, ZoneCameraProcessState> _zoneCameraProcesses = new();
    private Process? _cameraProcess;
    private CameraStartRequest? _lastRequest;

    public CameraStreamService(ILogger<CameraStreamService> logger)
    {
        _logger = logger;
        _solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        _pythonProjectDir = Path.Combine(_solutionRoot, "License-Plate-Recognition-main");
        _pythonScriptPath = Path.Combine(_pythonProjectDir, "webcam_smart_lowlatency.py");
        _zonePythonScriptPath = Path.Combine(_pythonProjectDir, "webcam_zone_locator.py");
        _pythonExecutable = ResolvePythonExecutable();
    }

    public async Task<CameraStreamStatus> StartAsync(CameraStartRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        lock (_sync)
        {
            if (_cameraProcess is { HasExited: false } && RequestMatches(_lastRequest, request))
            {
                return BuildStatus("Camera service is already running.");
            }
        }

        await StopAsync(cancellationToken);
        KillOrphanCameraProcesses();

        if (!File.Exists(_pythonScriptPath))
        {
            throw new FileNotFoundException("Camera script not found.", _pythonScriptPath);
        }

        var args =
            $"\"{_pythonScriptPath}\" --ip {request.CameraIp}:{request.CameraPort} --api-server --api-host 0.0.0.0 --api-port {request.ApiPort} --headless --station {request.StationMode} --jpeg-quality 60 --stream-fps 8";

        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            Arguments = args,
            WorkingDirectory = _pythonProjectDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (!string.IsNullOrWhiteSpace(request.BackendToken))
        {
            startInfo.Environment["SMARTPARKING_API_TOKEN"] = request.BackendToken;
        }

        if (request.BranchId.HasValue)
        {
            startInfo.Environment["SMARTPARKING_BRANCH_ID"] = request.BranchId.Value.ToString();
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogInformation("[CameraAI] {Message}", e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogWarning("[CameraAI] {Message}", e.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start camera process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        lock (_sync)
        {
            _cameraProcess = process;
            _lastRequest = request;
        }

        var healthUrl = BuildHealthUrl(request);
        var healthy = await WaitForHealthAsync(healthUrl, process, cancellationToken);
        if (!healthy)
        {
            var exitCode = process.HasExited ? $" Exit code: {process.ExitCode}." : string.Empty;
            CleanupFailedStart(process);
            throw new InvalidOperationException(
                $"Camera service did not become ready at {healthUrl}.{exitCode}"
            );
        }

        return BuildStatus("Camera service started successfully.");
    }

    public Task<CameraStreamStatus> StopAsync(CancellationToken cancellationToken = default)
    {
        Process? processToStop;

        lock (_sync)
        {
            processToStop = _cameraProcess;
            _cameraProcess = null;
            _lastRequest = null;
        }

        if (processToStop is null)
        {
            return Task.FromResult(new CameraStreamStatus(
                false,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                "Camera service is already stopped."
            ));
        }

        try
        {
            if (!processToStop.HasExited)
            {
                processToStop.Kill(entireProcessTree: true);
                processToStop.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed while stopping camera process.");
        }
        finally
        {
            processToStop.Dispose();
        }

        return Task.FromResult(new CameraStreamStatus(
            false,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            "Camera service stopped."
        ));
    }

    public async Task<CameraStreamStatus> StartZoneAsync(ZoneCameraStartRequest request, CancellationToken cancellationToken = default)
    {
        ValidateZoneRequest(request);

        var key = BuildZoneKey(request.CameraId, request.ApiPort);
        lock (_sync)
        {
            if (_zoneCameraProcesses.TryGetValue(key, out var existing) &&
                existing.Process is { HasExited: false } &&
                ZoneRequestMatches(existing.Request, request))
            {
                return BuildZoneStatus(existing, "Zone camera service is already running.");
            }
        }

        await StopZoneAsync(request.CameraId, request.ApiPort, cancellationToken);
        KillOrphanZoneCameraProcesses(request.ApiPort);

        if (!File.Exists(_zonePythonScriptPath))
        {
            throw new FileNotFoundException("Zone camera script not found.", _zonePythonScriptPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonExecutable,
            WorkingDirectory = _pythonProjectDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add(_zonePythonScriptPath);
        startInfo.ArgumentList.Add("--ip");
        startInfo.ArgumentList.Add($"{request.CameraIp}:{request.CameraPort}");
        startInfo.ArgumentList.Add("--camera-id");
        startInfo.ArgumentList.Add(request.CameraId);
        AddOptionalArgument(startInfo, "--parking-lot", request.ParkingLotCode);
        AddOptionalArgument(startInfo, "--zone", request.ZoneCode);
        AddOptionalArgument(startInfo, "--column", request.ColumnCode);
        startInfo.ArgumentList.Add("--location-name");
        startInfo.ArgumentList.Add(request.LocationName);
        startInfo.ArgumentList.Add("--api-server");
        startInfo.ArgumentList.Add("--api-host");
        startInfo.ArgumentList.Add("0.0.0.0");
        startInfo.ArgumentList.Add("--api-port");
        startInfo.ArgumentList.Add(request.ApiPort.ToString());
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--jpeg-quality");
        startInfo.ArgumentList.Add("60");
        startInfo.ArgumentList.Add("--stream-fps");
        startInfo.ArgumentList.Add("8");

        if (!string.IsNullOrWhiteSpace(request.BackendToken))
        {
            startInfo.Environment["SMARTPARKING_API_TOKEN"] = request.BackendToken;
        }

        if (request.BranchId.HasValue)
        {
            startInfo.Environment["SMARTPARKING_BRANCH_ID"] = request.BranchId.Value.ToString();
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogInformation("[ZoneCameraAI:{CameraId}] {Message}", request.CameraId, e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                _logger.LogWarning("[ZoneCameraAI:{CameraId}] {Message}", request.CameraId, e.Data);
            }
        };
        process.Exited += (_, _) =>
        {
            lock (_sync)
            {
                if (_zoneCameraProcesses.TryGetValue(key, out var state) && state.Process.Id == process.Id)
                {
                    _zoneCameraProcesses.Remove(key);
                }
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start zone camera process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var zoneState = new ZoneCameraProcessState(process, request);
        lock (_sync)
        {
            _zoneCameraProcesses[key] = zoneState;
        }

        var healthUrl = BuildZoneHealthUrl(request);
        var healthy = await WaitForHealthAsync(healthUrl, process, cancellationToken);
        if (!healthy)
        {
            var exitCode = process.HasExited ? $" Exit code: {process.ExitCode}." : string.Empty;
            CleanupFailedZoneStart(key, process);
            throw new InvalidOperationException(
                $"Zone camera service did not become ready at {healthUrl}.{exitCode}"
            );
        }

        return BuildZoneStatus(zoneState, "Zone camera service started successfully.");
    }

    public Task<CameraStreamStatus> StopZoneAsync(string cameraId, int apiPort, CancellationToken cancellationToken = default)
    {
        var key = BuildZoneKey(cameraId, apiPort);
        ZoneCameraProcessState? state;

        lock (_sync)
        {
            if (!_zoneCameraProcesses.TryGetValue(key, out state))
            {
                return Task.FromResult(new CameraStreamStatus(
                    false,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "Zone camera service is already stopped."
                ));
            }

            _zoneCameraProcesses.Remove(key);
        }

        StopProcess(state.Process, "zone camera process");

        return Task.FromResult(new CameraStreamStatus(
            false,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            "Zone camera service stopped."
        ));
    }

    public async Task<CameraStreamStatus> GetZoneStatusAsync(string cameraId, int apiPort, CancellationToken cancellationToken = default)
    {
        var key = BuildZoneKey(cameraId, apiPort);
        ZoneCameraProcessState? state;

        lock (_sync)
        {
            _zoneCameraProcesses.TryGetValue(key, out state);
        }

        if (state is null || state.Process.HasExited)
        {
            return new CameraStreamStatus(
                false,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                "Zone camera service is not running."
            );
        }

        var healthy = await IsHealthyAsync(BuildZoneHealthUrl(state.Request), cancellationToken);
        return BuildZoneStatus(state, healthy ? "Zone camera service is running." : "Zone camera process is running but stream is not ready.");
    }

    public async Task<CameraStreamStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        CameraStartRequest? request;
        Process? process;

        lock (_sync)
        {
            request = _lastRequest;
            process = _cameraProcess;
        }

        if (request is null || process is null || process.HasExited)
        {
            return new CameraStreamStatus(
                false,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                "Camera service is not running."
            );
        }

        var healthy = await IsHealthyAsync(BuildHealthUrl(request), cancellationToken);
        return BuildStatus(healthy ? "Camera service is running." : "Camera process is running but stream is not ready.");
    }

    public void Dispose()
    {
        _ = StopAsync();
        List<ZoneCameraProcessState> zoneStates;
        lock (_sync)
        {
            zoneStates = _zoneCameraProcesses.Values.ToList();
            _zoneCameraProcesses.Clear();
        }

        foreach (var state in zoneStates)
        {
            StopProcess(state.Process, "zone camera process");
        }
    }

    private CameraStreamStatus BuildStatus(string message)
    {
        lock (_sync)
        {
            if (_lastRequest is null || _cameraProcess is null || _cameraProcess.HasExited)
            {
                return new CameraStreamStatus(
                    false,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    message
                );
            }

            return new CameraStreamStatus(
                true,
                _cameraProcess.Id,
                BuildStreamUrl(_lastRequest),
                BuildDetectionUrl(_lastRequest),
                BuildHealthUrl(_lastRequest),
                message
            );
        }
    }

    private static void ValidateRequest(CameraStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CameraIp))
        {
            throw new ArgumentException("Camera IP is required.");
        }

        if (request.CameraPort <= 0 || request.ApiPort <= 0)
        {
            throw new ArgumentException("Camera port and API port must be positive.");
        }

        if (string.IsNullOrWhiteSpace(request.ApiHost))
        {
            throw new ArgumentException("API host is required.");
        }
    }

    private static void ValidateZoneRequest(ZoneCameraStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CameraIp))
        {
            throw new ArgumentException("Camera IP is required.");
        }

        if (request.CameraPort <= 0 || request.ApiPort <= 0)
        {
            throw new ArgumentException("Camera port and API port must be positive.");
        }

        if (string.IsNullOrWhiteSpace(request.ApiHost))
        {
            throw new ArgumentException("API host is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CameraId))
        {
            throw new ArgumentException("Camera ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LocationName))
        {
            throw new ArgumentException("Location name is required.");
        }
    }

    private static bool RequestMatches(CameraStartRequest? left, CameraStartRequest right)
    {
        return left is not null &&
               string.Equals(left.CameraIp, right.CameraIp, StringComparison.OrdinalIgnoreCase) &&
               left.CameraPort == right.CameraPort &&
               string.Equals(left.ApiHost, right.ApiHost, StringComparison.OrdinalIgnoreCase) &&
               left.ApiPort == right.ApiPort &&
               string.Equals(left.StationMode, right.StationMode, StringComparison.OrdinalIgnoreCase) &&
               left.BranchId == right.BranchId;
    }

    private static bool ZoneRequestMatches(ZoneCameraStartRequest left, ZoneCameraStartRequest right)
    {
        return string.Equals(left.CameraIp, right.CameraIp, StringComparison.OrdinalIgnoreCase) &&
               left.CameraPort == right.CameraPort &&
               string.Equals(left.ApiHost, right.ApiHost, StringComparison.OrdinalIgnoreCase) &&
               left.ApiPort == right.ApiPort &&
               string.Equals(left.CameraId, right.CameraId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.LocationName, right.LocationName, StringComparison.Ordinal) &&
               string.Equals(left.ParkingLotCode ?? string.Empty, right.ParkingLotCode ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.ZoneCode ?? string.Empty, right.ZoneCode ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.ColumnCode ?? string.Empty, right.ColumnCode ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
               left.BranchId == right.BranchId;
    }

    private static string BuildStreamUrl(CameraStartRequest request) =>
        $"http://{request.ApiHost}:{request.ApiPort}/api/stream";

    private static string BuildDetectionUrl(CameraStartRequest request) =>
        $"http://{request.ApiHost}:{request.ApiPort}/api/detection";

    private static string BuildHealthUrl(CameraStartRequest request) =>
        $"http://{request.ApiHost}:{request.ApiPort}/api/health";

    private static string BuildZoneStreamUrl(ZoneCameraStartRequest request) =>
        $"http://{request.ApiHost}:{request.ApiPort}/api/stream";

    private static string BuildZoneDetectionUrl(ZoneCameraStartRequest request) =>
        $"http://{request.ApiHost}:{request.ApiPort}/api/detection";

    private static string BuildZoneHealthUrl(ZoneCameraStartRequest request) =>
        $"http://{request.ApiHost}:{request.ApiPort}/api/health";

    private static string BuildZoneKey(string cameraId, int apiPort) =>
        $"{cameraId.Trim().ToUpperInvariant()}:{apiPort}";

    private static void AddOptionalArgument(ProcessStartInfo startInfo, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        startInfo.ArgumentList.Add(name);
        startInfo.ArgumentList.Add(value.Trim());
    }

    private static CameraStreamStatus BuildZoneStatus(ZoneCameraProcessState state, string message)
    {
        if (state.Process.HasExited)
        {
            return new CameraStreamStatus(
                false,
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                message
            );
        }

        return new CameraStreamStatus(
            true,
            state.Process.Id,
            BuildZoneStreamUrl(state.Request),
            BuildZoneDetectionUrl(state.Request),
            BuildZoneHealthUrl(state.Request),
            message
        );
    }

    private static string ResolvePythonExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Python", "Python311", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Python", "Python313", "python.exe"),
            "python"
        };

        return candidates.FirstOrDefault(File.Exists) ?? "python";
    }

    private void KillOrphanCameraProcesses()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE CommandLine LIKE '%webcam_smart_lowlatency.py%'"
            );

            foreach (System.Management.ManagementObject processInfo in searcher.Get())
            {
                var processId = Convert.ToInt32(processInfo["ProcessId"]);
                if (_cameraProcess is not null && _cameraProcess.Id == processId)
                {
                    continue;
                }

                try
                {
                    var process = Process.GetProcessById(processId);
                    if (!process.HasExited)
                    {
                        _logger.LogWarning("Stopping stale camera process {ProcessId}.", processId);
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Unable to stop stale camera process {ProcessId}.", processId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan for stale camera processes.");
        }
    }

    private void KillOrphanZoneCameraProcesses(int apiPort)
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE CommandLine LIKE '%webcam_zone_locator.py%'"
            );

            foreach (System.Management.ManagementObject processInfo in searcher.Get())
            {
                var commandLine = Convert.ToString(processInfo["CommandLine"]) ?? string.Empty;
                if (!commandLine.Contains($"--api-port {apiPort}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var processId = Convert.ToInt32(processInfo["ProcessId"]);
                try
                {
                    var process = Process.GetProcessById(processId);
                    if (!process.HasExited)
                    {
                        _logger.LogWarning("Stopping stale zone camera process {ProcessId} on API port {ApiPort}.", processId, apiPort);
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Unable to stop stale zone camera process {ProcessId}.", processId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan for stale zone camera processes.");
        }
    }

    private static async Task<bool> WaitForHealthAsync(string healthUrl, Process process, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                return false;
            }

            if (await IsHealthyAsync(healthUrl, cancellationToken))
            {
                return true;
            }

            await Task.Delay(1000, cancellationToken);
        }

        return false;
    }

    private static async Task<bool> IsHealthyAsync(string healthUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await httpClient.GetAsync(
                healthUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void CleanupFailedStart(Process process)
    {
        lock (_sync)
        {
            if (_cameraProcess?.Id == process.Id)
            {
                _cameraProcess = null;
                _lastRequest = null;
            }
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed while cleaning up camera process after startup failure.");
        }
        finally
        {
            process.Dispose();
        }
    }

    private void CleanupFailedZoneStart(string key, Process process)
    {
        lock (_sync)
        {
            if (_zoneCameraProcesses.TryGetValue(key, out var state) && state.Process.Id == process.Id)
            {
                _zoneCameraProcesses.Remove(key);
            }
        }

        StopProcess(process, "zone camera process after startup failure");
    }

    private void StopProcess(Process process, string processName)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed while stopping {ProcessName}.", processName);
        }
        finally
        {
            process.Dispose();
        }
    }

    private sealed record ZoneCameraProcessState(Process Process, ZoneCameraStartRequest Request);
}
