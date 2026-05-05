namespace SmartParking.Models
{
    public class ParkingLot
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid BranchId { get; set; }

        public Branch Branch { get; set; }

        public ICollection<Zone> Zones { get; set; }
    }
}
