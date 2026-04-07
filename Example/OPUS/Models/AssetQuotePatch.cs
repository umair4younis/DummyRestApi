using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;


namespace Puma.MDE.OPUS.Models
{
    /// <summary>
    /// PATCH DTO for updating an existing quote.
    /// All fields are optional — only include what you want to change.
    /// </summary>
    public class AssetQuotePatch
    {
        [JsonProperty("ask", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Ask { get; set; }

        [JsonProperty("bid", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Bid { get; set; }

        [JsonProperty("closingQuoteOfDay", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ClosingQuoteOfDay { get; set; }

        [JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [JsonProperty("high", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? High { get; set; }

        [JsonProperty("lastCheckout", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastCheckout { get; set; }

        [JsonProperty("lastQuoteOfDay", NullValueHandling = NullValueHandling.Ignore)]
        public bool? LastQuoteOfDay { get; set; }

        [JsonProperty("low", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Low { get; set; }

        [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Time { get; set; }

        [JsonProperty("timeZone", NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(50)]
        public string TimeZone { get; set; }

        [JsonProperty("updatedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("updateUser", NullValueHandling = NullValueHandling.Ignore)]
        [StringLength(100)]
        public string UpdateUser { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public AmountValue Value { get; set; }
    }
}
