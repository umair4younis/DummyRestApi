using System;


namespace Puma.MDE.OPUS.Models
{
    public class SwapQuoteData
    {
        public decimal? Ask { get; set; }
        public decimal? Bid { get; set; }
        public string AssetAtMarketplace { get; set; }
        public bool ClosingQuoteOfDay { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreationUser { get; set; }
        public DateTime Date { get; set; }                    // trading date
        public DateTime? DeletedAt { get; set; }
        public string DeletedBy { get; set; }
        public decimal? High { get; set; }
        public decimal? Low { get; set; }
        public bool LastQuoteOfDay { get; set; }
        public DateTime Time { get; set; }                    // timestamp of the quote
        public string TimeZone { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdateUser { get; set; }
        public AmountValue Value { get; set; }                // the actual quote value
    }
}