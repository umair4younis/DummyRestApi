using System;

namespace Puma.MDE.Data
{
    public class DependencyAnalyzerData
    {
        public bool Equals(DependencyAnalyzerData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.SophisInstrumentId == SophisInstrumentId && other.CorporateActionId == CorporateActionId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(DependencyAnalyzerData)) return false;
            return Equals((DependencyAnalyzerData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SophisInstrumentId * 397) ^ CorporateActionId;
            }
        }

        public bool IsDirty
        {
            get;
            set;
        }

        public int CorporateActionTypeId
        {
            get;
            set;
        }

        public int CorporateActionId
        {
            get;
            set;
        }
        public int SophisInstrumentId
        {
            get;
            set;
        }
        public String SophisReference
        {
            get;
            set;
        }
        public string Classification
        {
            get;
            set;
        }
        public int UnderlyingSicovam
        {
            get;
            set;
        }
        public string InstrumentName
        {
            get;
            set;
        }
        public int NumberOfPositions
        {
            get;
            set;
        }
        public string ChangeType
        {
            get;
            set;
        }
        public string ChangeOwner
        {
            get;
            set;
        }
        public bool ReplaceUnderlyingByBasket
        {
            get;
            set;
        }
        public int ReplacementBasketSophisId
        {
            get;
            set;
        }

        private string _replaceUnderlyingByBasketAsString = "";
        public string ReplaceUnderlyingByBasketAsString
        {
            set
            {
                _replaceUnderlyingByBasketAsString = value;
                if (_replaceUnderlyingByBasketAsString != null && _replaceUnderlyingByBasketAsString.ToUpper().Equals("YES"))
                    ReplaceUnderlyingByBasket = true;
                else
                    ReplaceUnderlyingByBasket = false;
            }
            get
            {
                return _replaceUnderlyingByBasketAsString;
            }
        }

        //public bool BasketReplacement
        //{
        //    get;
        //    set;
        //}

        private string _B2BReplacementAsString = "";
        public string B2BReplacementAsString
        {
            set
            {
                _B2BReplacementAsString = value;
                if (_B2BReplacementAsString != null && _B2BReplacementAsString.ToUpper().Equals("YES"))
                    B2BReplacement = true;
                else
                    B2BReplacement = false;
            }
            get
            {
                return _B2BReplacementAsString;
            }
        }

        public bool B2BReplacement
        {
            get;
            set;
        }
        public int Basket2BasketNewSophisId
        {
            get;
            set;
        }
        public int Basket2BasketOldSophisId
        {
            get;
            set;
        }

        private string _physicalToCash;
        public string PhysicalToCash
        {
            set
            {
                _physicalToCash = value;
                if (_physicalToCash != null && _physicalToCash.ToUpper().Equals("YES"))
                    ChangePhysicalToCashSettlement = true;
                else
                    ChangePhysicalToCashSettlement = false;
            }
            get
            {
                return _physicalToCash;
            }
        }

        public bool ChangePhysicalToCashSettlement
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
        public string BasketReplacementStatus
        {
            get;
            set;
        }
        public string LastChangeUser
        {
            get;
            set;
        }

        public double IndividualRFactor
        {
            get;
            set;
        }
    }
}
