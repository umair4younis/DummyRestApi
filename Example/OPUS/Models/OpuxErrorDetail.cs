using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Error detail in OPUS API responses.
    /// </summary>
    public class OpuxErrorDetail
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("localizedMessage")]
        public string LocalizedMessage { get; set; }
    }
}