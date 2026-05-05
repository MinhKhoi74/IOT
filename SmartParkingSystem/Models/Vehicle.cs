using SmartParking.Models.Enums;
using SmartParking.Models.Identity;

namespace SmartParking.Models
{
    public class Vehicle
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }

        public string LicensePlate { get; set; }

        public VehicleType VehicleType { get; set; }

        public string Brand { get; set; }

        public string Color { get; set; }

        public bool IsDefault { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public ApplicationUser User { get; set; }
    }
}
