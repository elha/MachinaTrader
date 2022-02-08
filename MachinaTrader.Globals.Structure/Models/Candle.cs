using System;
using System.Text.Json.Serialization;

namespace MachinaTrader.Globals.Structure.Models
{
    public class Candle
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonPropertyName("high")]
        public decimal High { get; set; }
        [JsonPropertyName("low")]
        public decimal Low { get; set; }
        [JsonPropertyName("open")]
        public decimal Open { get; set; }
        [JsonPropertyName("close")]
        public decimal Close { get; set; }
        [JsonPropertyName("volume")]
        public decimal Volume { get; set; }
    }
}
