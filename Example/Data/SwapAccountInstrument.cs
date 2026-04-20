using Puma.MDE.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountInstrument : Entity
    {
        public SwapAccountInstrument() { }
        public SwapAccountInstrument(int id) { Id = id; }
        public String InstrumentName { get; set; }
        public String Description { get; set; }
        public String DbName { get; set; }
        public double ContractSize { get; set; }
        public int InstrumentTypeId { get; set; }
        public SwapAccountInstrumentType InstrumentType
        {
            get => null;
            set
            {
                this.InstrumentTypeId = value.Id;
                NotifyPropertyChanged(() => InstrumentTypeId);
                NotifyPropertyChanged(() => InstrumentType);
            }
        }
        public TypeManstInstrument InstrumentTypeEnum
        {
            get => TypeManstInstrument.Cash;
            set
            {
                InstrumentType = null;
                NotifyPropertyChanged(nameof(InstrumentType));
                NotifyPropertyChanged(nameof(InstrumentTypeEnum));
            }
        }
        public String xRIC { get; set; }
        public String RIC
        {
            get
            {
                if (IsRICNull()) return string.Empty;
                else return xRIC;
            }
            set
            {
                xRIC = value;
                NotifyPropertyChanged(() => xRIC);
                NotifyPropertyChanged(() => RIC);
            }
        }
        public String ISIN { get; set; }
        public String xISIN
        {
            get
            {
                return ISIN == null ? String.Empty : ISIN;
            }
        }
        public void SetISINNull() { ISIN = null; }


        public String BBG { get; set; }
        public bool IsISINNull()
        {
            return ISIN == null;
        }
        public bool IsRICNull()
        {
            return xRIC == null;
        }


        public String mSICOVAM { get; set; }
        public String SICOVAM
        {
            get
            {
                return mSICOVAM == null ? String.Empty : mSICOVAM;
            }
            set
            {

                mSICOVAM = value;
                NotifyPropertyChanged(() => mSICOVAM);
                NotifyPropertyChanged(() => SICOVAM);
            }
        }

        public bool IsSICOVAMNull()
        {
            return mSICOVAM == null;
        }
        public String mWKN { get; set; }
        public String WKN
        {
            get
            {
                return mWKN == null ? string.Empty : mWKN;
            }
            set
            {
                mWKN = value;
                NotifyPropertyChanged(() => mWKN);
                NotifyPropertyChanged(() => WKN);
            }
        }
        public String PriceProvider { get; set; }
        public DateTime? BondMaturity { get; set; }
        public bool IsBondMaturityNull()
        {
            return !BondMaturity.HasValue;
        }
        public Object BondMaturityOrNull
        {
            get
            {
                return (IsBondMaturityNull()) ? null : (object)BondMaturity.Value;
            }
            set
            {
                if (value != null)
                    BondMaturity = (DateTime)value;
                else
                    SetBondMaturityNull();
            }
        }
        public void SetBondMaturityNull()
        {
            BondMaturity = null;
        }
        public bool IsRealCash { get { return this.InstrumentType.IsCash && !this.InstrumentName.Contains("Temporal Cash"); } }
        public bool IsTemporalCash { get { return this.InstrumentType.IsCash && this.InstrumentName.Contains("Temporal Cash"); } }
        public int xSICOVAM
        {
            get
            {
                int sico = 0;
                int.TryParse(this.SICOVAM, out sico);
                return sico;
            }
        }

        public int getSicovamForSPIUpload()
        {
            int sico = 0;
            if (!string.IsNullOrEmpty(this.SophisReferenceForSPIUpload))
                sico = 1;
            if (sico == 0)
                int.TryParse(this.SICOVAM, out sico); // if no "sophis reference for spi upload" is specified, take the sicovam of the instrument itself
            return sico;
        }

        public String BondStructure { get; set; }

        public bool IsPriceProviderNull() { return false; }

        public int? CountryId { get; set; }
        public SwapCountry Country
        {
            get => null;
            set
            {
                if (value == null)
                {
                    this.CountryId = null;
                }
                else
                {
                    this.CountryId = 1;
                }
                NotifyPropertyChanged(() => this.CountryId);
                NotifyPropertyChanged(() => this.Country);
            }
        }
        public bool IsCountryIDNull()
        {
            return Country == null;
        }
        public void SetCountryIDNull()
        {
            Country = null;
        }

        public IEnumerable<SwapTrade> GetTradeRows() => null;

        public bool IsBondStructureNull()
        {
            return BondStructure == null;
        }
        public void SetBondStructureNull()
        {
            BondStructure = null;
        }

        public String xBondStructure
        {
            get
            {
                return IsBondStructureNull() ? String.Empty : BondStructure;
            }
        }

        public DateTime? ValueDate { get; set; }
        public bool IsValueDateNull() { return !ValueDate.HasValue; }
        public void SetValueDateNull() { ValueDate = null; }

        public double? BondCoupon { get; set; }
        public double xBondCoupon
        {
            get
            {
                return IsBondCouponNull() ? 0 : BondCoupon.Value;
            }
            set
            {
                BondCoupon = value;
                NotifyPropertyChanged(() => BondCoupon);
                NotifyPropertyChanged(() => xBondCoupon);
            }
        }
        public bool IsBondCouponNull() { return !BondCoupon.HasValue; }
        public void SetBondCouponNull() { BondCoupon = null; }

        public int? BondFloatingRate { get; set; }
        public bool IsBondFloatingRateNull()
        {
            return !BondFloatingRate.HasValue;
        }
        public void SetBondFloatingRateNull() { BondFloatingRate = null; }

        // This field is used by SwapInstrumentPropertiesUI
        public string BondFloatingRateByName
        {
            get
            {
                if (IsBondFloatingRateNull()) return string.Empty;
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (true)
                {
                    BondFloatingRate = 1;
                    NotifyPropertyChanged(() => BondFloatingRate);
                    NotifyPropertyChanged(() => BondFloatingRateByName);
                }
            }
        }

        public string FieldClose { get; set; }
        public string FieldLast { get; set; }
        public string FieldBid { get; set; }
        public string FieldAsk { get; set; }
        public string SophisReference { get; set; }
        public string SophisReferenceForSPIUpload { get; set; }
        public string m_MarketWay { get; set; }
        public string MarketWay
        {
            get
            {
                //"XXX/YYY" is default value
                return m_MarketWay == null ? "XXX/YYY" : m_MarketWay;
            }
            set
            {
                m_MarketWay = value;
                NotifyPropertyChanged(() => m_MarketWay);
                NotifyPropertyChanged(() => MarketWay);
            }
        }

        /// <summary>
        /// Gets the issue date of the Bond, if its found in the bond structure, or the DateTime.MinValue.
        /// </summary>
        /// <value>The issue date of the bond or the DateTime.MinValue.</value>
        public DateTime xIssueDateOrMinValue
        {
            get
            {
                // bond structure is in english culture format
                IFormatProvider EnglishCulture = new CultureInfo("en-US", false);

                // try to get the issue date from the structure
                if (InstrumentType.IsBond && xBondStructure.Contains("ISSUE:"))
                {
                    string dateStr = xBondStructure.Substring(xBondStructure.IndexOf("ISSUE:") + 6, 9);
                    DateTime tmpDateTime = DateTime.MinValue;
                    if (DateTime.TryParseExact(dateStr, "ddMMMyyyy", EnglishCulture, System.Globalization.DateTimeStyles.None, out tmpDateTime))
                        return tmpDateTime.Date;
                }
                return DateTime.MinValue;
            }
        }

        private int m_DaysToNextDividend = -1;
        private DateTime? m_NextDividend = null;

        private bool m_IsDirtyDividend = true;
        public bool IsDirtyDividend
        {
            get { return m_IsDirtyDividend; }
            set { m_IsDirtyDividend = value; }
        }
        public DateTime? xNextDividend
        {
            get
            {
                if (IsDirtyDividend == true)
                    refreshDirtyDividend();
                return m_NextDividend;
            }
        }

        public int xDaysToNextDividend
        {
            get
            {
                if (IsDirtyDividend == true)
                    refreshDirtyDividend();
                return m_DaysToNextDividend;
            }
        }

        private void refreshDirtyDividend()
        {
            IsDirtyDividend = false;

            TypeManstInstrument instrumentType = TypeManstInstrument.Cash;

            if ((instrumentType == TypeManstInstrument.Fund) || (instrumentType == TypeManstInstrument.Index)) // currently, only Fund and Index have divs maintained via the instrument dialog
            {
                List<SwapAccountDividend> dividends = null;
                loadDividends(out dividends);

                // next (upcoming)
                m_NextDividend = null;
                int i = 0;
                while ((i < dividends.Count) && (dividends[i].ExDate < DateTime.Now.Date))
                    i++;
                if (i < dividends.Count)
                    m_NextDividend = dividends[i].ExDate;

                // days to next div
                m_DaysToNextDividend = -1;
                if (m_NextDividend.HasValue && m_NextDividend.Value != null)
                    m_DaysToNextDividend = Convert.ToInt32((m_NextDividend.Value.Date - DateTime.Now.Date).TotalDays);
            }
        }

        public IList<SwapDividendRow> DividendRows { get; set; } = new List<SwapDividendRow>();
        public IList<SwapDividendRow> GetDividendRows()
        {
            return DividendRows;
        }
        public ObservableCollection<SwapDividendRow> m_DividendRowsObservable { get; } = new ObservableCollection<SwapDividendRow>();
        public ObservableCollection<SwapDividendRow> DividendRowsObservable
        {
            get
            {
                m_DividendRowsObservable.Clear();
                foreach (var row in DividendRows)
                {
                    m_DividendRowsObservable.Add(row);
                }
                return m_DividendRowsObservable;
            }

        }

        public void ClearDividend()
        {
            DividendRows.Clear();
            SetToDirty();
        }

        public IList<SwapFloaterHistoryRow> FloaterHistoryRows { get; set; } = new List<SwapFloaterHistoryRow>();
        public IList<SwapFloaterHistoryRow> GetFloaterHistoryRows()
        {
            return FloaterHistoryRows;
        }

        public ObservableCollection<SwapFloaterHistoryRow> m_FloaterHistoryRowsObservable { get; } = new ObservableCollection<SwapFloaterHistoryRow>();
        public ObservableCollection<SwapFloaterHistoryRow> FloaterHistoryRowsObservable
        {
            get
            {
                m_FloaterHistoryRowsObservable.Clear();
                foreach (var row in FloaterHistoryRows)
                {
                    m_FloaterHistoryRowsObservable.Add(row);
                }
                return m_FloaterHistoryRowsObservable;
            }

        }

        public void ClearHistoricFloatingRates()
        {
            DateTime sinceAlways = DateTime.MinValue;
            ClearHistoricFloatingRates(sinceAlways);
            SetToDirty();
        }
        /// <summary>
        /// Clears the historic floating rates.
        /// </summary>
        public void ClearHistoricFloatingRates(DateTime beyond)
        {
            List<SwapFloaterHistoryRow> filtered = new List<SwapFloaterHistoryRow>();
            foreach (SwapFloaterHistoryRow fh in GetFloaterHistoryRows())
            {
                if (fh.StartDate.Date < beyond.Date)
                {
                    filtered.Add(fh);
                    this.SetToDirty();
                }
            }
            FloaterHistoryRows = filtered;
        }

        public string Currency { get; set; }
        public bool IsCurrencyNull()
        {
            return Currency == null;
        }
        public string xCurrency
        {
            get
            {
                if (IsCurrencyNull()) return String.Empty; else return Currency;
            }
        }

        public string OptionType { get; set; }
        public bool IsOptionTypeNull()
        {
            return OptionType == null;
        }
        public string xOptionType
        {
            get
            {
                return IsOptionTypeNull() ? String.Empty : OptionType;
            }
            set
            {
                OptionType = value;
                NotifyPropertyChanged(() => OptionType);
                NotifyPropertyChanged(() => xOptionType);
            }
        }

        public double? Strike1 { get; set; }
        public bool IsStrike1Null() { return !Strike1.HasValue; }
        public double? Strike2 { get; set; }
        public bool IsStrike2Null() { return !Strike2.HasValue; }


        public void loadDividends(out List<SwapAccountDividend> dividends)
        {
            dividends = new List<SwapAccountDividend>();
            if (GetDividendRows().Count > 0)
            {
                foreach (SwapDividendRow r in GetDividendRows())
                    dividends.Add(new SwapAccountDividend(ISIN, r.ExDate, r.PayDate, r.Div, this.xCurrency, null, null));
                dividends.Sort(delegate (SwapAccountDividend d1, SwapAccountDividend d2) { return Comparer<DateTime>.Default.Compare(d1.ExDate, d2.ExDate); }); // sort on Date
            }
        }


    }

}
