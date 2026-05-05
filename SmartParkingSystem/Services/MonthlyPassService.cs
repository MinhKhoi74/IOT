using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.Models;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class MonthlyPassService : IMonthlyPassService
    {
        private readonly ApplicationDBContext _context;
        private readonly IRedisService _redis;

        public MonthlyPassService(ApplicationDBContext context, IRedisService redis)
        {
            _context = context;
            _redis = redis;
        }

        public async Task<List<MonthlyPassDto>> GetAllAsync()
        {
            return await _context.MonthlyPasses
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.LicensePlate)
                .Select(x => ToDto(x))
                .ToListAsync();
        }

        public async Task<MonthlyPassDto> UpsertAsync(MonthlyPassUpsertRequest request)
        {
            var plate = NormalizePlate(request.LicensePlate);
            if (string.IsNullOrWhiteSpace(plate))
            {
                throw new ArgumentException("License plate is required.");
            }

            if (request.ValidTo.Date < request.ValidFrom.Date)
            {
                throw new ArgumentException("ValidTo must be greater than or equal to ValidFrom.");
            }

            var pass = await _context.MonthlyPasses.FirstOrDefaultAsync(x => x.LicensePlate == plate);
            if (pass is null)
            {
                pass = new MonthlyPass
                {
                    LicensePlate = plate,
                    CreatedAt = DateTime.Now
                };
                _context.MonthlyPasses.Add(pass);
            }

            pass.OwnerName = request.OwnerName.Trim();
            pass.OwnerPhone = request.OwnerPhone?.Trim();
            pass.ValidFrom = request.ValidFrom.Date;
            pass.ValidTo = request.ValidTo.Date;
            pass.IsActive = request.IsActive;
            pass.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await UpdateRedisCacheForPassAsync(pass);
            return ToDto(pass);
        }

        public async Task DeleteAsync(int id)
        {
            var pass = await _context.MonthlyPasses.FindAsync(id);
            if (pass is null)
            {
                return;
            }

            _context.MonthlyPasses.Remove(pass);
            await _context.SaveChangesAsync();
            await _redis.RemoveMonthlyPassAsync(pass.LicensePlate);
        }

        public async Task<bool> HasActivePassAsync(string licensePlate, DateTime atTime)
        {
            var plate = NormalizePlate(licensePlate);
            var date = atTime.Date;
            if (await _redis.HasActiveMonthlyPassAsync(plate, date))
            {
                return true;
            }

            var hasActivePass = await _context.MonthlyPasses.AnyAsync(x =>
                x.LicensePlate == plate &&
                x.IsActive &&
                x.ValidFrom.Date <= date &&
                x.ValidTo.Date >= date);

            if (hasActivePass)
            {
                var pass = await _context.MonthlyPasses
                    .Where(x => x.LicensePlate == plate && x.IsActive && x.ValidFrom.Date <= date && x.ValidTo.Date >= date)
                    .OrderByDescending(x => x.ValidTo)
                    .FirstAsync();
                await _redis.SetMonthlyPassAsync(plate, pass.ValidTo);
            }

            return hasActivePass;
        }

        public async Task SyncRedisCacheAsync(DateTime atTime)
        {
            var date = atTime.Date;
            var activePasses = await _context.MonthlyPasses
                .Where(x => x.IsActive && x.ValidFrom.Date <= date && x.ValidTo.Date >= date)
                .ToListAsync();

            await _redis.SyncMonthlyPassesAsync(activePasses.Select(x => (x.LicensePlate, x.ValidTo)));
        }

        private static string NormalizePlate(string plate) => (plate ?? string.Empty).Trim().ToUpperInvariant();

        private async Task UpdateRedisCacheForPassAsync(MonthlyPass pass)
        {
            var today = DateTime.Today;
            if (pass.IsActive && pass.ValidFrom.Date <= today && pass.ValidTo.Date >= today)
            {
                await _redis.SetMonthlyPassAsync(pass.LicensePlate, pass.ValidTo);
                return;
            }

            await _redis.RemoveMonthlyPassAsync(pass.LicensePlate);
        }

        private static MonthlyPassDto ToDto(MonthlyPass pass)
        {
            return new MonthlyPassDto
            {
                Id = pass.Id,
                LicensePlate = pass.LicensePlate,
                OwnerName = pass.OwnerName,
                OwnerPhone = pass.OwnerPhone,
                ValidFrom = pass.ValidFrom,
                ValidTo = pass.ValidTo,
                IsActive = pass.IsActive
            };
        }
    }
}
