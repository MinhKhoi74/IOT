namespace SmartParking.Services.Interfaces
{
    public interface IArduinoSerialService
    {
        Task SendCheckInOkAsync(string plateNumber, CancellationToken cancellationToken = default);
        Task SendCheckOutOkAsync(string plateNumber, CancellationToken cancellationToken = default);
    }
}
