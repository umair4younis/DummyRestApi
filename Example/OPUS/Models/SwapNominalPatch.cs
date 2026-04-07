
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class SwapNominalPatch
    {
        [JsonProperty("nominal")]
        public AmountValue Nominal { get; set; }
    }
}
