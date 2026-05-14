
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class PortfolioGraphQlAssetComposition
    {
        public long id { get; set; }

        public string uuid { get; set; }

        public string name { get; set; }

        [JsonProperty("__typename")]
        public string __typename { get; set; }

        public PortfolioGraphQlMember[] members { get; set; }
    }
}
