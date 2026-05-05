namespace SmartParking.DTOs.Branch
{
    public class ZoneDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string VehicleType { get; set; }

        public List<SlotDetailDto> Slots { get; set; } = new();
    }
}
