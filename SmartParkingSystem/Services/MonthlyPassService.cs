using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs;
using SmartParking.Models;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class MonthlyPassService : IMonthlyPassService
    {
        private const string MonthlyAmountSettingKey = "MonthlyPass.MonthlyAmount";
        private const decimal DefaultMonthlyAmount = 200000m;
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

        public async Task<decimal> GetMonthlyAmountAsync()
        {
            var value = await _context.SystemSettings
                .AsNoTracking()
                .Where(x => x.Key == MonthlyAmountSettingKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            return decimal.TryParse(value, out var amount) && amount > 0
                ? amount
                : DefaultMonthlyAmount;
        }

        public async Task<decimal> SetMonthlyAmountAsync(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Monthly amount must be greater than 0.");
            }

            var setting = await _context.SystemSettings.FindAsync(MonthlyAmountSettingKey);
            if (setting is null)
            {
                setting = new SystemSetting { Key = MonthlyAmountSettingKey };
                _context.SystemSettings.Add(setting);
            }

            setting.Value = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            setting.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return amount;
        }

        public int CalculateMonthCount(DateTime validFrom, DateTime validTo)
        {
            var from = validFrom.Date;
            var to = validTo.Date;
            if (to < from)
            {
                return 1;
            }

            var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
            if (to.Day > from.Day)
            {
                months++;
            }

            return Math.Max(1, months);
        }

        public async Task<decimal> CalculateAmountAsync(DateTime validFrom, DateTime validTo)
        {
            return await GetMonthlyAmountAsync() * CalculateMonthCount(validFrom, validTo);
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
            pass.Amount = request.Amount > 0
                ? request.Amount
                : await CalculateAmountAsync(request.ValidFrom, request.ValidTo);
            pass.IsActive = request.IsActive;
            pass.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await UpdateRedisCacheForPassAsync(pass);
            return ToDto(pass);
        }

        public async Task<MonthlyPassDto> RegisterForUserAsync(string userId, MonthlyPassUpsertRequest request)
        {
            var plate = NormalizePlate(request.LicensePlate);
            var ownsVehicle = await _context.Vehicle.AnyAsync(x => x.UserId == userId && x.LicensePlate == plate);
            if (!ownsVehicle)
            {
                throw new ArgumentException("License plate is not registered to this user.");
            }

            request.LicensePlate = plate;
            request.IsActive = true;
            return await UpsertAsync(request);
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
                Amount = pass.Amount,
                IsActive = pass.IsActive
            };
        }
    }
}
