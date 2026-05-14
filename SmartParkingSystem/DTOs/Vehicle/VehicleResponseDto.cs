using SmartParking.Models.Enums;

namespace SmartParking.DTOs.Vehicle
{
    public class VehicleResponseDto
    {
        public Guid Id { get; set; }

        public string LicensePlate { get; set; }

        public string? UserId { get; set; }

        public string? OwnerName { get; set; }

        public string? OwnerEmail { get; set; }

        public VehicleType VehicleType { get; set; }

        public string Brand { get; set; }

        public string Color { get; set; }

        public bool IsDefault { get; set; }
    }
}
