namespace SmartParking.Services.Interfaces
{
    public interface IBranchAuthorizationService
    {
        Task EnsureCanManageBranchAsync(Guid branchId, string userId, bool isAdmin);
        Task EnsureCanManageParkingLotAsync(Guid parkingLotId, string userId, bool isAdmin);
        Task EnsureCanManageZoneAsync(Guid zoneId, string userId, bool isAdmin);
        Task EnsureCanManageSlotAsync(Guid slotId, string userId, bool isAdmin);
    }
}
