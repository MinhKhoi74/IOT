using SmartParking.DTOs;

namespace SmartParking.Services.Interfaces
{
    public interface IMonthlyPassService
    {
        Task<List<MonthlyPassDto>> GetAllAsync();
        Task<MonthlyPassDto> UpsertAsync(MonthlyPassUpsertRequest request);
        Task<MonthlyPassDto> RegisterForUserAsync(string userId, MonthlyPassUpsertRequest request);
        Task DeleteAsync(int id);
        Task<bool> HasActivePassAsync(string licensePlate, DateTime atTime);
        Task SyncRedisCacheAsync(DateTime atTime);
    }
}
