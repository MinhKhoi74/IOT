using SmartParking.DTOs.Parking;
using SmartParking.DTOs.Vehicle;

namespace SmartParking.DTOs.User
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string[] Roles { get; set; } = [];
        public bool IsActive { get; set; }
        public BranchInfoDto? Branch { get; set; }
        public WalletInfoDto Wallet { get; set; } = new();
        public List<VehicleResponseDto> Vehicles { get; set; } = [];
        public List<MonthlyPassProfileDto> MonthlyPasses { get; set; } = [];
        public List<ParkingHistoryItemDto> RecentParkingHistory { get; set; } = [];
    }

    public class WalletInfoDto
    {
        public Guid? Id { get; set; }
        public decimal Balance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class MonthlyPassProfileDto
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string? OwnerPhone { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
    }
}
