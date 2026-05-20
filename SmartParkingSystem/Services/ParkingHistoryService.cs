using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Parking;
using SmartParking.Models;
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
            return await GetHistoryByPlateAsync(plate, null);
        }

        public async Task<List<ParkingHistoryItemDto>> GetHistoryByPlateAsync(string plate, Guid? branchId)
        {
            plate = plate.ToUpper().Trim();

            return await ApplyBranchScope(BuildQuery(), branchId)
                .Where(x => x.LicensePlate == plate)
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<List<ParkingHistoryItemDto>> GetHistoryAsync(Guid? branchId = null)
        {
            return await ApplyBranchScope(BuildQuery(), branchId)
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

        public async Task<ParkingHistoryItemDto?> GetLatestCheckInAsync(Guid? branchId = null)
        {
            return await ApplyBranchScope(BuildQuery(), branchId)
                .OrderByDescending(x => x.CheckInTime)
                .FirstOrDefaultAsync();
        }

        public async Task<ParkingHistoryItemDto?> GetLatestCheckOutAsync(Guid? branchId = null)
        {
            return await ApplyBranchScope(BuildQuery(), branchId)
                .Where(x => x.CheckOutTime != null)
                .OrderByDescending(x => x.CheckOutTime)
                .FirstOrDefaultAsync();
        }

        public async Task<ParkingDashboardDto> GetDashboardAsync(Guid? branchId = null)
        {
            var sessions = await ApplyBranchScope(BuildQuery(), branchId)
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();

            var checkIns = ApplyBranchScope(_context.CheckInOuts.AsQueryable(), branchId);

            var activeVehicleCount = await checkIns
                .CountAsync(x => x.Status == "Active" && x.CheckOutTime == null);

            var casualRevenue = await checkIns
                .Where(x => x.PaymentStatus == "Paid")
                .SumAsync(x => x.FeeAmount);

            var maxVehicleCapacity = branchId.HasValue
                ? await _context.Branches
                    .Where(x => x.Id == branchId.Value)
                    .Select(x => (int?)x.MaxVehicleCapacity)
                    .FirstOrDefaultAsync()
                : await _context.Branches.SumAsync(x => (int?)x.MaxVehicleCapacity) ?? 0;

            var monthlyPassRevenue = branchId.HasValue
                ? 0m
                : (await _context.MonthlyPasses.SumAsync(x => (decimal?)x.Amount) ?? 0m);

            return new ParkingDashboardDto
            {
                ActiveVehicleCount = activeVehicleCount,
                MaxVehicleCapacity = maxVehicleCapacity,
                TotalRevenue = casualRevenue + monthlyPassRevenue,
                CasualRevenue = casualRevenue,
                MonthlyPassRevenue = monthlyPassRevenue,
                Sessions = sessions
            };
        }

        public async Task<ParkingHistoryItemDto?> GetSessionDetailAsync(int id, Guid? branchId = null)
        {
            return await ApplyBranchScope(BuildQuery(), branchId).FirstOrDefaultAsync(x => x.Id == id);
        }

        private static IQueryable<CheckInOut> ApplyBranchScope(IQueryable<CheckInOut> query, Guid? branchId)
        {
            return branchId.HasValue ? query.Where(x => x.BranchId == branchId.Value) : query;
        }

        private static IQueryable<ParkingHistoryItemDto> ApplyBranchScope(IQueryable<ParkingHistoryItemDto> query, Guid? branchId)
        {
            return branchId.HasValue ? query.Where(x => x.BranchId == branchId.Value) : query;
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
                    UserId = x.UserId,
                    BranchId = x.BranchId,
                    BranchName = x.Branch != null ? x.Branch.Name : null
                });
        }
    }
}
