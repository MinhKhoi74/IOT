namespace SmartParking.DTOs.Branch
{
    public class AvailableSlotResponseDto
    {
        public int AvailableCount { get; set; }

        public List<AvailableSlotDto> Slots { get; set; }
    }
}
