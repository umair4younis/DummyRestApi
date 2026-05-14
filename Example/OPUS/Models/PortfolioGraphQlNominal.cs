
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class PortfolioGraphQlNominal
    {
        [JsonProperty("__typename")]
        public string __typename { get; set; }

        public AmountValue lastValue { get; set; }
    }
}
