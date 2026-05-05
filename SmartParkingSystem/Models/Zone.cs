namespace SmartParking.Models
{
    public class Zone
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string VehicleType { get; set; } // Car / Motorbike

        public Guid ParkingLotId { get; set; }

        public ParkingLot ParkingLot { get; set; }

        public ICollection<Slot> Slots { get; set; }
    }
}
