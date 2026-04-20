using Newtonsoft.Json;
using Puma.MDE.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountReport : Entity
    {
        public SwapAccountReport() { }
        public SwapAccountReport(string dbName, int accountId, int numbOfRecip, string name, string displayName, bool includeInExcel, bool includeInMail, string value, int decimals)
        {
            this.DbName = dbName;
            this.AccountId = accountId;
            this.Recipient = numbOfRecip;
            this.xName = name;
            this.DisplayName = displayName;
            this.IncludeInExcel = includeInExcel;
            this.IncludeInMail = includeInMail;
            this.Value = value;
            this.Decimals = decimals;
        }
        public string DbName { get; set; }
        public int AccountId { get; set; }
        public int Recipient { get; set; }
        public string xName { get; set; }
        public string DisplayName { get; set; }
        public bool IncludeInExcel { get; set; }
        public bool IncludeInMail { get; set; }
        public string Value { get; set; }
        public bool IsValueNull() { return this.Value == null; }
        public int Decimals { get; set; }

        public int Priority { get; set; } // this field usage is purely internal when changing the order of rows in the account

        [JsonIgnore]
        public SwapAccount Account
        {
            get
            {
                return null;
            }
        }

        public bool getNameWithValue(out string str, out string error)
        {
            str = string.Empty;
            error = string.Empty;

            string s = string.Empty;
            if (!getValueAsString(out s, out error))
                return false;

            switch (xName)
            {
                case "Next upcoming Holidays":
                    {
                        str = string.Format("{0}{1}: {2}{0}", Environment.NewLine, DisplayName, s);
                        break;
                    }
                case "Description":
                case "Market Weight by Instrument Type":
                    {
                        str = string.Format("{0}{1}:{0}{2}{0}", Environment.NewLine, DisplayName, s);
                        break;
                    }
                case "Put Price in Amount":
                    {
                        str = string.Format("{0}", s);
                        break;
                    }
                default:
                    {
                        str = string.Format("{0}: {1}", DisplayName, s);
                        break;
                    }
            }

            return true;
        }

        public bool getValueAsString(out string str, out string error)
        {
            str = string.Empty;
            error = string.Empty;

            if ((!IsValueNull()) && (!string.IsNullOrEmpty(Value))) // has a manual value been set?
            {
                switch (string.Empty)
                {
                    case "double":
                        {
                            double d = 0.0;
                            if (!Double.TryParse(Value, out d))
                            {
                                error = string.Format("could not convert '{0}' for '{1}' into double", Value, DisplayName);
                                return false;
                            }
                            str = formatDouble(d);
                            break;
                        }
                    case "pct":
                        {
                            double d = 0.0;
                            if (!Double.TryParse(Value, out d))
                            {
                                error = string.Format("could not convert '{0}' for '{1}' into double", Value, DisplayName);
                                return false;
                            }
                            d = d * 0.01;
                            str = formatPct(d);
                            break;
                        }
                    default:
                        {
                            str = Value;
                            break;
                        }
                }

                return true;
            }

            switch (xName)
            {
                case "Actual Date":
                    {
                        str += string.Format("{0}", DateTime.Now.Date.ToString("dd/MM/yyyy"));
                        break;
                    }
                case "Maturity":
                    {
                        if (!Account.IsCertificateMaturityNull())
                            str += string.Format("{0}", Account.CertificateMaturity.Value.Date.ToString("dd/MM/yyyy"));
                        break;
                    }
                case "Valuation Date":
                    {
                        DateTime pricingDate = Account.getPricingDate(DateTime.Now.Date);
                        str += string.Format("{0}", pricingDate.ToString("dd/MM/yyyy"));
                        break;
                    }
                case "Number of Certificates":
                    {
                        str += formatDouble(Account.xCertificates);
                        break;
                    }
                case "High Watermark":
                    {
                        str += formatDouble(Account.HighWaterMark);
                        break;
                    }
                case "Index":
                    {
                        str += formatDouble(Account.xIndexValue);
                        break;
                    }
                case "Index without Accrued":
                    {
                        double value = Account.getIndexWithoutAccrued();
                        str += formatDouble(value);
                        break;
                    }
                case "Performance Swap Price":
                    {
                        double value = Account.getPerformanceSwapPrice();
                        str += formatDouble(value);
                        break;
                    }
                case "Put Price in Pct":
                    {
                        double value = Account.getPutPrice();
                        str += formatPct(value);
                        break;
                    }
                case "Put Price in Amount":
                    {
                        bool first = true;
                        double putPrice = Account.getPutPrice();
                        foreach (SwapTippShareRow share in Account.GetTippShareRows())
                        {
                            first = false;
                            if (true)
                            {
                                str += string.Format("Put Price in Amount ({0}): {1}", share.SophisReference, "N/A");
                            }
                            else
                            {
                                double value = putPrice;
                                str += string.Format("Put Price in Amount ({0}): {1}", share.SophisReference, formatDouble(value));
                            }
                        }
                        break;
                    }
                case "Snapshot Index":
                    {
                        str += formatDouble(Account.IndexSnapShotValue);
                        break;
                    }
                case "Reported Index":
                    {
                        str += formatDouble(Account.LastReportedIndex);
                        break;
                    }
                case "Market Value":
                    {
                        double value = Account.xIndexValue / Account.IndexFactorDouble;
                        str += formatDouble(value);
                        break;
                    }
                case "Swap Notional":
                    {
                        str += formatDouble(Account.SwapNotional);
                        break;
                    }
                case "Swap Strike":
                    {
                        str += formatDouble(Account.SwapStrike);
                        break;
                    }
                case "Swap Units":
                    {
                        str += formatDouble(Account.xSwapUnits);
                        break;
                    }
                case "Fee Calc Offset":
                    {
                        str += formatDouble(Account.xFeeCalcOffset);
                        break;
                    }
                case "Swap Value":
                    {
                        str += formatPct(Account.xSwapValue);
                        break;
                    }
                case "Swap MTM":
                    {
                        double marketValue = Account.xMarketValue;
                        double swapUnits = Account.xSwapUnits;
                        double accrued = 0.0;
                        string reason = "";
                        if (!Account.GetAccruedPremiumAmount(out accrued, out reason))
                        {
                            error = string.Format("could not calculate '{0}' for reason '{1}'", xName, reason);
                            return false;
                        }
                        double value = 0.0;
                        if (swapUnits != 0.0)
                            value = (marketValue + accrued) / swapUnits;
                        str += formatPct(value);
                        break;
                    }
                case "Swap MTM amount":
                    {
                        double marketValue = Account.xMarketValue;
                        double accrued = 0.0;
                        string reason = "";
                        if (!Account.GetAccruedPremiumAmount(out accrued, out reason))
                        {
                            error = string.Format("could not calculate '{0}' for reason '{1}'", xName, reason);
                            return false;
                        }
                        double value = marketValue + accrued;
                        str += formatDouble(value);
                        break;
                    }
                case "MtM":
                    {
                        str += formatDouble(Account.xMtM);
                        break;
                    }
                case "Sum Fee1":
                    {
                        double value = Account.getSumFee(TypeManastEvent.ManagementFee1, false);
                        str += formatDouble(value);
                        break;
                    }
                case "Sum Fee2":
                    {
                        double value = Account.getSumFee(TypeManastEvent.ManagementFee2, false);
                        str += formatDouble(value);
                        break;
                    }
                case "Sum Perf Fee":
                    {
                        double value = Account.getSumFee(TypeManastEvent.PerformanceFee, false);
                        str += formatDouble(value);
                        break;
                    }
                case "Daily Premium":
                    {
                        DateTime today = DateTime.Now.Date;
                        SortedDictionary<DateTime, PremiumValue> values;
                        Account.CalculatePremiumValues(today, out values);
                        double value = double.NaN;
                        DateTime pricingDate = Account.getPricingDate(today);
                        string reason = "";
                        if (values.ContainsKey(pricingDate))
                        {
                            PremiumValue v = values[pricingDate];
                            if (v.Error == PremiumValue.ErrorCode.eNoError)
                                value = v.xTotalFee;
                            else
                                reason = v.xErrrorString;
                        }
                        else
                        {
                            reason = "no entry in the table on that date";
                        }
                        if (double.IsNaN(value))
                        {

                            error = string.Format("could not calculate '{0}' on {1} for reason '{2}'", xName, pricingDate.ToString("dd/MM/yyyy"), reason);
                            return false;
                        }
                        str += formatDouble(value);
                        break;
                    }
                case "Accrued Premium Amount":
                    {
                        double value = 0.0;
                        string reason = "";
                        if (!Account.GetAccruedPremiumAmount(out value, out reason))
                        {
                            error = string.Format("could not calculate '{0}' for reason '{1}'", xName, reason);
                            return false;
                        }
                        str += formatDouble(value);
                        break;
                    }
                case "Performance":
                    {
                        str += formatPct(Account.xPerformance);
                        break;
                    }
                case "MTM from Index Performance":
                    {
                        double marketValue = Account.xMarketValue;
                        double swapUnits = Account.xSwapUnits;
                        double value = 0.0;
                        if (swapUnits != 0.0)
                            value = marketValue / swapUnits;
                        str += formatPct(value);
                        break;
                    }
                case "MTM from Financing":
                    {
                        double value = 0.0;
                        string reason = "";
                        if (!Account.GetMTMfromFinancing(out value, out reason))
                        {
                            error = string.Format("could not calculate '{0}' for reason '{1}'", xName, reason);
                            return false;
                        }
                        str += formatPct(value);
                        break;
                    }
                case "Day Performance":
                    {
                        str += formatPct(Account.xDayPerformance);
                        break;
                    }
                case "Yesterday Performance":
                    {
                        str += formatPct(Account.xYesterdayPerformance);
                        break;
                    }
                case "Leverage":
                    {
                        str += formatPct(Account.xLeverageFactor);
                        break;
                    }
                case "Next upcoming Holidays":
                    { // print the next 5 holidays
                        SortedDictionary<DateTime, SortedDictionary<string, bool>> bankHolidays;
                        Account.GetBankHolidays(DateTime.Now.Date, out bankHolidays);
                        int i = 0;
                        while ((i < 5) && (bankHolidays.Count > i))
                        {
                            if (i > 0)
                                str += ", ";
                            str += bankHolidays.ElementAt(i).Key.ToString("dd/MM/yyyy");
                            i++;
                        }
                        break;
                    }
                case "Description":
                    {
                        str += string.Format("{0}", Account.Description);
                        break;
                    }
                case "Market Weight by Instrument Type":
                    {
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Cash", formatPct(Account.xCashWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Stocks", formatPct(Account.xStockWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Bonds", formatPct(Account.xBondWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Funds", formatPct(Account.xFundWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "ETFs", formatPct(Account.xETFWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Certificates", formatPct(Account.xCertificateWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Futures", formatPct(Account.xFutureWeight));
                        str += string.Format("{0}  {1,-12}: {2}", Environment.NewLine, "Futures Cash", formatPct(Account.xFutureCashWeight));
                        break;
                    }
                case "Customer":
                    {
                        str += "N/A";
                        break;
                    }
                case "Fund":
                    {
                        str += Account.AccountName;
                        break;
                    }
                case "Settlement Currency":
                    {
                        str += Account.Currency;
                        break;
                    }
                case "Next Rebalance":
                    {
                        DateTime? date = Account.xNextRebalance;
                        if (date.HasValue)
                            str += string.Format("{0}", date.Value.ToString("dd/MM/yyyy"));
                        break;
                    }
                default:
                    break;
            }

            return true;
        }

        public string formatDouble(double d)
        {
            return formatDouble(d, Decimals);
        }

        public string formatPct(double p)
        {
            string format = "{0:P" + Decimals.ToString() + "}"; // eg. "{0:P2}" when Decimals is 2
            return string.Format(format, p);
        }

        static public string formatDouble(double d, int decimals)
        {
            string format = "{0:n" + decimals.ToString() + "}"; // eg. "{0:n4}" when Decimals is 4
            return string.Format(format, d);
        }

        public int getNumberOfRowsInExcel()
        {
            switch (xName)
            {
                case "Put Price in Amount": // spans several rows in the Excel sheet, depending on how many "share classes" the user input in the account properties
                    int count = Account.GetTippShareRows().Count();
                    return count;
                default:
                    return 1;
            }
        }

        private void setCellFormat(object valueCell, string type, bool isGermanFormat)
        {
            return;
        }

        private void setNumber(object valueCell, double value, bool isGermanFormat)
        {
            return;
        }


        public bool setExcel(object nameCell, object valueCell, bool isGermanFormat, out string error)
        {
            error = string.Empty;

            setCellFormat(valueCell, string.Empty, isGermanFormat);

            if ((!IsValueNull()) && (!string.IsNullOrEmpty(Value))) // has a manual value been set?
            {
                switch (string.Empty)
                {
                    case "double":
                        {
                            double d = 0.0;
                            if (!Double.TryParse(Value, out d))
                            {
                                error = string.Format("could not convert '{0}' for '{1}' into double", Value, DisplayName);
                                return false;
                            }
                            setNumber(valueCell, d, isGermanFormat);
                            break;
                        }
                    case "pct":
                        {
                            double d = 0.0;
                            if (!Double.TryParse(Value, out d))
                            {
                                error = string.Format("could not convert '{0}' for '{1}' into double", Value, DisplayName);
                                return false;
                            }
                            d = d * 0.01;
                            setNumber(valueCell, d, isGermanFormat);
                            break;
                        }
                    default:
                        break;
                }

                return true;
            }

            switch (xName)
            {
                case "Actual Date":
                    {
                        break;
                    }
                case "Maturity":
                    {
                        break;
                    }
                case "Valuation Date":
                    {
                        break;
                    }
                case "Number of Certificates":
                    {
                        setNumber(valueCell, Account.xCertificates, isGermanFormat);
                        break;
                    }
                case "High Watermark":
                    {
                        setNumber(valueCell, Account.HighWaterMark, isGermanFormat);
                        break;
                    }
                case "Index":
                    {
                        setNumber(valueCell, Account.xIndexValue, isGermanFormat);
                        break;
                    }
                case "Index without Accrued":
                    {
                        double value = Account.getIndexWithoutAccrued();
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Performance Swap Price":
                    {
                        double value = Account.getPerformanceSwapPrice();
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Put Price in Pct":
                    {
                        double value = Account.getPutPrice();
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Snapshot Index":
                    {
                        setNumber(valueCell, Account.IndexSnapShotValue, isGermanFormat);
                        break;
                    }
                case "Reported Index":
                    {
                        setNumber(valueCell, Account.LastReportedIndex, isGermanFormat);
                        break;
                    }
                case "Market Value":
                    {
                        double value = Account.xIndexValue / Account.IndexFactorDouble;
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Swap Notional":
                    {
                        setNumber(valueCell, Account.SwapNotional, isGermanFormat);
                        break;
                    }
                case "Swap Strike":
                    {
                        setNumber(valueCell, Account.SwapStrike, isGermanFormat);
                        break;
                    }
                case "Swap Units":
                    {
                        setNumber(valueCell, Account.xSwapUnits, isGermanFormat);
                        break;
                    }
                case "Fee Calc Offset":
                    {
                        setNumber(valueCell, Account.xFeeCalcOffset, isGermanFormat);
                        break;
                    }
                case "Swap Value":
                    {
                        setNumber(valueCell, Account.xSwapValue, isGermanFormat);
                        break;
                    }
                case "MtM":
                    {
                        setNumber(valueCell, Account.xMtM, isGermanFormat);
                        break;
                    }
                case "Sum Fee1":
                    {
                        double value = Account.getSumFee(TypeManastEvent.ManagementFee1, false);
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Sum Fee2":
                    {
                        double value = Account.getSumFee(TypeManastEvent.ManagementFee2, false);
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Sum Perf Fee":
                    {
                        double value = Account.getSumFee(TypeManastEvent.PerformanceFee, false);
                        setNumber(valueCell, value, isGermanFormat);
                        break;
                    }
                case "Next Rebalance":
                    {
                        break;
                    }
                default:
                    {
                        string str = string.Empty;
                        if (!getValueAsString(out str, out error))
                            return false;
                        break;
                    }
            }

            return true;
        }

        public bool setExcel(object sheet, int nameColId, int valueColId, ref int rowId, bool isGermanFormat, out string error)
        {
            error = string.Empty;

            switch (xName)
            {
                case "Put Price in Amount":
                    {
                        double putPrice = Account.getPutPrice();
                        foreach (SwapTippShareRow share in Account.GetTippShareRows())
                        {
                            rowId = rowId + 1;
                        }
                        break;
                    }
                default:
                    {
                        rowId = rowId + 1;
                        break;
                    }
            }

            return true;
        }
    }
}
