
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class TradeCurrency
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
