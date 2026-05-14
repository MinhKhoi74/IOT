using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Parking;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class ParkingHistoryService : IParkingHistoryService
    {
        private readonly ApplicationDBContext _context;

        public ParkingHistoryService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<ParkingHistoryItemDto>> GetHistoryByPlateAsync(string plate)
        {
            plate = plate.ToUpper().Trim();

            return await BuildQuery()
                .Where(x => x.LicensePlate == plate)
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<List<ParkingHistoryItemDto>> GetMyHistoryAsync(string userId)
        {
            return await BuildQuery()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<ParkingHistoryItemDto?> GetLatestCheckInAsync()
        {
            return await BuildQuery()
                .OrderByDescending(x => x.CheckInTime)
                .FirstOrDefaultAsync();
        }

        public async Task<ParkingHistoryItemDto?> GetLatestCheckOutAsync()
        {
            return await BuildQuery()
                .Where(x => x.CheckOutTime != null)
                .OrderByDescending(x => x.CheckOutTime)
                .FirstOrDefaultAsync();
        }

        public async Task<ParkingDashboardDto> GetDashboardAsync()
        {
            var sessions = await BuildQuery()
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();

            var activeVehicleCount = await _context.CheckInOuts
                .CountAsync(x => x.Status == "Active" && x.CheckOutTime == null);

            var totalRevenue = await _context.CheckInOuts
                .Where(x => x.PaymentStatus == "Paid")
                .SumAsync(x => x.FeeAmount);

            var monthlyPassRevenue = await _context.WalletTransactions
                .Where(x => x.ReferenceType == "MonthlyPass" && x.Type == "MonthlyPassRevenue")
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            return new ParkingDashboardDto
            {
                ActiveVehicleCount = activeVehicleCount,
                TotalRevenue = totalRevenue + monthlyPassRevenue,
                Sessions = sessions
            };
        }

        public async Task<ParkingHistoryItemDto?> GetSessionDetailAsync(int id)
        {
            return await BuildQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        private IQueryable<ParkingHistoryItemDto> BuildQuery()
        {
            return _context.CheckInOuts
                .Select(x => new ParkingHistoryItemDto
                {
                    Id = x.Id,
                    LicensePlate = x.LicensePlate,
                    CheckInTime = x.CheckInTime,
                    CheckOutTime = x.CheckOutTime,
                    DurationMinutes = x.DurationMinutes,
                    FeeAmount = x.FeeAmount,
                    FeeStatus = x.FeeStatus,
                    PaymentStatus = x.PaymentStatus,
                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,
                    CheckInStationId = x.CheckInStationId,
                    CheckInImageBase64 = x.CheckInImageBase64,
                    CheckOutStationId = x.CheckOutStationId,
                    CheckOutImageBase64 = x.CheckOutImageBase64,
                    VehicleId = x.VehicleId,
                    UserId = x.UserId
                });
        }
    }
}
