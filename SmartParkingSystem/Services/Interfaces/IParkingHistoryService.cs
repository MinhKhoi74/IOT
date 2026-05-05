using SmartParking.DTOs.Parking;

namespace SmartParking.Services.Interfaces
{
    public interface IParkingHistoryService
    {
        Task<List<ParkingHistoryItemDto>> GetHistoryByPlateAsync(string plate);
        Task<List<ParkingHistoryItemDto>> GetMyHistoryAsync(string userId);
        Task<ParkingHistoryItemDto?> GetLatestCheckInAsync();
        Task<ParkingHistoryItemDto?> GetLatestCheckOutAsync();
        Task<ParkingDashboardDto> GetDashboardAsync();
        Task<ParkingHistoryItemDto?> GetSessionDetailAsync(int id);
    }
}
