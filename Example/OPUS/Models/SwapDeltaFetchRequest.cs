using Newtonsoft.Json;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.Models
{
    public class SwapDeltaFetchRequest
    {
        [JsonProperty("accountSegments")]
        public List<string> AccountSegments { get; set; }
    }
}