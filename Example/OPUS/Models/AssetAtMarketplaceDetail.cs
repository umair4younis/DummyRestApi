using Newtonsoft.Json;
using System;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Detailed information for each AssetAtMarketplace entry in a Total Return Swap.
    /// Updated to match the exact fields from the latest JSON response.
    /// </summary>
    public class AssetAtMarketplaceDetail
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

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

        [JsonProperty("lastQuote")]
        public object LastQuote { get; set; }   // Can be string UUID or full object

        [JsonProperty("tradable")]
        public bool? Tradable { get; set; }

        [JsonProperty("tradeCurrency")]
        public TradeCurrency TradeCurrency { get; set; }

        [JsonProperty("useQuoteFactorHistorically")]
        public bool UseQuoteFactorHistorically { get; set; }

        // Additional fields present in JSON
        [JsonProperty("Bloomberg Query")]
        public object BloombergQuery { get; set; }

        [JsonProperty("Bloomberg Ticker")]
        public object BloombergTicker { get; set; }

        [JsonProperty("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonProperty("deletedBy")]
        public string DeletedBy { get; set; }

        [JsonProperty("Exchange Asset Identifier")]
        public object ExchangeAssetIdentifier { get; set; }

        [JsonProperty("FIGI")]
        public object Figi { get; set; }

        [JsonProperty("Sedol")]
        public object Sedol { get; set; }

        [JsonProperty("UC_ID")]
        public object UcId { get; set; }
    }
}