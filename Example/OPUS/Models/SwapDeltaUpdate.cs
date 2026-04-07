using System.Collections.Generic;
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Payload for PUT /unicredit-swap-service/api/swaps/{swapId} – Update Delta
    /// </summary>
    public class SwapDeltaUpdate
    {
        [JsonProperty("members")]
        public List<SwapDeltaMember> Members { get; set; } = new List<SwapDeltaMember>();
    }
}
