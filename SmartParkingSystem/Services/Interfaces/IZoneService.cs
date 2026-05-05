using SmartParking.DTOs.Branch;
using SmartParking.Models;

namespace SmartParking.Services.Interfaces
{
    public interface IZoneService
    {
        Task CreateAsync(ZoneDto dto);

        Task<List<Zone>> GetAllAsync();

        Task UpdateAsync(Guid id, ZoneDto dto);

        Task DeleteAsync(Guid id);
    }
}
