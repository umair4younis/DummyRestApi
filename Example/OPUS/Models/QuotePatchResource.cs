using Newtonsoft.Json;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Resource returned in PATCH quote response (updated quote).
    /// </summary>
    public class QuotePatchResource
    {
        [JsonProperty("method")]
        public string Method { get; set; } = "PATCH";

        [JsonProperty("type")]
        public string Type { get; set; } = "QUOTE";

        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("updatedQuote")]
        public AssetQuote UpdatedQuote { get; set; }
    }
}