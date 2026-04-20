namespace Puma.MDE.Data
{
    public class HVBMassQuoteImprovement
    {
        public bool Equals(HVBMassQuoteImprovement hvbMassQuoteImprovements)
        {
            if (ReferenceEquals(null, hvbMassQuoteImprovements)) return false;
            if (ReferenceEquals(this, hvbMassQuoteImprovements)) return true;
            return hvbMassQuoteImprovements.MultiUnderlying == MultiUnderlying && hvbMassQuoteImprovements.UnderlyingTypeId == UnderlyingTypeId && hvbMassQuoteImprovements.ClientId == ClientId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassQuoteImprovement)) return false;
            return Equals((HVBMassQuoteImprovement)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (MultiUnderlying * 397) ^ UnderlyingTypeId;
            }
        }
        public int MultiUnderlying { get; set; }
        public int UnderlyingTypeId { get; set; }
        public int ClientId { get; set; }
        public int DoImprovement { get; set; }
        public double StartMargin { get; set; }
        public double MinMargin { get; set; }
        public double MaxMargin { get; set; }
        public double ImprovementFloor { get; set; }
        public double ImprovementRange { get; set; }
        public double WideningPriceFactor { get; set; }
        public double ImprovementPriceFactor { get; set; }
        public double BarrierStrikeThreshold { get; set; }
        public int MaturityThreshold { get; set; }
        public int TimeElapsedThreshold { get; set; }
    }
}
