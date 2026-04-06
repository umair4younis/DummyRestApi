using Newtonsoft.Json;


namespace Example.OPUS.Models
{
    public class SwapDeltaMember
    {
        /// <summary>
        /// UUID of the asset (component) being updated
        /// </summary>
        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        /// <summary>
        /// Current number of pieces (holdings count)
        /// </summary>
        [JsonProperty("currentPieces")]
        public decimal CurrentPieces { get; set; }

        /// <summary>
        /// Current weight (percentage or ratio)
        /// </summary>
        [JsonProperty("currentWeight")]
        public decimal CurrentWeight { get; set; }
    }
}
