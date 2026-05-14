
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class PortfolioGraphQlRateable
    {
        public long id { get; set; }

        public string uuid { get; set; }

        public string name { get; set; }

        [JsonProperty("__typename")]
        public string __typename { get; set; }

        public PortfolioGraphQlNominal nominal { get; set; }

        public PercentAmountValue mtmFromFinancing { get; set; }

        public PercentAmountValue swapValue { get; set; }

        public PortfolioGraphQlAssetComposition asset { get; set; }
    }
}
