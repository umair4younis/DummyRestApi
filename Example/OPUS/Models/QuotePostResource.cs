using Newtonsoft.Json;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Resource returned in POST quote response (created quote).
    /// </summary>
    public class QuotePostResource
    {
        [JsonProperty("method")]
        public string Method { get; set; } = "POST";

        [JsonProperty("type")]
        public string Type { get; set; } = "QUOTE";

        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("quote")]
        public AssetQuote Quote { get; set; }
    }
}