using Newtonsoft.Json;

namespace Puma.MDE.OPUS.Models
{
    public class AssetAtMarketplacePatch
    {
        [JsonProperty("home")]
        public bool? Home { get; set; }

        [JsonProperty("quoteFactor")]
        public AmountValue QuoteFactor { get; set; }

        [JsonProperty("lotSize")]
        public AmountValue LotSize { get; set; }

        [JsonProperty("quoteSource")]
        public string QuoteSource { get; set; }

        [JsonProperty("quoteUnit")]
        public string QuoteUnit { get; set; }

        [JsonProperty("reference")]
        public bool? Reference { get; set; }
        
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("lastQuote")]
        public string LastQuoteUuid { get; set; }

        [JsonProperty("tradable")]
        public bool? Tradable { get; set; }

        [JsonProperty("tradeCurrency")]
        public TradeCurrency TradeCurrency { get; set; }
    }
}