using System;

namespace Puma.MDE.Data
{
    public class HVBMassFundingBucket //: System.ComponentModel.INotifyPropertyChanged
    {
        /*public bool Equals(HVBMassFundingBucket hvbMassFundingBucket)
        {
            if (ReferenceEquals(null, hvbMassFundingBucket)) return false;
            if (ReferenceEquals(this, hvbMassFundingBucket)) return true;
            return hvbMassFundingBucket.Maturity == Maturity && hvbMassFundingBucket.FundingSpread == FundingSpread && hvbMassFundingBucket.Currency == Currency;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassFundingBucket)) return false;
            return Equals((HVBMassFundingBucket)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return  Convert.ToInt32(FundingSpread) ^ Convert.ToInt32(Currency);
            }
        }*/

        private int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        private string maturity;

        public string Maturity
        {
            get { return maturity; }
            set { maturity = value; }//NotifyPropertyChanged(() => Maturity); }
        }
        private string fundingSpread;

        public string FundingSpread
        {
            get { return fundingSpread; }
            set { fundingSpread = value; }//NotifyPropertyChanged(() => FundingSpread); }
        }
        private int currency;

        public int Currency
        {
            get { return currency; }
            set { currency = value; }//NotifyPropertyChanged(() => Currency); }
        }
        private bool isDirty;
        
        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }//NotifyPropertyChanged(() => IsDirty); }
        }
    }

    public class PuMaMIPFunding //: System.ComponentModel.INotifyPropertyChanged
    {
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string issuer_LEI;
        public string IssuerLEI
        {
            get { return issuer_LEI; }
            set { issuer_LEI = value; }
        }

        private string currency;
        public string Currency
        {
            get { return currency; }
            set { currency = value; }
        }

        private double term;
        public double Term
        {
            get { return term; }
            set { term = value; }
        }

        private string maturity;
        public string MIPMaturity
        {
            get { return maturity; }
            set { maturity = value; }
        }

        private double rate;
        public double Rate
        {
            get { return rate; }
            set { rate = value; }
        }

        private string fundingSpread;
        public string MIPFundingSpread
        {
            get { return fundingSpread; }
            set { fundingSpread = value; }
        }

        private string type;
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        private DateTime last_update;
        public DateTime LastUpdate
        {
            get { return last_update; }
            set { last_update = value; }
        }

        private bool isDirty;
        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }
    }

    public class MIPIssuer
    {
        public string issuerLEI { get; set; }
        public string issuerName { get; set; }

        public override string ToString()
        {
            return issuerName;
        }
    }
}
