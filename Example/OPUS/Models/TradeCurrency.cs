
using Newtonsoft.Json;


namespace Example.OPUS.Models
{
    public class TradeCurrency
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
