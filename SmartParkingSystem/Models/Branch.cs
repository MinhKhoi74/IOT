using SmartParking.Models.Identity;

namespace SmartParking.Models
{
    public class Branch
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }
        public string? ManagerId { get; set; }
        public int MaxVehicleCapacity { get; set; } = 100;
        public string? ParkingMapJson { get; set; }
        public DateTime? ParkingMapUpdatedAt { get; set; }

        public ApplicationUser? Manager { get; set; }

        public ICollection<ApplicationUser> Staff { get; set; }
        public ICollection<ParkingLot> ParkingLots { get; set; }
    }
}
