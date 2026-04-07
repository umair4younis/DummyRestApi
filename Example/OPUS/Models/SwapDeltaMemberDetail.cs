using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Models
{
    public class SwapDeltaMemberDetail
    {
        [JsonProperty("assetUUID")]
        public string AssetUuid { get; set; }

        [JsonProperty("assetName")]
        public string AssetName { get; set; }

        [JsonProperty("assetIsin")]
        public string AssetIsin { get; set; }

        [JsonProperty("currentPieces")]
        public decimal CurrentPieces { get; set; }

        [JsonProperty("currentWeight")]
        public decimal CurrentWeight { get; set; }

        [JsonProperty("targetPieces")]
        public decimal TargetPieces { get; set; }

        [JsonProperty("targetWeight")]
        public decimal TargetWeight { get; set; }

        [JsonProperty("deltaPieces")]
        public decimal DeltaPieces { get; set; }

        [JsonProperty("deltaWeight")]
        public decimal DeltaWeight { get; set; }
    }
}