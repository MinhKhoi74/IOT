using System.IO.Ports;
using Microsoft.Extensions.Options;
using SmartParking.Configurations;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class ArduinoSerialService : IArduinoSerialService, IDisposable
    {
        private readonly ArduinoSerialSettings _settings;
        private readonly ILogger<ArduinoSerialService> _logger;
        private readonly object _syncRoot = new();
        private SerialPort? _serialPort;
        private bool _disposed;

        public ArduinoSerialService(
            IOptions<ArduinoSerialSettings> settings,
            ILogger<ArduinoSerialService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task SendCheckInOkAsync(string plateNumber, CancellationToken cancellationToken = default)
        {
            return SendCommandAsync($"IN_OK:{NormalizePlate(plateNumber)}", cancellationToken);
        }

        public Task SendCheckOutOkAsync(string plateNumber, CancellationToken cancellationToken = default)
        {
            return SendCommandAsync($"OUT_OK:{NormalizePlate(plateNumber)}", cancellationToken);
        }

        private Task SendCommandAsync(string command, CancellationToken cancellationToken)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Arduino serial disabled. Skipped command: {Command}", command);
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(_settings.PortName))
            {
                _logger.LogWarning("Arduino serial port is not configured. Skipped command: {Command}", command);
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                lock (_syncRoot)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var port = EnsurePortOpen();
                    port.WriteLine(command);
                }

                _logger.LogInformation("Sent Arduino command: {Command}", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send Arduino command {Command} to {PortName}",
                    command,
                    _settings.PortName);
            }

            return Task.CompletedTask;
        }

        private SerialPort EnsurePortOpen()
        {
            if (_serialPort is { IsOpen: true })
            {
                return _serialPort;
            }

            _serialPort?.Dispose();
            _serialPort = new SerialPort(_settings.PortName, _settings.BaudRate)
            {
                NewLine = _settings.NewLine,
                WriteTimeout = _settings.WriteTimeoutMilliseconds,
                ReadTimeout = 1000,
                DtrEnable = true,
                RtsEnable = true
            };

            _serialPort.Open();
            _logger.LogInformation(
                "Arduino serial port opened: {PortName} at {BaudRate}",
                _settings.PortName,
                _settings.BaudRate);

            return _serialPort;
        }

        private static string NormalizePlate(string plateNumber)
        {
            return (plateNumber ?? string.Empty).Trim().ToUpperInvariant();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_syncRoot)
            {
                _serialPort?.Dispose();
                _serialPort = null;
                _disposed = true;
            }
        }
    }
}
