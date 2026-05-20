using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.DTOs.ElectronicTicket;
using SmartParking.Models;
using SmartParking.Services.Interfaces;
using SmartParking.SignalR;

namespace SmartParking.Services
{
    public class CheckInService : ICheckInService
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisService _redis;
        private readonly IElectronicTicketService _electronTicketService;
        private readonly IArduinoSerialService _arduinoSerialService;
        private readonly IHubContext<ParkingHub> _parkingHub;
        private readonly ILogger<CheckInService> _logger;

        public CheckInService(
            ApplicationDBContext context,
            IRedisService redis,
            IElectronicTicketService electronTicketService,
            IArduinoSerialService arduinoSerialService,
            IHubContext<ParkingHub> parkingHub,
            ILogger<CheckInService> logger)
        {
            _context = context;
            _redis = redis;
            _electronTicketService = electronTicketService;
            _arduinoSerialService = arduinoSerialService;
            _parkingHub = parkingHub;
            _logger = logger;
        }

        public async Task<CheckInResult> ProcessCheckInAsync(CheckInRequest request)
        {
            if (string.IsNullOrEmpty(request.PlateNumber))
            {
                return new CheckInResult
                {
                    Success = false,
                    Message = "Biển số xe không được để trống",
                    ErrorCode = "EMPTY_PLATE"
                };
            }

            var plate = request.PlateNumber.ToUpper().Trim();
            var now = DateTime.Now;

            try
            {
                if (await _redis.IsPlateActiveAsync(plate))
                {
                    _logger.LogWarning("Duplicate checkin attempt: {Plate}", plate);
                    return new CheckInResult
                    {
                        Success = false,
                        Message = $"Xe {plate} đã checkin rồi. Không thể checkin lại!",
                        ErrorCode = "DUPLICATE_CHECKIN"
                    };
                }

                if (request.BranchId.HasValue)
                {
                    var capacity = await _context.Branches
                        .Where(x => x.Id == request.BranchId.Value)
                        .Select(x => x.MaxVehicleCapacity)
                        .FirstOrDefaultAsync();

                    if (capacity > 0)
                    {
                        var activeVehicleCount = await _context.CheckInOuts
                            .CountAsync(x =>
                                x.BranchId == request.BranchId.Value &&
                                x.Status == "Active" &&
                                x.CheckOutTime == null);

                        if (activeVehicleCount >= capacity)
                        {
                            _logger.LogWarning(
                                "Checkin rejected because branch {BranchId} is full: {ActiveVehicleCount}/{Capacity}",
                                request.BranchId.Value,
                                activeVehicleCount,
                                capacity);

                            return new CheckInResult
                            {
                                Success = false,
                                Message = $"Bai xe da day ({activeVehicleCount}/{capacity}). Khong the checkin them xe.",
                                ErrorCode = "PARKING_FULL"
                            };
                        }
                    }
                }

                await _redis.AddCheckinAsync(plate, now);

                var vehicle = await _context.Vehicle
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LicensePlate == plate && x.IsActive);

                var checkinRecord = new CheckInOut
                {
                    VehicleId = vehicle?.Id,
                    UserId = vehicle?.UserId,
                    BranchId = request.BranchId,
                    LicensePlate = plate,
                    PlateImagePath = string.Empty,
                    CheckInTime = now,
                    CheckInStationId = request.StationId ?? "STATION_01",
                    CheckInImageBase64 = request.ImageBase64 ?? string.Empty,
                    CheckOutStationId = string.Empty,
                    CheckOutImageBase64 = string.Empty,
                    Confidence = request.Confidence,
                    Status = "Active",
                    FeeStatus = "Pending",
                    PaymentStatus = "Pending",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.CheckInOuts.Add(checkinRecord);
                await _context.SaveChangesAsync();

                // ======== TẠO VÉ ĐIỆN TỬ ========
                // Tạo vé điện tử ảo cho mọi biển số
                try
                {
                    // Lấy parking lot đầu tiên (mặc định) vì CheckInRequest không có ParkingLotId
                    var parkingLot = await _context.ParkingLots
                        .Include(p => p.Branch)
                        .Where(p => !request.BranchId.HasValue || p.BranchId == request.BranchId.Value)
                        .FirstOrDefaultAsync();

                    var parkingLotName = parkingLot?.Name ?? "Chưa xác định";
                    var branchName = parkingLot?.Branch?.Name ?? "Chưa xác định";

                    var ticketDto = await _electronTicketService.CreateTicketAsync(new CreateElectronicTicketDto
                    {
                        LicensePlate = plate,
                        ParkingLotName = parkingLotName,
                        BranchName = branchName,
                        CheckInDateTime = now
                    });

                    _logger.LogInformation("Electronic ticket created for {Plate}: {TicketCode}", plate, ticketDto.TicketCode);
                }
                catch (Exception ticketEx)
                {
                    _logger.LogError(ticketEx, "Error creating electronic ticket for {Plate}", plate);
                    // Không throw - tiếp tục với check-in ngay cả nếu tạo vé thất bại
                }

                _logger.LogInformation("Checkin - {Plate} - {Time:dd/M/yyyy - HH:mm}", plate, now);
                var dashboardPayload = new
                {
                    type = "checkin",
                    checkInId = checkinRecord.Id,
                    licensePlate = plate,
                    checkInTime = now
                };

                await _parkingHub.Clients.Group(ParkingHub.AdminGroup)
                    .SendAsync("ParkingDashboardUpdated", dashboardPayload);
                if (checkinRecord.BranchId.HasValue)
                {
                    await _parkingHub.Clients.Group(ParkingHub.BranchGroup(checkinRecord.BranchId.Value))
                        .SendAsync("ParkingDashboardUpdated", dashboardPayload);
                }

                await _arduinoSerialService.SendCheckInOkAsync(plate);

                return new CheckInResult
                {
                    Success = true,
                    Message = $"Checkin thành công cho {plate}",
                    CheckInId = checkinRecord.Id,
                    CheckInTime = now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkin failed for {Plate}: {Message}", plate, ex.Message);

                try
                {
                    await _redis.RemoveCheckinAsync(plate);
                }
                catch (Exception redisEx)
                {
                    _logger.LogError(redisEx, "Failed to rollback Redis for {Plate}", plate);
                }

                return new CheckInResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình checkin",
                    ErrorCode = "SYSTEM_ERROR"
                };
            }
        }
    }
}
