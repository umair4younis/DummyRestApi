using System.Collections.Generic;
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Generic wrapper for OPUS API responses (used for GET, POST, PATCH).
    /// </summary>
    public class OpusApiResponse<TResource>
    {
        [JsonProperty("errors")]
        public List<OpuxErrorDetail> Errors { get; set; } = new List<OpuxErrorDetail>();

        [JsonProperty("warnings")]
        public List<OpuxWarningDetail> Warnings { get; set; } = new List<OpuxWarningDetail>();

        [JsonProperty("resource")]
        public TResource Resource { get; set; }
    }
}