using System.Text.Json.Serialization;

namespace MarketDataSimulator.Models
{
    public record Item
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("duplicate")]
        public bool Duplicate { get; set; }
    }
}
