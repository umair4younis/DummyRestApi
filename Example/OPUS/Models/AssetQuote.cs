using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Example.OPUS.Utilities;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Represents a quote for an asset in a marketplace (e.g. home marketplace of a swap).
    /// </summary>
    public class AssetQuote
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("ask")]
        public decimal? Ask { get; set; }

        [JsonProperty("bid")]
        public decimal? Bid { get; set; }

        [JsonProperty("closingQuoteOfDay")]
        public bool ClosingQuoteOfDay { get; set; }

        [JsonProperty("date")]
        [Required(ErrorMessage = "Quote date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [JsonProperty("high")]
        public decimal? High { get; set; }

        [JsonProperty("lastCheckout")]
        public DateTime? LastCheckout { get; set; }

        [JsonProperty("lastQuoteOfDay")]
        public bool LastQuoteOfDay { get; set; }

        [JsonProperty("low")]
        public decimal? Low { get; set; }

        [JsonProperty("time")]
        [JsonConverter(typeof(OpisIsoDateTimeConverter))]
        [Required(ErrorMessage = "Quote time is required")]
        public DateTime Time { get; set; }

        [JsonProperty("timeZone")]
        [Required(ErrorMessage = "Time zone is required")]
        [StringLength(50)]
        public string TimeZone { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("updateUser")]
        [StringLength(100)]
        public string UpdateUser { get; set; }

        [JsonProperty("value")]
        [Required(ErrorMessage = "Quote value is required")]
        public AmountValue Value { get; set; }
    }
}
