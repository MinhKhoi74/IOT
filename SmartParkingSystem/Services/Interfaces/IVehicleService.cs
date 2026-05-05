using SmartParking.DTOs.Vehicle;

namespace SmartParking.Services.Interfaces
{
    public interface IVehicleService
    {
        Task CreateVehicleAsync(string userId, CreateVehicleDto dto);

        Task<List<VehicleResponseDto>> GetMyVehiclesAsync(string userId);

        Task UpdateVehicleAsync(Guid id, string userId, UpdateVehicleDto dto);

        Task DeleteVehicleAsync(Guid id, string userId);
    }
}
