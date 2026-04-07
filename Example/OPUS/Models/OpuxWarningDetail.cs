using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Warning detail in OPUS API responses.
    /// </summary>
    public class OpuxWarningDetail
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
