using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.DTOs.ElectronicTicket;
using SmartParking.DTOs.Momo;
using SmartParking.Models;
using SmartParking.Models.Enums;
using SmartParking.Services.Interfaces;
using SmartParking.SignalR;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SmartParking.Services
{
    public class CheckOutService : ICheckOutService
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisService _redis;
        private readonly IElectronicTicketService _electronicTicketService;
        private readonly IElectronicTicketNotificationService _notificationService;
        private readonly IMonthlyPassService _monthlyPassService;
        private readonly IArduinoSerialService _arduinoSerialService;
        private readonly IHubContext<ParkingHub> _parkingHub;
        private readonly ILogger<CheckOutService> _logger;

        private readonly decimal _baseFee = 5000m;
        private readonly decimal _feePerHour = 1000m;

        public CheckOutService(
            ApplicationDBContext context,
            IRedisService redis,
            IWalletService walletService,
            IMomoService momoService,
            IElectronicTicketService electronicTicketService,
            IElectronicTicketNotificationService notificationService,
            IMonthlyPassService monthlyPassService,
            IArduinoSerialService arduinoSerialService,
            IHubContext<ParkingHub> parkingHub,
            ILogger<CheckOutService> logger)
        {
            _context = context;
            _redis = redis;
            _electronicTicketService = electronicTicketService;
            _notificationService = notificationService;
            _monthlyPassService = monthlyPassService;
            _arduinoSerialService = arduinoSerialService;
            _parkingHub = parkingHub;
            _logger = logger;
        }

        public async Task<CheckOutResult> ProcessCheckOutAsync(CheckOutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PlateNumber))
            {
                return new CheckOutResult
                {
                    Success = false,
                    Message = "Bien so xe khong duoc de trong",
                    ErrorCode = "EMPTY_PLATE"
                };
            }

            var plate = request.PlateNumber.ToUpper().Trim();
            var now = DateTime.Now;

            try
            {
                var checkinTime = await _redis.GetCheckinTimeAsync(plate);
                var checkinRecord = await _context.CheckInOuts
                    .Where(c => c.LicensePlate == plate && c.Status == "Active")
                    .Where(c => !request.BranchId.HasValue || c.BranchId == request.BranchId.Value)
                    .OrderByDescending(c => c.CheckInTime)
                    .FirstOrDefaultAsync();

                checkinTime ??= checkinRecord?.CheckInTime;

                if (checkinRecord == null || checkinTime == null)
                {
                    _logger.LogWarning("No active checkin found for {Plate}", plate);
                    return new CheckOutResult
                    {
                        Success = false,
                        Message = $"Khong tim thay thong tin checkin cho {plate}",
                        ErrorCode = "NO_CHECKIN_RECORD"
                    };
                }

                var duration = now - checkinTime.Value;
                var hasMonthlyPass = await _monthlyPassService.HasActivePassAsync(plate, now);
                var fee = hasMonthlyPass ? 0m : CalculateFee(duration);

                if (fee > 0)
                {
                    return await BuildPendingPaymentResultAsync(
                        checkinRecord,
                        request,
                        plate,
                        now,
                        duration,
                        fee,
                        FormatCheckoutPaymentMessage(plate, "Pending", fee),
                        "PAYMENT_REQUIRED_CASH");
                }

                checkinRecord.CheckOutTime = now;
                checkinRecord.CheckOutStationId = request.StationId ?? "STATION_02";
                checkinRecord.CheckOutImageBase64 = request.ImageBase64 ?? checkinRecord.CheckOutImageBase64 ?? string.Empty;
                checkinRecord.DurationMinutes = (int)duration.TotalMinutes;
                checkinRecord.FeeAmount = fee;
                checkinRecord.FeeCalculatedAt = now;
                checkinRecord.FeeStatus = "Paid";
                checkinRecord.PaymentStatus = "Paid";
                checkinRecord.PaymentMethod = hasMonthlyPass ? "MonthlyPass" : "Free";
                checkinRecord.PaidAt = now;
                checkinRecord.Status = "Completed";
                checkinRecord.UpdatedAt = now;

                _context.CheckInOuts.Update(checkinRecord);
                await _context.SaveChangesAsync();

                await UpdateElectronicTicketAsync(checkinRecord, plate, now, fee);
                await _redis.RemoveCheckinAsync(plate);
                await DeleteVehicleLocationAsync(checkinRecord.LicensePlate, checkinRecord.BranchId);

                var feeText = hasMonthlyPass ? "- Ve thang" : "- Mien phi";
                _logger.LogInformation("Checkout - {Plate} - {Time:dd/M/yyyy - HH:mm} {FeeText}", plate, now, feeText);
                await NotifyDashboardUpdatedAsync("checkout", checkinRecord);
                await _arduinoSerialService.SendCheckOutOkAsync(plate);

                return new CheckOutResult
                {
                    Success = true,
                    Message = FormatCheckoutPaymentMessage(plate, checkinRecord.PaymentStatus, fee),
                    CheckOutId = checkinRecord.Id,
                    CheckOutTime = now,
                    DurationMinutes = (int)duration.TotalMinutes,
                    FeeAmount = fee,
                    PaymentStatus = checkinRecord.PaymentStatus,
                    PaymentMethod = checkinRecord.PaymentMethod
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout failed for {Plate}: {Message}", plate, ex.Message);

                return new CheckOutResult
                {
                    Success = false,
                    Message = "Co loi xay ra trong qua trinh checkout",
                    ErrorCode = "SYSTEM_ERROR"
                };
            }
        }

        public decimal CalculateFee(TimeSpan duration)
        {
            var hours = Math.Ceiling(Math.Max(0, duration.TotalMinutes) / 60.0);
            return _baseFee + ((decimal)hours * _feePerHour);
        }

        public async Task<CheckOutResult> ConfirmPendingPaymentAsync(int checkOutId, ConfirmCheckOutPaymentRequest request)
        {
            var paymentMethod = (request.PaymentMethod ?? "Cash").Trim();
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                paymentMethod = "Cash";
            }

            var checkinRecord = await _context.CheckInOuts.FirstOrDefaultAsync(x => x.Id == checkOutId);
            if (checkinRecord == null)
            {
                return new CheckOutResult
                {
                    Success = false,
                    Message = $"Khong tim thay phien gui xe {checkOutId}",
                    ErrorCode = "CHECKOUT_NOT_FOUND"
                };
            }

            if (!string.Equals(checkinRecord.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return new CheckOutResult
                {
                    Success = false,
                    Message = $"Phien gui xe {checkOutId} khong o trang thai cho thanh toan",
                    ErrorCode = "PAYMENT_NOT_PENDING"
                };
            }

            var now = DateTime.Now;
            checkinRecord.PaymentStatus = "Paid";
            checkinRecord.PaymentMethod = paymentMethod;
            checkinRecord.FeeStatus = "Paid";
            checkinRecord.PaidAt = now;
            checkinRecord.Status = "Completed";
            checkinRecord.UpdatedAt = now;

            _context.CheckInOuts.Update(checkinRecord);
            await _context.SaveChangesAsync();
            await _redis.RemoveCheckinAsync(checkinRecord.LicensePlate);
            await DeleteVehicleLocationAsync(checkinRecord.LicensePlate, checkinRecord.BranchId);
            await NotifyDashboardUpdatedAsync("payment-confirmed", checkinRecord);
            await _arduinoSerialService.SendCheckOutOkAsync(checkinRecord.LicensePlate);

            return new CheckOutResult
            {
                Success = true,
                Message = FormatCheckoutPaymentMessage(
                    checkinRecord.LicensePlate,
                    checkinRecord.PaymentStatus,
                    checkinRecord.FeeAmount
                ),
                CheckOutId = checkinRecord.Id,
                CheckOutTime = checkinRecord.CheckOutTime,
                DurationMinutes = checkinRecord.DurationMinutes,
                FeeAmount = checkinRecord.FeeAmount,
                PaymentStatus = checkinRecord.PaymentStatus,
                PaymentMethod = checkinRecord.PaymentMethod
            };
        }

        private async Task<CheckOutResult> BuildPendingPaymentResultAsync(
            CheckInOut checkinRecord,
            CheckOutRequest request,
            string plate,
            DateTime now,
            TimeSpan duration,
            decimal fee,
            string message,
            string errorCode)
        {
            checkinRecord.CheckOutTime = now;
            checkinRecord.CheckOutStationId = request.StationId ?? "STATION_02";
            checkinRecord.CheckOutImageBase64 = request.ImageBase64 ?? checkinRecord.CheckOutImageBase64 ?? string.Empty;
            checkinRecord.DurationMinutes = (int)duration.TotalMinutes;
            checkinRecord.FeeAmount = fee;
            checkinRecord.FeeCalculatedAt = now;
            checkinRecord.FeeStatus = "Calculated";
            checkinRecord.PaymentStatus = "Pending";
            checkinRecord.PaymentMethod = null;
            checkinRecord.PaidAt = null;
            checkinRecord.Status = "Active";
            checkinRecord.UpdatedAt = now;

            _context.CheckInOuts.Update(checkinRecord);
            await _context.SaveChangesAsync();
            await _redis.RemoveCheckinAsync(plate);
            await NotifyDashboardUpdatedAsync("checkout-pending-payment", checkinRecord);

            return new CheckOutResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                CheckOutId = checkinRecord.Id,
                CheckOutTime = now,
                DurationMinutes = (int)duration.TotalMinutes,
                FeeAmount = fee,
                PaymentStatus = "Pending",
                RequiresPaymentAction = true,
                PaymentOptions = new List<CheckOutPaymentOptionDto>
                {
                    new()
                    {
                        Method = "Cash",
                        Label = "Tien mat",
                        Note = "Staff thu tien va bam xac nhan thanh toan"
                    }
                }
            };
        }

        private async Task UpdateElectronicTicketAsync(CheckInOut checkinRecord, string plate, DateTime now, decimal fee)
        {
            try
            {
                var ticket = await _electronicTicketService.GetTicketByLicensePlateAsync(plate);
                if (ticket == null)
                {
                    return;
                }

                await _electronicTicketService.UpdateCheckOutInfoAsync(ticket.Id, now, fee);

                if (fee <= 0)
                {
                    return;
                }

                var confirmDto = new PaymentConfirmationDto
                {
                    TicketId = ticket.Id,
                    PaymentAmount = fee,
                    PaymentMethod = PaymentMethod.Cash
                };
                await _electronicTicketService.UpdatePaymentStatusAsync(ticket.Id, confirmDto);

                if (!string.IsNullOrEmpty(checkinRecord.UserId))
                {
                    await _notificationService.SendPaymentConfirmationAsync(
                        checkinRecord.UserId,
                        ticket.Id,
                        $"Thanh toan {fee:N0}d tai quay thanh cong");
                }
            }
            catch (Exception ticketEx)
            {
                _logger.LogError(ticketEx, "Error updating electronic ticket for {Plate}", plate);
            }
        }

        private async Task NotifyDashboardUpdatedAsync(string type, CheckInOut session)
        {
            var dashboardPayload = new
            {
                type,
                checkInOutId = session.Id,
                licensePlate = session.LicensePlate,
                checkInTime = session.CheckInTime,
                checkOutTime = session.CheckOutTime,
                paymentStatus = session.PaymentStatus,
                status = session.Status
            };

            await _parkingHub.Clients.Group(ParkingHub.AdminGroup)
                .SendAsync("ParkingDashboardUpdated", dashboardPayload);
            if (session.BranchId.HasValue)
            {
                await _parkingHub.Clients.Group(ParkingHub.BranchGroup(session.BranchId.Value))
                    .SendAsync("ParkingDashboardUpdated", dashboardPayload);
            }
        }

        private async Task DeleteVehicleLocationAsync(string plate, Guid? branchId)
        {
            var normalizedPlate = plate.ToUpperInvariant().Replace(" ", "");
            await _context.VehicleLocationDetections
                .Where(x => x.LicensePlate.Replace(" ", "") == normalizedPlate)
                .Where(x => !branchId.HasValue || x.BranchId == branchId.Value)
                .ExecuteDeleteAsync();
        }

        private static string EncodeExtraData(Dictionary<string, string> data)
        {
            var json = JsonSerializer.Serialize(data);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        private static string FormatCheckoutPaymentMessage(string plate, string? paymentStatus, decimal? fee)
        {
            if (string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return $"Xe {plate} da thanh toan";
            }

            return $"Xe {plate} chua thanh toan. Phi can thu: {FormatVnd(fee ?? 0m)}d";
        }

        private static string FormatVnd(decimal amount)
        {
            return amount.ToString("#,0", CultureInfo.GetCultureInfo("vi-VN"));
        }
    }
}
