namespace Puma.MDE.Data
{
    public class PumaETGlobalSettings
    {
        public int    Id                        { get; set; }
        public int    TradablePrices            { get; set; }
        public int    AutoCallableTrades        { get; set; }  
        public double AdditionalMarginAddon     { get; set; }
        public double MaxMarginChange           { get; set; }
        public int    OrdersWithoutCompetitors  { get; set; }
        public int    SendEncodedTextLen        { get; set; }
        public int    SolverRequestTradable     { get; set; }
        public int    SolverCompetCheckEnabled  { get; set; }
        public double MaxPreMarketSpotChange    { get; set; }
        public int    SpotterSuspensionCheck    { get; set; }
        public int    ImproveMarginPrice        { get; set; }   // Trac: #35785 - point 4b: Remove the dropdowns “Improve Margin Price” to “Improve Margin Barrier” from the maintenance of “Global eTrading Parameters” (no longer required).
        public int    ImproveMarginStrike       { get; set; }   // Event though the dropdowns were removed, the model was not changed hence no  
        public int    ImproveMarginCoupon       { get; set; }   // changes in the PumaETGlobalSettings classs or corresponding  
        public int    ImproveMarginBarrier      { get; set; }   // nhibernate configuration for the puma_et_global_settings table
        public int    ForwardPricingTradable    { get; set; }
        public int    CorporateActionCheck      { get; set; }
    }
}
