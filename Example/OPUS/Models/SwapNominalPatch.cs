using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// Patch payload for updating a swap's nominal value, MTM-from-financing rate, and swap value.
    /// Matches the OPUS API request body:
    /// {
    ///   "nominal":           { "quantity": 20000000, "unit": "EUR",  "type": "MONEY" },
    ///   "mtmFromFinancing":  { "quantity": 10,       "unit": "%" },
    ///   "swapValue":         { "quantity": 12.3,     "unit": "%" }
    /// }
    /// mtmFromFinancing and swapValue are optional.
    /// </summary>
    public class SwapNominalPatch
    {
        [JsonProperty("nominal")]
        public AmountValue Nominal { get; set; }

        [JsonProperty("mtmFromFinancing", NullValueHandling = NullValueHandling.Ignore)]
        public PercentAmountValue MtmFromFinancing { get; set; }

        [JsonProperty("swapValue", NullValueHandling = NullValueHandling.Ignore)]
        public PercentAmountValue SwapValue { get; set; }
    }
}
