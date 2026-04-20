namespace Puma.MDE.Data
{
    public class HVBMassDividendCap
    {
        private int iD;

        public int ID
        {
            get { return iD; }
            set { iD = value; }
        }

        private string capMaturity;

        public string CapMaturity
        {
            get { return capMaturity; }
            set { capMaturity = value; }
        }

        private double baseYield;

        public double BaseYield
        {
            get { return baseYield; }
            set { baseYield = value; }
        }

        private double cutOff;

        public double CutOff
        {
            get { return cutOff; }
            set { cutOff = value; }
        }

        private double cap;

        public double Cap
        {
            get { return cap; }
            set { cap = value; }
        }

        private double method;

        public double Method
        {
            get { return method; }
            set { method = value; }
        }

        private double internalTaxScaling;

        public double InternalTaxScaling
        {
            get { return internalTaxScaling; }
            set { internalTaxScaling = value; }
        }

        private double liquidityImpactFloor;

        public double LiquidityImpactFloor
        {
            get { return liquidityImpactFloor; }
            set { liquidityImpactFloor = value; }
        }

        private double liquidityImpactScaling;

        public double LiquidityImpactScaling
        {
            get { return liquidityImpactScaling; }
            set { liquidityImpactScaling = value; }
        }

        private bool isDirty;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }
    }
}
