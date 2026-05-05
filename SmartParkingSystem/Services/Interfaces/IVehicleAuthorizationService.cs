namespace SmartParking.Services.Interfaces
{
    public interface IVehicleAuthorizationService
    {
        Task ValidateVehicleOwnership(Guid vehicleId, string userId);
    }
}
