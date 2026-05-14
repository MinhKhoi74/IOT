using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.Models;
using SmartParking.Services.Interfaces;
using SmartParking.SignalR;

namespace SmartParking.Services
{
    public class VehicleLocationService : IVehicleLocationService
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisService _redis;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IHubContext<ParkingHub> _parkingHub;
        private readonly ILogger<VehicleLocationService> _logger;

        public VehicleLocationService(
            ApplicationDBContext context,
            IRedisService redis,
            IHubContext<NotificationHub> notificationHub,
            IHubContext<ParkingHub> parkingHub,
            ILogger<VehicleLocationService> logger)
        {
            _context = context;
            _redis = redis;
            _notificationHub = notificationHub;
            _parkingHub = parkingHub;
            _logger = logger;
        }

        public async Task<VehicleLocationDetectionResult> ProcessDetectionAsync(VehicleLocationDetectionRequest request)
        {
            return await ProcessDetectionCoreAsync(request);
        }

        public async Task<VehicleLocationDetectionBatchResult> ProcessDetectionBatchAsync(VehicleLocationDetectionBatchRequest request)
        {
            var bestByPlate = request.Detections
                .Where(x => !string.IsNullOrWhiteSpace(x.PlateNumber))
                .GroupBy(x => NormalizePlate(x.PlateNumber))
                .Select(group => group.OrderByDescending(x => x.Confidence).First())
                .ToList();

            var results = new List<VehicleLocationDetectionResult>();
            foreach (var item in bestByPlate)
            {
                var single = new VehicleLocationDetectionRequest
                {
                    PlateNumber = item.PlateNumber,
                    CameraId = request.CameraId,
                    ParkingLotCode = request.ParkingLotCode,
                    ZoneCode = request.ZoneCode,
                    ColumnCode = request.ColumnCode,
                    LocationName = request.LocationName,
                    Confidence = item.Confidence,
                    ImageBase64 = item.ImageBase64,
                    FullFrameImageBase64 = item.FullFrameImageBase64,
                    DetectedAt = item.DetectedAt ?? request.BatchEndedAt ?? DateTime.Now
                };

                results.Add(await ProcessDetectionCoreAsync(single));
            }

            return new VehicleLocationDetectionBatchResult
            {
                Success = true,
                ProcessedCount = results.Count,
                Results = results
            };
        }

        private async Task<VehicleLocationDetectionResult> ProcessDetectionCoreAsync(VehicleLocationDetectionRequest request)
        {
            var plate = NormalizePlate(request.PlateNumber);
            var detectedAt = request.DetectedAt ?? DateTime.Now;

            var activeInRedis = await _redis.IsPlateActiveAsync(plate);
            var activeSessions = await _context.CheckInOuts
                .Where(x => x.Status == "Active")
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
            var activeSession = activeSessions.FirstOrDefault(x => NormalizePlate(x.LicensePlate) == plate)
                ?? activeSessions.FirstOrDefault(x => IsSamePlate(x.LicensePlate, plate));
            var canonicalPlate = activeSession != null ? NormalizePlate(activeSession.LicensePlate) : plate;

            var vehicle = await _context.Vehicle
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.LicensePlate == canonicalPlate && x.IsActive);

            var hasMonthlyPass = await HasActiveMonthlyPassAsync(canonicalPlate, detectedAt);
            var isCheckedIn = activeInRedis || activeSession != null;
            var isRegisteredToUser = vehicle != null && !string.IsNullOrWhiteSpace(vehicle.UserId);
            var isKnownUnregisteredPlate = isCheckedIn || hasMonthlyPass || vehicle != null;

            var status = "UnknownPlate";
            var severity = "High";
            var message = $"Phat hien bien so la {plate} tai {request.LocationName}.";

            if (isRegisteredToUser)
            {
                status = isCheckedIn ? "KnownCheckedIn" : "RegisteredUserVehicle";
                severity = "Info";
                message = $"Xe {canonicalPlate} dang do tai {request.LocationName}.";
            }
            else if (isKnownUnregisteredPlate)
            {
                status = "KnownUnregisteredPlate";
                severity = "Info";
                message = $"Bien so {canonicalPlate} duoc ghi nhan tai {request.LocationName}.";
            }

            var detection = new VehicleLocationDetection
            {
                LicensePlate = canonicalPlate,
                VehicleId = vehicle?.Id,
                UserId = activeSession?.UserId ?? vehicle?.UserId,
                CheckInOutId = activeSession?.Id,
                CameraId = request.CameraId.Trim(),
                ParkingLotCode = TrimOrNull(request.ParkingLotCode),
                ZoneCode = TrimOrNull(request.ZoneCode),
                ColumnCode = TrimOrNull(request.ColumnCode),
                LocationName = request.LocationName.Trim(),
                Confidence = request.Confidence,
                ImageBase64 = request.ImageBase64,
                FullFrameImageBase64 = request.FullFrameImageBase64,
                DetectedAt = detectedAt,
                Status = status,
                Severity = severity,
                Message = message,
                IsLatest = true,
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await DeleteExistingLocationsAsync(canonicalPlate);
            _context.VehicleLocationDetections.Add(detection);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var dto = ToDto(detection);
            if (isRegisteredToUser && !string.IsNullOrWhiteSpace(detection.UserId))
            {
                await NotifyUserLocationAsync(detection.UserId, dto);
            }
            else if (status == "UnknownPlate")
            {
                await NotifyStaffAlertAsync(dto);
            }

            _logger.LogInformation(
                "Vehicle location detection {Status}: {Plate} at {Location} ({Confidence})",
                status,
                plate,
                request.LocationName,
                request.Confidence);

            return new VehicleLocationDetectionResult
            {
                Success = true,
                Message = message,
                Status = status,
                Severity = severity,
                Detection = dto,
            };
        }

        public async Task<List<VehicleLocationDto>> GetMyLatestLocationsAsync(string userId)
        {
            return await _context.VehicleLocationDetections
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsLatest
                    && (x.Status == "KnownCheckedIn" || x.Status == "RegisteredUserVehicle"))
                .OrderByDescending(x => x.DetectedAt)
                .Select(x => ToDto(x))
                .ToListAsync();
        }

        public async Task<List<VehicleLocationDto>> GetRecentAlertsAsync(int take = 50)
        {
            take = Math.Clamp(take, 1, 200);
            return await _context.VehicleLocationDetections
                .AsNoTracking()
                .OrderByDescending(x => x.DetectedAt)
                .Take(take)
                .Select(x => ToDto(x))
                .ToListAsync();
        }

        public async Task<VehicleLocationDto?> GetLocationAlertAsync(int id)
        {
            return await _context.VehicleLocationDetections
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => ToDto(x))
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasActiveMonthlyPassAsync(string plate, DateTime atTime)
        {
            if (await _redis.HasActiveMonthlyPassAsync(plate, atTime))
            {
                return true;
            }

            return await _context.MonthlyPasses
                .AsNoTracking()
                .AnyAsync(x => x.LicensePlate == plate
                    && x.IsActive
                    && x.ValidFrom <= atTime
                    && x.ValidTo >= atTime);
        }

        private async Task DeleteExistingLocationsAsync(string plate)
        {
            await _context.VehicleLocationDetections
                .Where(x => x.LicensePlate == plate)
                .ExecuteDeleteAsync();
        }

        private async Task NotifyUserLocationAsync(string userId, VehicleLocationDto dto)
        {
            var notification = new
            {
                type = "vehicle_location_updated",
                title = "Vi tri xe moi nhat",
                message = dto.Message,
                location = dto,
                timestamp = DateTime.UtcNow
            };

            await _notificationHub.Clients.User(userId)
                .SendAsync("ReceiveVehicleLocationUpdated", notification);
        }

        private async Task NotifyStaffAlertAsync(VehicleLocationDto dto)
        {
            var alert = new
            {
                type = "vehicle_location_alert",
                title = dto.Status == "UnknownPlate" ? "Bien so la" : "Xe chua checkin",
                message = dto.Message,
                severity = dto.Severity,
                detection = dto,
                timestamp = DateTime.UtcNow
            };

            await _parkingHub.Clients.All.SendAsync("VehicleLocationAlert", alert);
        }

        private static VehicleLocationDto ToDto(VehicleLocationDetection x)
        {
            return new VehicleLocationDto
            {
                Id = x.Id,
                LicensePlate = x.LicensePlate,
                VehicleId = x.VehicleId,
                UserId = x.UserId,
                OwnerName = x.User != null ? x.User.FullName : null,
                CheckInOutId = x.CheckInOutId,
                CameraId = x.CameraId,
                ParkingLotCode = x.ParkingLotCode,
                ZoneCode = x.ZoneCode,
                ColumnCode = x.ColumnCode,
                LocationName = x.LocationName,
                Confidence = x.Confidence,
                ImageBase64 = x.ImageBase64,
                FullFrameImageBase64 = x.FullFrameImageBase64,
                DetectedAt = x.DetectedAt,
                CreatedAt = x.CreatedAt,
                IsLatest = x.IsLatest,
                Status = x.Status,
                Severity = x.Severity,
                Message = x.Message,
            };
        }

        private static string NormalizePlate(string plate)
        {
            return (plate ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");
        }

        private static bool IsSamePlate(string sourcePlate, string detectedPlate)
        {
            var source = NormalizePlate(sourcePlate);
            var detected = NormalizePlate(detectedPlate);
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(detected))
            {
                return false;
            }

            if (source == detected)
            {
                return true;
            }

            var distance = LevenshteinDistance(source, detected);
            var maxLength = Math.Max(source.Length, detected.Length);
            if (maxLength == 0)
            {
                return false;
            }

            var similarity = (1.0 - (double)distance / maxLength) * 100.0;
            return similarity >= 80.0;
        }

        private static int LevenshteinDistance(string left, string right)
        {
            if (left.Length < right.Length)
            {
                return LevenshteinDistance(right, left);
            }

            if (right.Length == 0)
            {
                return left.Length;
            }

            var previous = Enumerable.Range(0, right.Length + 1).ToArray();
            for (var i = 0; i < left.Length; i++)
            {
                var current = new int[right.Length + 1];
                current[0] = i + 1;
                for (var j = 0; j < right.Length; j++)
                {
                    var insertions = previous[j + 1] + 1;
                    var deletions = current[j] + 1;
                    var substitutions = previous[j] + (left[i] == right[j] ? 0 : 1);
                    current[j + 1] = Math.Min(Math.Min(insertions, deletions), substitutions);
                }
                previous = current;
            }

            return previous[right.Length];
        }

        private static string? TrimOrNull(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }
    }
}
