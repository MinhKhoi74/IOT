namespace SmartParking.DTOs.Branch
{
    public class ParkingLotDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ZoneDetailDto> Zones { get; set; } = new();
    }
}
