using Newtonsoft.Json;
using System.Collections.Generic;


namespace Example.OPUS.Models
{
    public class SwapDeltaInfo
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("members")]
        public List<SwapDeltaMemberDetail> Members { get; set; }
    }
}
