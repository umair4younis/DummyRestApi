using Newtonsoft.Json;
using System.Collections.Generic;


namespace Example.OPUS.Models
{
    public class SwapDeltaFetchRequest
    {
        [JsonProperty("accountSegments")]
        public List<string> AccountSegments { get; set; }
    }
}