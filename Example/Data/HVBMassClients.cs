namespace Puma.MDE.Data
{
    public class HVBMassClients
    {

        public bool Equals(HVBMassClients hvbMassClients)
        {
            if (ReferenceEquals(null, hvbMassClients)) return false;
            if (ReferenceEquals(this, hvbMassClients)) return true;
            return hvbMassClients.UnderlyingTypeID == UnderlyingTypeID && hvbMassClients.ClientID == ClientID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassClients)) return false;
            return Equals((HVBMassClients)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnderlyingTypeID * 397) ^ ClientID;
            }
        }

        private int clientID;

        public int ClientID
        {
            get { return clientID; }
            set { clientID = value; }
        }
        private string clientName;

        public string ClientName
        {
            get { return clientName; }
            set { clientName = value; }
        }
        private int underlyingTypeID;

        public int UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
        private double competitiveFactor;

        public double CompetitiveFactor
        {
            get { return competitiveFactor; }
            set { competitiveFactor = value; }
        }
        private double fundingFactor;

        public double FundingFactor
        {
            get { return fundingFactor; }
            set { fundingFactor = value; }
        }

        private double minMarginAmount;

        public double MinMarginAmount
        {
            get { return minMarginAmount; }
            set { minMarginAmount = value; }
        }
        private double maxCompetitorDeviation;

        public double MaxCompetitorDeviation
        {
            get { return maxCompetitorDeviation; }
            set { maxCompetitorDeviation = value; }
        }
        private double matWGHTFundingFactor;

        public double MatWGHTFundingFactor
        {
            get { return matWGHTFundingFactor; }
            set { matWGHTFundingFactor = value; }
        }


        private bool volatilityAgeCheck;

        public bool VolatilityAgeCheck
        {
            get { return volatilityAgeCheck; }
            set { volatilityAgeCheck = value; }
        }


        private double maxSecMarketNotionalInEUR ;

        public double MaxSecMarketNotionalInEUR
        {
            get { return maxSecMarketNotionalInEUR; }
            set { maxSecMarketNotionalInEUR = value; }
        }

        public override string ToString()
        {
            return ClientName;
        }

    }
}
