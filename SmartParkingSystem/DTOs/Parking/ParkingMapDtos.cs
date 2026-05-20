using System.Text.Json.Serialization;

namespace SmartParking.DTOs.Parking
{
    public class ParkingMapDto
    {
        [JsonPropertyName("branchId")]
        public Guid BranchId { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; } = 40;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 24;

        [JsonPropertyName("elements")]
        public List<ParkingMapElementDto> Elements { get; set; } = new();

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class ParkingMapElementDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonPropertyName("type")]
        public string Type { get; set; } = "custom";

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("sourceId")]
        public string? SourceId { get; set; }

        [JsonPropertyName("sourceType")]
        public string? SourceType { get; set; }

        [JsonPropertyName("parentId")]
        public string? ParentId { get; set; }

        [JsonPropertyName("x")]
        public double X { get; set; } = 2;

        [JsonPropertyName("y")]
        public double Y { get; set; } = 2;

        [JsonPropertyName("width")]
        public double Width { get; set; } = 3;

        [JsonPropertyName("height")]
        public double Height { get; set; } = 2;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#2563eb";
    }
}
