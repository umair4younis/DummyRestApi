using Puma.MDE.Common;
using System;
using System.Globalization;

namespace Puma.MDE.Data
{
    public class MDEPriceCollar : Entity
    {
        public MDEPriceCollar() { }
        ~MDEPriceCollar() { }
        
        public string   Payoff   { get; set; }
        public decimal  MinPrice { get; set; }
        public decimal  MaxPrice { get; set; }
        public new bool IsDirty  { get; set; }
        public bool     IsValid  { get; set; }
        
        public MDEPriceCollar Clone()
        {
            MDEPriceCollar retval = new MDEPriceCollar();

            retval.Payoff   = Payoff;
            retval.MinPrice = MinPrice;
            retval.MaxPrice = MaxPrice;
            retval.IsDirty  = IsDirty;
            retval.IsValid  = IsValid;
            return retval;
        }
        
        public bool Equals(MDEPriceCollar other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Payoff   == Payoff &&
                   other.MinPrice == MinPrice &&
                   other.MaxPrice == MaxPrice &&
                   other.IsDirty  == IsDirty &&
                   other.IsValid  == IsValid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(MDEPriceCollar)) return false;
            return Equals((MDEPriceCollar)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (MinPrice.GetHashCode() * 397) ^ MinPrice.GetHashCode();
            }
        }

        public override string ToString()
        {
            return String.Format("MDEPriceCollar<" +
                                 "Payoff: '{0}', "   +
                                 "MinPrice: {1}, " +
                                 "MaxPrice: {2}>",
                                 Payoff,
                                 MinPrice.ToString("F", CultureInfo.InvariantCulture),
                                 MaxPrice.ToString("F", CultureInfo.InvariantCulture));
        }
    }
}
