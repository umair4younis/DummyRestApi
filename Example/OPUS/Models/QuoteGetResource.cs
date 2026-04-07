using System.Collections.Generic;
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Resource returned in GET quote response.
    /// </summary>
    public class QuoteGetResource
    {
        [JsonProperty("quotes")]
        public List<AssetQuote> Quotes { get; set; } = new List<AssetQuote>();

        [JsonProperty("metadata")]
        public object Metadata { get; set; } // optional, can be expanded if known
    }
}