using SmartParking.Configurations;
using SmartParking.DTOs.Momo;

namespace SmartParking.Services.Interfaces
{
    public interface IMomoService
    {
        MomoSettings GetSettings();
        MomoCreatePaymentResultDto BuildCreatePaymentRequest(MomoCreatePaymentRequestDto request);
        Task<MomoCreatePaymentResultDto> CreatePaymentAsync(MomoCreatePaymentRequestDto request, CancellationToken cancellationToken = default);
        bool VerifyNotificationSignature(MomoPaymentNotificationDto notification);
        Dictionary<string, string> DecodeExtraData(string extraData);
        List<string> GetAvailablePaymentMethods();
    }
}
