
using Newtonsoft.Json;


namespace Example.OPUS.Models
{
    public class SwapNominalPatch
    {
        [JsonProperty("nominal")]
        public AmountValue Nominal { get; set; }
    }
}
