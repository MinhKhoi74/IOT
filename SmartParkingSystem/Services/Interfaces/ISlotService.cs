using SmartParking.DTOs.Branch;
using SmartParking.Models;

namespace SmartParking.Services.Interfaces
{
    public interface ISlotService
    {
        Task CreateAsync(SlotDto dto);

        Task<List<Slot>> GetAllAsync();
        Task<List<Slot>> GetByBranchAsync(Guid branchId);

        Task UpdateAsync(Guid id, SlotDto dto);
        Task DeleteAsync(Guid id);
        Task UpdateStatusAsync(Guid id, UpdateSlotStatusDto dto);
    }
}
