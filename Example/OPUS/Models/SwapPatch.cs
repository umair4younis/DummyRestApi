using Newtonsoft.Json;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.Models
{
    public class SwapPatch
    {
        [JsonProperty("nominal")]
        public AmountValue Nominal { get; set; }

        [JsonProperty("assetAtMarketplaces")]
        public List<AssetAtMarketplaceDetail> AssetAtMarketplaces { get; set; }
    }
}
