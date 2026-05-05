namespace SmartParking.DTOs.Branch
{
    public class BranchFullDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public List<ParkingLotDetailDto> ParkingLots { get; set; } = new();
    }
}
