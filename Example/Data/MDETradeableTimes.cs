namespace Puma.MDE.Data
{
    public class MDETradeableTimes
    {
        public string Market { get; set; }
        public string IndicativeStart { get; set; }
        public string IndicativeEnd { get; set; }
        public string TradeableStart { get; set; }
        public string TradeableEnd { get; set; }
        public bool IsDirty { get; set; }
        public bool IsValid { get; set; }
    }
}
