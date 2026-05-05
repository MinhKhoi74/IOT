using SmartParking.DTOs;

namespace SmartParking.Services.Interfaces
{
    public interface ICheckInService
    {
        Task<CheckInResult> ProcessCheckInAsync(CheckInRequest request);
    }
}
