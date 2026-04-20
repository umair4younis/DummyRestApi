using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Puma.MDE.Data
{
    public class XStepInFixing
    {
        public XStepInFixing(DateTime date, double fixing) { Date = date; Fixing = fixing; }
        public DateTime Date { get; set; }  // forward start date of StepIn option contained by Sophis package
        public double Fixing { get; set; }  // HISTORIQUE.hvb_close or hvb_nav for underlying of option

        static public SwapAccountPricing.Price loadFixingValue(SwapAccountPricing.Provider.IPriceProvider pp, int underlyingId, DateTime date)
        {
            SwapAccountPricing.SPriceRequest priceRequest = new SwapAccountPricing.SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eSpecificDate, date);
            SwapAccountPricing.Price p = null;
            return p;
        }

        static public bool calculateStepInRatio(SwapAccount ar, DateTime date, out double ratio, out string error)
        {
            List<XStepInFixing> fixings = null;
            ar.loadStepInFixings(out fixings);
            return calculateStepInRatio(fixings, date, out ratio, out error);
        }

        static public bool calculateStepInRatio(List<XStepInFixing> fixings, DateTime date, out double ratio, out string error)
        {
            double notional = 1000.0; // notional of certificate, instead of hardcoding should ideally do "SELECT nbtitres FROM titres" !

            ratio = 0.0;
            error = "";

            if (fixings.Count == 0)
                return true;

            double sum = 0.0;

            foreach (XStepInFixing f in fixings)
            {
                if (f.Date < date.Date)
                {
                    if (f.Fixing != 0.0)
                    {
                        sum = sum + (1.0 / f.Fixing);
                    }
                    else
                    {
                        error = string.Format("missing Step In fixing on {0}", f.Date.ToString("dd/MM/yyyy"));
                        break;
                    }
                }
            }

            double count = fixings.Count; // most often 12
            ratio = sum / count * notional;
            ratio = Math.Round(ratio, 6);

            return (error == "");
        }
    }

    public class XStepInDividend
    {
        public XStepInDividend(DateTime exDate, DateTime payDate, double factor, double div, double amount, DateTime booked, SwapUser user, string refcons) { ExDate = exDate; PayDate = payDate; Factor = factor; Div = div; Amount = amount; Booked = booked; User = user; Refcons = refcons; }
        public DateTime ExDate { get; set; }
        public DateTime PayDate { get; set; }
        public double Factor { get; set; }
        public double Div { get; set; }
        public double Amount { get; set; }
        public DateTime Booked { get; set; }
        public SwapUser User { get; set; }
        public string Refcons { get; set; }

        public void calculateAmount(double ratio)
        {
            Amount = Div * Factor * ratio;
            Amount = Math.Round(Amount, 2);
        }
    }

    public class XIndexValue
    {
        public XIndexValue(double value, DateTime date) { mValue = value; mDate = date; }
        public XIndexValue() : this(0.0, DateTime.MinValue) { }
        public double mValue;
        public DateTime mDate;
    }
    public class PriceTip
    {
        public TypeManastPriceTip mType { get; set; }
        public DateTime? mDate { get; set; }
        public int mSeverity { get; set; }
        public string mName { get; set; }
    }

    /// <summary>
    /// This class contains a dividend information
    /// </summary>
	public class SwapAccountDividend : IEquatable<SwapAccountDividend>
    {
        public string ISIN;
        public DateTime ExDate;
        public DateTime PayDate;
        public double Amount;
        public string Currency;
        public string Type;
        public string DataSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dividend"/> class.
        /// </summary>
        /// <param name="isin">The isin.</param>
        /// <param name="exDate">The ex date.</param>
        /// <param name="payDate">The pay date.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="currency">The currency.</param>
        /// <param name="dataSource">The data source.</param>
		public SwapAccountDividend(string isin, DateTime exDate, DateTime payDate, double amount, string currency, string type, string dataSource)
        {
            ISIN = isin;
            ExDate = exDate;
            PayDate = payDate;
            Amount = amount;
            DataSource = dataSource;
            Currency = currency;
            Type = type;
        }

        /*
          ISIN + ex date + amount do NOT uniquely identify a dividend, because below is an example whereby divs differ only by their type
         
            ISIN        ;FinYear   ;Type;Xdd       ;PayDate   ;Amount;Currency
            ES0148396007;2019/01/31;FIN ;2019/10/31;2019/11/04;0,22;;;EUR;true
            ES0148396007;2019/01/31;SPEC;2019/10/31;2019/11/04;0,22;;;EUR;true

          So we need to include the type
        */

        public bool Equals(SwapAccountDividend other)
        {
            return ISIN == other.ISIN && ExDate == other.ExDate && Currency == other.Currency && Type == other.Type && Amount == other.Amount;
        }

        public override string ToString()
        {
            return string.Format("{{isin: {0}, ex: {1}, amount: {2}, ccy: {3}, type: {4}}}", ISIN, ExDate.ToString("dd/MM/yyyy"), Amount, Currency, Type);
        }
    }

    public class RawIndexHistory
    {
        public DateTime Date = DateTime.MinValue;
        public double IndexValue = 0.0;
        public double Certificates = 0.0;
        public double HighWaterMark = 0.0;
        public string Description = string.Empty;
        public double IndexFactor { get { return (Certificates != 0.0) ? (1.0 / Certificates) : (0.0); } }
        public double? CollateralRatio = null;
        public double? Ratio = null;
    }

    public class PremiumValue
    {
        public enum ErrorCode { eNoError, eMissingIndexValue, eMissingRate };

        public TypeAccountPremiumNotionalCalc NotionalCalc { get; set; }
        public TypeAccountPremiumDayCalc DayCalc { get; set; }
        public DateTime Date { get; set; }
        public DateTime Tomorrow { get; set; }
        public double IndexValue { get; set; }
        public DateTime IndexDate { get; set; }
        public double Certificates { get; set; }
        public double SwapNotional { get; set; }
        public double Rate { get; set; }
        public double Days { get; set; }
        public double Basis { get; set; }
        public double AdditionalFee { get; set; }
        public double xTotalFee
        {
            get
            {
                if (Error != ErrorCode.eNoError)
                    return 0.0;
                switch (NotionalCalc)
                {
                    case TypeAccountPremiumNotionalCalc.Standard:
                        return IndexValue * Certificates * Rate * Days / Basis + AdditionalFee;
                    case TypeAccountPremiumNotionalCalc.Certificates:
                        return Certificates * Rate * Days / Basis + AdditionalFee;
                    case TypeAccountPremiumNotionalCalc.SwapNotional:
                        return SwapNotional * Rate * Days / Basis + AdditionalFee;
                    default:
                        return 0.0;
                }
            }
        }
        public ErrorCode Error { get; set; }
        public string xErrrorString
        {
            get
            {
                switch (Error)
                {
                    case ErrorCode.eMissingIndexValue:
                        return string.Format("{0} not found in Index history on {1}", xErrrorColumnName, IndexDate.ToString("dd/MM/yyyy"));
                    case ErrorCode.eMissingRate:
                        return string.Format("{0} not found on {1}", xErrrorColumnName, Date.ToString("dd/MM/yyyy"));
                    default:
                        return string.Empty;
                }
            }
        }
        public string xErrrorColumnName
        {
            get
            {
                switch (Error)
                {
                    case ErrorCode.eMissingIndexValue:
                        return "Index Value";
                    case ErrorCode.eMissingRate:
                        return "Rate";
                    default:
                        return string.Empty;
                }
            }
        }
        public double xMTMfromFinancing
        {
            get
            {
                if (Error != ErrorCode.eNoError)
                    return 0.0;
                return Rate * Days / Basis;
            }
        }
    }

    public class MonthPerformance
    {
        public DateTime mMonth { get; set; } // eg. 20171201
        public DateTime mFirstDate { get; set; } // eg. 20171201
        public double mFirstIndex { get; set; } // 100.0
        public DateTime mLastDate { get; set; } // eg. 20171231
        public double mLastIndex { get; set; } // 105.0
        public double mPerformance { get; set; } // 5% mont-over-month performance
        public int Year() { return mMonth.Year; }
        public int Month() { return mMonth.Month; }
    }

    public class PerformanceGridRow : INotifyPropertyChanged
    {
        public int Year { get; set; }
        public double Jan { get; set; }
        public double Feb { get; set; }
        public double Mar { get; set; }
        public double Apr { get; set; }
        public double May { get; set; }
        public double Jun { get; set; }
        public double Jul { get; set; }
        public double Aug { get; set; }
        public double Sep { get; set; }
        public double Oct { get; set; }
        public double Nov { get; set; }
        public double Dec { get; set; }
        public double YearPerf { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class XStopLossData
    {
        public XStopLossData(string kind, double pct, DateTime date) { mKind = kind; mPct = pct; mDate = date; }
        public XStopLossData() : this("", 0.0, DateTime.MinValue) { }
        public string mKind; // eg. "AllTime", "IntraDay", "YearToDate", "QuarterToDate", "DateToDate", ..
        public double mPct; // eg. -5%
        public DateTime mDate; // used by the kind "DateToDate"
    }

    public class XStopLossPerformance
    {
        public XStopLossPerformance() { mIndexValue = new XIndexValue(); mPerformance = 0.0; }
        public XIndexValue mIndexValue;  // index value on startdate of period
        public double mPerformance;
    }

}
