using SmartParking.DTOs.Branch;
using SmartParking.Models;

namespace SmartParking.Services.Interfaces
{
    public interface IParkingLotService
    {
        Task CreateAsync(ParkingLotDto dto);

        Task<List<ParkingLot>> GetAllAsync();
        Task<AvailableSlotResponseDto> GetAvailableSlotsAsync(Guid parkingLotId);

        Task UpdateAsync(Guid id, ParkingLotDto dto);

        Task DeleteAsync(Guid id);
    }
}
