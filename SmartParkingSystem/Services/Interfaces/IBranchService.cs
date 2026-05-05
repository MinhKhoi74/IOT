using SmartParking.DTOs.Branch;
using SmartParking.Models;

namespace SmartParking.Services.Interfaces
{
    public interface IBranchService
    {
        Task CreateAsync(BranchCreateDto dto);

        Task<IEnumerable<BranchDto>> GetAllAsync();

        Task UpdateAsync(Guid id, BranchDto dto);

        Task DeleteAsync(Guid id);
        Task<BranchFullDto> GetFullAsync(Guid id);
    }
}
