using SmartParking.DTOs.Parking;

namespace SmartParking.Services.Interfaces
{
    public interface IParkingHistoryService
    {
        Task<List<ParkingHistoryItemDto>> GetHistoryByPlateAsync(string plate);
        Task<List<ParkingHistoryItemDto>> GetHistoryByPlateAsync(string plate, Guid? branchId);
        Task<List<ParkingHistoryItemDto>> GetHistoryAsync(Guid? branchId = null);
        Task<List<ParkingHistoryItemDto>> GetMyHistoryAsync(string userId);
        Task<ParkingHistoryItemDto?> GetLatestCheckInAsync(Guid? branchId = null);
        Task<ParkingHistoryItemDto?> GetLatestCheckOutAsync(Guid? branchId = null);
        Task<ParkingDashboardDto> GetDashboardAsync(Guid? branchId = null);
        Task<ParkingHistoryItemDto?> GetSessionDetailAsync(int id, Guid? branchId = null);
    }
}
