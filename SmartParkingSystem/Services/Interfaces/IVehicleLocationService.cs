using SmartParking.DTOs;

namespace SmartParking.Services.Interfaces
{
    public interface IVehicleLocationService
    {
        Task<VehicleLocationDetectionResult> ProcessDetectionAsync(VehicleLocationDetectionRequest request);
        Task<VehicleLocationDetectionBatchResult> ProcessDetectionBatchAsync(VehicleLocationDetectionBatchRequest request);
        Task<List<VehicleLocationDto>> GetMyLatestLocationsAsync(string userId);
        Task<List<VehicleLocationDto>> GetRecentAlertsAsync(int take = 50);
        Task<VehicleLocationDto?> GetLocationAlertAsync(int id);
    }
}
