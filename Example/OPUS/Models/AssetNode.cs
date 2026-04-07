using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class AssetNode
    {
        public long id { get; set; }

        public string name { get; set; }

        public string uuid { get; set; }

        [JsonProperty("__typename")]
        public string __typename { get; set; }

        public Symbol[] symbols { get; set; }
    }
}
