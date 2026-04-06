using Newtonsoft.Json;
using System.Collections.Generic;


namespace Example.OPUS.Models
{
    public class AccountSegmentDelta
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("swaps")]
        public List<SwapDeltaInfo> Swaps { get; set; }
    }
}
