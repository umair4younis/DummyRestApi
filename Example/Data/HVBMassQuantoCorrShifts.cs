using Puma.MDE.Common;
using System;
using System.Globalization;

namespace Puma.MDE.Data
{
    public class HVBMassQuantoCorrShifts : Entity
    {
        public String   PaymentCurrency      { get; set; }   // e.g. EUR
        public String   UnderlyingCurrency   { get; set; }
        public Decimal  AbsoluteShiftSize    { get; set; }
        public int      PaymentCurrencyID    { get; set; }   // e.g. 54875474
        public int      UnderlyingCurrencyID { get; set; }
        public bool     IsRowUpdated         { get; set; }
        public bool     IsRowDeleted         { get; set; }
        public bool     IsRowNew             { get; set; }
        
        public HVBMassQuantoCorrShifts()
        {
            PaymentCurrency      = "";
            UnderlyingCurrency   = "";
            AbsoluteShiftSize    = 0;
            PaymentCurrencyID    = 0;
            UnderlyingCurrencyID = 0;
            IsRowUpdated         = false;
            IsRowDeleted         = false;
            IsRowNew             = false;
        }

        public void Reset()
        {
            PaymentCurrency      = "";
            UnderlyingCurrency   = "";
            AbsoluteShiftSize    = 0;
            PaymentCurrencyID    = 0;
            UnderlyingCurrencyID = 0;
            IsRowUpdated         = false;
            IsRowDeleted         = false;
            IsRowNew             = false;
        }

        public bool Equals(HVBMassQuantoCorrShifts hvbMassQuantoCorrShifts)
        {
            if (ReferenceEquals(null, hvbMassQuantoCorrShifts)) return false;
            if (ReferenceEquals(this, hvbMassQuantoCorrShifts)) return true;
            return hvbMassQuantoCorrShifts.PaymentCurrency    == PaymentCurrency &&
                   hvbMassQuantoCorrShifts.UnderlyingCurrency == UnderlyingCurrency;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassQuantoCorrShifts)) return false;
            return Equals((HVBMassQuantoCorrShifts)obj);
        }

        public override int GetHashCode()
        {
            unchecked { return Convert.ToInt32(PaymentCurrency.GetHashCode()) ^ Convert.ToInt32(UnderlyingCurrency.GetHashCode()); }
        }

        public override string ToString()
        {
            return String.Format("HVBMassQuantoCorrShifts<" +
                     "PaymentCurrency: {0}, " +
                     "PaymentCurrencyID: {1}, " +
                     "UnderlyingCurrency: {2}, " +
                     "UnderlyingCurrencyID: {3}, " +
                     "AbsoluteShiftSize: {4}, " +
                     "IsRowUpdated: {5}, " +
                     "IsRowDeleted: {6}, " +
                     "IsRowNew: {7}>",
                     PaymentCurrency,
                     PaymentCurrencyID,
                     UnderlyingCurrency,
                     UnderlyingCurrencyID,
                     AbsoluteShiftSize.ToString("F", CultureInfo.InvariantCulture),
                     IsRowUpdated,
                     IsRowDeleted,
                     IsRowNew);
        }
    }
}