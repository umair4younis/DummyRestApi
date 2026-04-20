namespace Puma.MDE.Data
{
    public class HVBUnderlyingTypes
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        private string underlyingType;

        public string UnderlyingType
        {
            get { return underlyingType; }
            set { underlyingType = value; }
        }
        private double fundingRatio;

        public double FundingRatio
        {
            get { return fundingRatio; }
            set { fundingRatio = value; }
        }
        private string maturityMin;

        public string MaturityMin
        {
            get { return maturityMin; }
            set { maturityMin = value; }
        }

        public bool isDirty;
        public bool IsDirty
        {
            get;
            set;
        }

        public override string ToString()
        {
            return UnderlyingType;
        }
    }
}
