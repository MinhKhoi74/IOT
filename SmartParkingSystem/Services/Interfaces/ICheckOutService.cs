using SmartParking.DTOs;

namespace SmartParking.Services.Interfaces
{
    public interface ICheckOutService
    {
        Task<CheckOutResult> ProcessCheckOutAsync(CheckOutRequest request);
        Task<CheckOutResult> ConfirmPendingPaymentAsync(int checkOutId, ConfirmCheckOutPaymentRequest request);
        decimal CalculateFee(TimeSpan duration);
    }
}
