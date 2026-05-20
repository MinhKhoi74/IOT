using SmartParking.DTOs;

namespace SmartParking.Services.Interfaces
{
    public interface IMonthlyPassService
    {
        Task<List<MonthlyPassDto>> GetAllAsync();
        Task<decimal> GetMonthlyAmountAsync();
        Task<decimal> SetMonthlyAmountAsync(decimal amount);
        int CalculateMonthCount(DateTime validFrom, DateTime validTo);
        Task<decimal> CalculateAmountAsync(DateTime validFrom, DateTime validTo);
        Task<MonthlyPassDto> UpsertAsync(MonthlyPassUpsertRequest request);
        Task<MonthlyPassDto> RegisterForUserAsync(string userId, MonthlyPassUpsertRequest request);
        Task DeleteAsync(int id);
        Task<bool> HasActivePassAsync(string licensePlate, DateTime atTime);
        Task SyncRedisCacheAsync(DateTime atTime);
    }
}
