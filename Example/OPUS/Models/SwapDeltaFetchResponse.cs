using Newtonsoft.Json;
using System.Collections.Generic;


namespace Example.OPUS.Models
{
    public class SwapDeltaFetchResponse
    {
        [JsonProperty("accountSegments")]
        public List<AccountSegmentDelta> AccountSegments { get; set; }
    }
}
