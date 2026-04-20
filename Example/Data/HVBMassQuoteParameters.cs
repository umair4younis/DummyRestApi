namespace Puma.MDE.Data
{
    public class HVBMassQuoteParameters
    {

        public bool Equals(HVBMassQuoteParameters hvbMassQuoteParameters)
        {
            if (ReferenceEquals(null, hvbMassQuoteParameters)) return false;
            if (ReferenceEquals(this, hvbMassQuoteParameters)) return true;
            return hvbMassQuoteParameters.UnderlyingTypeID == UnderlyingTypeID && hvbMassQuoteParameters.ClientID == ClientID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassQuoteParameters)) return false;
            return Equals((HVBMassQuoteParameters)obj);
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
        private int underlyingTypeID;

        public int UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
        private int shiftClassID;

        public int ShiftClassID
        {
            get { return shiftClassID; }
            set { shiftClassID = value; }
        }
        private int maxTradeSize;

        public int MaxTradeSize
        {
            get { return maxTradeSize; }
            set { maxTradeSize = value; }
        }
        private int maxSubscriptionVolume;

        public int MaxSubscriptionVolume
        {
            get { return maxSubscriptionVolume; }
            set { maxSubscriptionVolume = value; }
        }


        private int maxPreMarketTradeSize;

        public int MaxPreMarketTradeSize
        {
            get { return maxPreMarketTradeSize; }
            set { maxPreMarketTradeSize = value; }
        }
        
        private double autoCallableVegaShiftScaling;

        public double AutoCallableVegaShiftScaling
        {
            get { return autoCallableVegaShiftScaling; }
            set { autoCallableVegaShiftScaling = value; }
        }

        private double autoCallableEpsilonShiftScaling;

        public double AutoCallableEpsilonShiftScaling
        {
            get { return autoCallableEpsilonShiftScaling; }
            set { autoCallableEpsilonShiftScaling = value; }
        }

    }
}
