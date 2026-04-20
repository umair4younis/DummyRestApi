namespace Puma.MDE.Data
{
    public class PlannedAction
    {
        public bool Equals(PlannedAction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Sicovam == Sicovam && other.CorporateActionId == CorporateActionId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(PlannedAction)) return false;
            return Equals((PlannedAction)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Sicovam * 397) ^ CorporateActionId;
            }
        }

        public int CorporateActionId
        {
            get;
            set;
        }
        public int Sicovam
        {
            get;
            set;
        }
        public int UnderlyingSicovam
        {
            get;
            set;
        }
        public decimal RFactor
        {
            get;
            set;
        }
        public bool BasketUpdate
        {
            get;
            set;
        }
        public int BasketSicovam
        {
            get;
            set;
        }
        public bool PublishCorporateAction
        {
            get;
            set;
        }
        public bool VolatilityUpdate
        {
            get;
            set;
        }
        public bool DividendUpdate
        {
            get;
            set;
        }
        public bool SpotUpdate
        {
            get;
            set;
        }
        public bool B2BReplacement
        {
            get;
            set;
        }
        public int Basket2BasketOldSophisId
        {
            get;
            set;
        }
        public int Basket2BasketNewSophisId
        {
            get;
            set;
        }
    }
}
