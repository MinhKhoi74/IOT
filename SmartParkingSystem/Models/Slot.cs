using SmartParking.Models.Enums;

namespace SmartParking.Models
{
    public class Slot
    {
        public Guid Id { get; set; }

        public string SlotCode { get; set; }

        public SlotStatus Status { get; set; } = SlotStatus.Available;

        public Guid ZoneId { get; set; }

        public Zone Zone { get; set; }
    }
}
