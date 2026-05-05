using StackExchange.Redis;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private const string PARKING_CHECKINS_KEY = "parking:checkins";
        private const string MONTHLY_PASSES_KEY = "parking:monthly-passes";

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        public async Task<bool> IsPlateActiveAsync(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return false;

            plate = plate.ToUpper().Trim();
            return await _db.HashExistsAsync(PARKING_CHECKINS_KEY, plate);
        }

        public async Task<DateTime?> GetCheckinTimeAsync(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return null;

            plate = plate.ToUpper().Trim();
            var value = await _db.HashGetAsync(PARKING_CHECKINS_KEY, plate);
            
            if (value.HasValue && long.TryParse(value.ToString(), out var ticks))
            {
                return new DateTime(ticks);
            }
            return null;
        }

        public async Task AddCheckinAsync(string plate, DateTime checkinTime)
        {
            if (string.IsNullOrEmpty(plate))
                throw new ArgumentException("License plate cannot be empty");

            plate = plate.ToUpper().Trim();
            await _db.HashSetAsync(PARKING_CHECKINS_KEY, plate, checkinTime.Ticks.ToString());
        }

        public async Task<DateTime?> GetAndRemoveCheckinAsync(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return null;

            plate = plate.ToUpper().Trim();
            var value = await _db.HashGetAsync(PARKING_CHECKINS_KEY, plate);
            
            if (value.HasValue && long.TryParse(value.ToString(), out var ticks))
            {
                await _db.HashDeleteAsync(PARKING_CHECKINS_KEY, plate);
                return new DateTime(ticks);
            }
            
            return null;
        }

        public async Task RemoveCheckinAsync(string plate)
        {
            if (string.IsNullOrEmpty(plate))
                return;

            plate = plate.ToUpper().Trim();
            await _db.HashDeleteAsync(PARKING_CHECKINS_KEY, plate);
        }

        public async Task<bool> HasActiveMonthlyPassAsync(string plate, DateTime atTime)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return false;

            plate = plate.ToUpper().Trim();
            var value = await _db.HashGetAsync(MONTHLY_PASSES_KEY, plate);

            if (!value.HasValue || !long.TryParse(value.ToString(), out var validToTicks))
                return false;

            return new DateTime(validToTicks).Date >= atTime.Date;
        }

        public async Task SetMonthlyPassAsync(string plate, DateTime validTo)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return;

            plate = plate.ToUpper().Trim();
            await _db.HashSetAsync(MONTHLY_PASSES_KEY, plate, validTo.Date.Ticks.ToString());
        }

        public async Task RemoveMonthlyPassAsync(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return;

            plate = plate.ToUpper().Trim();
            await _db.HashDeleteAsync(MONTHLY_PASSES_KEY, plate);
        }

        public async Task SyncMonthlyPassesAsync(IEnumerable<(string Plate, DateTime ValidTo)> activePasses)
        {
            await _db.KeyDeleteAsync(MONTHLY_PASSES_KEY);

            var entries = activePasses
                .Where(pass => !string.IsNullOrWhiteSpace(pass.Plate))
                .Select(pass => new HashEntry(
                    pass.Plate.ToUpper().Trim(),
                    pass.ValidTo.Date.Ticks.ToString()))
                .ToArray();

            if (entries.Length > 0)
            {
                await _db.HashSetAsync(MONTHLY_PASSES_KEY, entries);
            }
        }
    }
}
