using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Puma.MDE.Common;

namespace Puma.MDE.Data
{

    [ComVisible(true)]
    [Serializable]
    public class SwapAccountPortfolio : Entity, INotifyPropertyChanged
    {
        public SwapAccountPortfolio() { }
        public SwapAccountPortfolio(int id) { Id = id; }
        public bool IsBeingEdited { get; set; } = false;
        public String DbName { get; set; }
        public int AccountId { get; set; }
        [JsonIgnore]
        public SwapAccount Account
        {
            get
            {
                return null;
            }
        }
        public double m_nominal { get; set; }
        public double Nominal
        {
            get => m_nominal;
            set
            {
                m_nominal = value;
                NotifyPropertyChanged(() => m_nominal);
                NotifyPropertyChanged(() => Nominal);
            }
        }
        public double AveragePrice { get; set; }
        public String Currency { get; set; }
        public string xForeignExchange { get { return Currency; } }
        public int? ManualInstrumentLastPrice { get; set; }
        public double? ManualValueLastPrice { get; set; }
        public double? ManualValueLastFX { get; set; }
        public double RealizedPL { get; set; } = 0;
        public int? InstrumentId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrument Instrument
        {
            get
            {
                if (InstrumentId.HasValue)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    InstrumentId = null;
                }
                else
                {
                    InstrumentId = value.Id;
                }
                NotifyPropertyChanged(() => InstrumentId);
                NotifyPropertyChanged(() => Instrument);
            }
        }
        public string xInstrumentName { get { return Instrument.InstrumentName; } }
        public string xInstrumentType { get { return Instrument.InstrumentType.TypeName; } }
        public string xOptionType { get { return Instrument.xOptionType; } }
        public string xRIC { get { return Instrument.RIC; } }
        public string xISIN { get { return Instrument.xISIN; } }
        public string xBBG { get { return Instrument.BBG; } }
        public bool? UseCloseValue { get; set; }
        public bool IsUseCloseValueNull()
        {
            return !UseCloseValue.HasValue;
        }
        public double? CloseValue { get; set; }
        public bool IsCloseValueNull()
        {
            return !CloseValue.HasValue;
        }
        public void SetCloseValueNull() { CloseValue = null; }
        public DateTime? CloseValueSaved { get; set; }
        public void SetCloseValueSavedNull()
        {
            CloseValueSaved = null;
        }
        public int? SophisPortfolioIdentifier { get; set; }
        public int? EventCountBalance { get; set; }
        public bool IsEventCountBalanceNull()
        {
            return !EventCountBalance.HasValue;
        }
        public bool m_UseDefaultTax { get; set; }
        public bool UseDefaultTax
        {
            get => m_UseDefaultTax;
            set
            {
                this.m_UseDefaultTax = value;
                NotifyPropertyChanged(() => m_UseDefaultTax);
                NotifyPropertyChanged(() => UseDefaultTax);
            }
        }
        public int? SophisIndexIdentifier { get; set; }
        public int? PendingOrderId { get; set; }

        public String StockExchange { get; set; }
        public string xStockExchange { get { return IsStockExchangeNull() ? string.Empty : StockExchange; } }
        public bool IsStockExchangeNull() { return StockExchange == null; }
        public void SetStockExchangeNull() { StockExchange = null; }

        public int? pRateSourceID { get; set; }
        public bool IspRateSourceIDNull()
        {
            return !pRateSourceID.HasValue;
        }
        public void SetpRateSourceIDNull()
        {
            pRateSourceID = null;
        }

        public double? PRateSourceFactor { get; set; }
        public bool IsPRateSourceFactorNull()
        {
            return !PRateSourceFactor.HasValue;
        }
        public void SetPRateSourceFactorNull()
        {
            PRateSourceFactor = null;
        }
        public double? PRateSourceOffset { get; set; }
        public bool IsPRateSourceOffsetNull()
        {
            return !PRateSourceOffset.HasValue;
        }
        public void SetPRateSourceOffsetNull()
        {
            PRateSourceOffset = null;
        }
        public bool? pRateFloorAtZero { get; set; }
        public bool IspRateFloorAtZeroNull()
        {
            return !pRateFloorAtZero.HasValue;
        }
        public bool? nRateFloorAtZero { get; set; }
        public bool IsnRateFloorAtZeroNull()
        {
            return !nRateFloorAtZero.HasValue;
        }

        public int? nRateSourceID { get; set; }
        public bool IsnRateSourceIDNull()
        {
            return !nRateSourceID.HasValue;
        }
        public void SetnRateSourceIDNull()
        {
            nRateSourceID = null;
        }
        public double? NRateSourceFactor { get; set; }
        public bool IsNRateSourceFactorNull()
        {
            return !NRateSourceFactor.HasValue;
        }
        public void SetNRateSourceFactorNull()
        {
            NRateSourceFactor = null;
        }
        public double? NRateSourceOffset { get; set; }
        public bool IsNRateSourceOffsetNull()
        {
            return !NRateSourceOffset.HasValue;
        }
        public void SetNRateSourceOffsetNull()
        {
            NRateSourceOffset = null;
        }
        public double? NRateManual { get; set; }
        public double xNRateManual { get { if (IsNRateManualNull()) return 0; else return NRateManual.Value; } }
        public bool IsNRateManualNull() { return !NRateManual.HasValue; }


        public double? InvestedValue { get; set; }
        public double? Tax { get; set; }

        public double? Collateral { get; set; }
        public bool IsCollateralNull()
        {
            return !Collateral.HasValue;
        }
        public void SetCollateralNull()
        {
            Collateral = null;
        }


        public string xInstrumentMaturity { get { return Instrument.IsBondMaturityNull() ? string.Empty : Instrument.BondMaturity.Value.ToString("dd/MM/yyyy"); } }
        public string xValueDate { get { return Instrument.IsValueDateNull() ? string.Empty : Instrument.ValueDate.Value.ToString("dd/MM/yyyy"); } }
        public string xInstrumentDaysToMaturity { get { return Instrument.IsBondMaturityNull() ? string.Empty : Math.Max((Instrument.BondMaturity.Value - DateTime.Now).Days, 0).ToString(); } }

        public double xStrike1 { get { return (this.Instrument.InstrumentType.IsFXDerivative || this.Instrument.InstrumentType.IsEquityOption) ? this.Instrument.Strike1.Value : 0.0; } }
        public double xStrike2 { get { return (this.Instrument.InstrumentType.IsFXSpread) ? this.Instrument.Strike2.Value : 0.0; } }

        public string xInstrumentWKN { get { return Instrument.WKN; } }
        public string xInstrumentSicovam { get { return Instrument.SICOVAM; } }
        public int? InstrumentByPositiveRateLinkId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrument InstrumentByPositiveRateLink
        {
            get
            {
                if (InstrumentByPositiveRateLinkId.HasValue)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    InstrumentByPositiveRateLinkId = null;
                }
                else
                {
                    InstrumentByPositiveRateLinkId = value.Id;
                }
                NotifyPropertyChanged(() => InstrumentByPositiveRateLinkId);
                NotifyPropertyChanged(() => InstrumentByPositiveRateLink);
            }
        }

        public int? InstrumentByNegativeRateLinkId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrument InstrumentByNegativeRateLink
        {
            get
            {
                if (InstrumentByNegativeRateLinkId.HasValue)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    InstrumentByNegativeRateLinkId = null;
                }
                else
                {
                    InstrumentByNegativeRateLinkId = value.Id;
                }
                NotifyPropertyChanged(() => InstrumentByNegativeRateLinkId);
                NotifyPropertyChanged(() => InstrumentByNegativeRateLink);
            }
        }


        [JsonIgnore]
        public SwapAccountInstrument InstrumentByLastPriceInstrumentLink
        {
            get
            {
                if (ManualInstrumentLastPrice.HasValue)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    ManualInstrumentLastPrice = null;
                }
                else
                {
                    ManualInstrumentLastPrice = value.Id;
                }
                NotifyPropertyChanged(() => ManualInstrumentLastPrice);
                NotifyPropertyChanged(() => InstrumentByLastPriceInstrumentLink);
            }
        }

        public int? InstrumentByFXInstrumentLinkId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrument InstrumentByFXInstrumentLink
        {
            get
            {
                if (InstrumentByFXInstrumentLinkId.HasValue)
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    InstrumentByFXInstrumentLinkId = null;
                }
                else
                {
                    InstrumentByFXInstrumentLinkId = value.Id;
                }
                NotifyPropertyChanged(() => InstrumentByFXInstrumentLinkId);
                NotifyPropertyChanged(() => InstrumentByFXInstrumentLink);
            }
        }

        public bool IsFXInstrumentNull()
        {
            return InstrumentByFXInstrumentLink == null;
        }

        public void SetFXInstrumentNull()
        {
            InstrumentByFXInstrumentLink = null;
        }

        public Dictionary<string, List<PriceTip>> m_Tips = new Dictionary<string, List<PriceTip>>();


        public double? PRateManual { get; set; }
        public double xPRateManual { get { if (IsPRateManualNull()) return 0; else return PRateManual.Value; } }
        public bool IsPRateManualNull() { return !PRateManual.HasValue; }

        public int? TippAssetClassCategoryID { get; set; }
        public bool IsTippAssetClassCategoryIDNull()
        {
            return !TippAssetClassCategoryID.HasValue;
        }

        public SwapTippAssetClass TippAssetClass { get; set; }
        public bool IsTippAssetClassIDNull()
        {
            return TippAssetClass == null;
        }
        public int? TippAssetRiskProfileID { get; set; }
        public bool IsTippAssetRiskProfileIDNull()
        {
            return !TippAssetRiskProfileID.HasValue;
        }


        public string xTippAssetClass { get { return (IsTippAssetClassIDNull()) ? "" : TippAssetClass.TippAssetClassName; } }
        public string xTippAssetClassCategory { get { return (IsTippAssetClassCategoryIDNull()) ? "" : System.Enum.GetName(typeof(TypeTippAssetClassCategory), TippAssetClassCategoryID); } }
        public string xTippAssetRiskProfile { get { return (IsTippAssetRiskProfileIDNull()) ? "" : System.Enum.GetName(typeof(TypeTippAssetRiskProfile), TippAssetRiskProfileID); } }

        [JsonIgnore]
        public SwapAccountPortfolio PortfolioRowParent
        {
            get
            {
                return PortfolioRowParentByCashLink;
            }
            set
            {
                this.PortfolioRowParentByCashLink = value;
                NotifyPropertyChanged(() => PortfolioRowParent);
                NotifyPropertyChanged(() => PortfolioRowParentByCashLink);
            }
        }
        public int? CashTargetID { get; set; }

        public int? PortfolioRowParentByTemporalCashLinkId { get; set; }

        [JsonIgnore]
        public SwapAccountPortfolio PortfolioRowParentByTemporalCashLink
        {
            get
            {
                if (PortfolioRowParentByTemporalCashLinkId.HasValue)
                    return null;
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    PortfolioRowParentByTemporalCashLinkId = null;
                }
                else
                {
                    PortfolioRowParentByTemporalCashLinkId = value.Id;
                }
                NotifyPropertyChanged(() => PortfolioRowParentByTemporalCashLinkId);
                NotifyPropertyChanged(() => PortfolioRowParentByTemporalCashLink);
            }
        }

        public int? PortfolioRowParentByCashLinkId { get; set; }

        [JsonIgnore]
        public SwapAccountPortfolio PortfolioRowParentByCashLink
        {
            get
            {
                if (PortfolioRowParentByCashLinkId.HasValue)
                    return null;
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    PortfolioRowParentByCashLinkId = null;
                }
                else
                {
                    PortfolioRowParentByCashLinkId = value.Id;
                }
                NotifyPropertyChanged(() => PortfolioRowParentByCashLinkId);
                NotifyPropertyChanged(() => PortfolioRowParentByCashLink);
            }
        }
        public bool IscashTargetIDNull() { return !CashTargetID.HasValue; }
        public bool IstemporalCashTargetIDNull() { return PortfolioRowParentByTemporalCashLink == null; }

        public bool CheckCashTargetAviability(string text)
        {
            if (IscashTargetIDNull())
            {
                string errorString = string.Format("'{0}' has no cash target specified. {1}", xInstrumentName, text);
                Engine.Instance.Log.Error(errorString);
                return false;
            }
            if (IstemporalCashTargetIDNull())
            {
                string errorString = string.Format("'{0}' has no temporal cash target specified. {1}", xInstrumentName, text);
                Engine.Instance.Log.Error(errorString);
                return false;
            }
            return true;
        }

        private double m_closePrice;
        private double m_closePriceCcy;
        private double m_lastPrice = 0.0;
        private double m_lastPriceCcy;
        private double m_lastPricePred;
        private double m_bid_price;
        private double m_ask_price;
        private double m_priceOffset;
        private double m_priceOffsetClosed;
        private double m_sophisNominalDifference = double.NaN;
        private int m_sophisEventDifference = 0;
        private double m_sophisGrossDifference = double.NaN;
        private double m_LastFxRate;
        private double m_CloseFxRate;
        private double m_Delta = 0;
        private double m_Gamma = 0;

        public double xLastPrice { get => m_lastPrice; set { m_lastPrice = value; OnPropertyChanged(nameof(xLastPrice)); } }
        public double xLastPriceCcy
        {
            get => m_lastPriceCcy;
            set
            {
                if (m_lastPriceCcy != value)
                {
                    m_lastPriceCcy = value;
                    OnPropertyChanged(nameof(xLastPriceCcy));
                }
            }
        }
        public double xLastPricePred { get { return m_lastPricePred; } }
        public double xClosePrice { get { return m_closePrice; } }
        public double xClosePriceCcy { get { return m_closePriceCcy; } }
        public double xDirtyPrice { get { return m_lastPrice + m_priceOffset; } }
        public double xPriceOffset { get { return m_priceOffset; } }
        public double xOffsetValue { get { return m_priceOffset * Nominal * Instrument.ContractSize / 100.0; } }

        /// <summary>
        /// Gets the event count (ONLY coupons, dividends and stock splits).
        /// </summary>
        /// <returns></returns>
        public int GetEventCount(DateTime sinceDate)
        {
            int count = 0;
            foreach (SwapEvent er in EventRowsByPortfolioSource)
            {
                if (er.PortfolioRowByPortfolioTarget.Instrument.IsTemporalCash)
                    continue;
                if (er.ExecutionDate.Date < sinceDate.Date)
                    continue;
                if (er.EventType == (int)TypeManastEvent.Coupon ||
                        er.EventType == (int)TypeManastEvent.Dividend ||
                        er.EventType == (int)TypeManastEvent._871mDividend ||
                        er.EventType == (int)TypeManastEvent.SwapDividend ||
                        er.EventType == (int)TypeManastEvent.DividendReinvest ||
                        er.EventType == (int)TypeManastEvent._871mDividendReinvest ||
                        er.EventType == (int)TypeManastEvent.StockSplit ||
                        er.EventType == (int)TypeManastEvent.ProfitOrLoss)
                    count++;
            }
            return count;
        }

        public double xFxRate { get { return m_LastFxRate; } }
        public double xCloseFxRate { get { return m_CloseFxRate; } }
        public double xSwapMTM { get { return xMarketValue - xInvestedValue; } }

        public double xUnrealPL { get { return xMarketValue - xInvestedValue; } }
        public double xUnrealPLClose { get { return xMarketValueClosed - xInvestedValue; } }

        public double xMarketWeight { get { return m_marketWeight; } }
        public double xMarketWeightCollateral { get { return m_marketWeightCollateral; } }
        public double xInvestedWeight { get { return m_investedWeight; } }

        public double xSPICheck { get { return (m_lastPrice + m_priceOffset) * xSPI; } }
        public double xSophisNominalDifference { get { return m_sophisNominalDifference; } set { m_sophisNominalDifference = value; } }
        public double xSophisDeltaCheck { get { if (!double.IsNaN(m_sophisNominalDifference)) return xSophisNominalDifference * xLastPrice; else return 0.0; } }

        public int xSophisEventDifference { get { return m_sophisEventDifference; } set { m_sophisEventDifference = value; } }
        public double xSophisGrossDifference { get { return m_sophisGrossDifference; } set { m_sophisGrossDifference = value; } }

        public double xDeltaPct { get { return m_Delta * 100.0; } }
        public double xDelta { get { return m_Delta; } }
        public double xGammaPct { get { return m_Gamma * 100.0; } }


        public int xDaysToDiv
        {
            get
            {
                TypeManstInstrument instrumentType = TypeManstInstrument.Cash;

                switch (instrumentType)
                {
                    case TypeManstInstrument.Stock:
                    case TypeManstInstrument.ETF:
                        {
                            if ((!Instrument.IsISINNull()) && (true))
                            {
                                DateTime date = new DateTime();
                                double d = (date - DateTime.Now.Date).TotalDays;
                                int days = Convert.ToInt32(d);
                                return days;
                            }
                        }
                        break;
                    case TypeManstInstrument.Fund:
                    case TypeManstInstrument.Index:
                        {
                            return Instrument.xDaysToNextDividend;
                        }
                    default:
                        break;
                }
                return -1;
            }
        }
        public DateTime? xExDivDate
        {
            get
            {
                TypeManstInstrument instrumentType = TypeManstInstrument.Cash;

                switch (instrumentType)
                {
                    case TypeManstInstrument.Stock:
                    case TypeManstInstrument.ETF:
                        {
                            if ((!Instrument.IsISINNull()) && (true))
                            {
                                DateTime date = new DateTime();
                                return date;
                            }
                            break;
                        }
                    case TypeManstInstrument.Fund:
                    case TypeManstInstrument.Index:
                        {
                            return Instrument.xNextDividend;
                        }
                    default:
                        break;
                }
                return null;
            }
        }
        public string xInstrumentCountry { get { return Instrument.IsCountryIDNull() ? string.Empty : Instrument.Country.Code; } }

        public double xAskPrice { get { return m_ask_price; } }
        public double xBidPrice { get { return m_bid_price; } }

        public double xBidAskSpread { get { return (this.Instrument.InstrumentType.IsCash || this.Instrument.InstrumentType.IsBond || Nominal == 0) ? 0.0 : (m_ask_price - m_bid_price); } }
        public double xBidAskSpreadPercent { get { return (this.Instrument.InstrumentType.IsCash || this.Instrument.InstrumentType.IsBond || Nominal == 0) ? 0.0 : (m_ask_price - m_bid_price) / m_bid_price; } }
        public double xWeightedBidAskSpread { get { return (this.Instrument.InstrumentType.IsCash || this.Instrument.InstrumentType.IsBond || Nominal == 0) ? 0.0 : xBidAskSpreadPercent * xMarketWeight; } }


        private double m_SumMgmtFee1 = 0;
        private double m_SumMgmtFee2 = 0;
        private double m_SumMgmtFee3 = 0;
        private double m_SumPerfFee = 0;

        public double xSumMgmtFee1 { get { return m_SumMgmtFee1; } set { m_SumMgmtFee1 = value; } }
        public double xSumMgmtFee2 { get { return m_SumMgmtFee2; } set { m_SumMgmtFee2 = value; } }
        public double xSumMgmtFee3 { get { return m_SumMgmtFee3; } set { m_SumMgmtFee3 = value; } }
        public double xSumPerfFee { get { return m_SumPerfFee; } set { m_SumPerfFee = value; } }

        private double m_SumMgmtFeePerUnit1 = 0;
        private double m_SumMgmtFeePerUnit2 = 0;
        private double m_SumMgmtFeePerUnit3 = 0;
        private double m_SumPerfFeePerUnit = 0;

        public double xSumMgmtFeePerUnit1 { get { return m_SumMgmtFeePerUnit1; } set { m_SumMgmtFeePerUnit1 = value; } }
        public double xSumMgmtFeePerUnit2 { get { return m_SumMgmtFeePerUnit2; } set { m_SumMgmtFeePerUnit2 = value; } }
        public double xSumMgmtFeePerUnit3 { get { return m_SumMgmtFeePerUnit3; } set { m_SumMgmtFeePerUnit3 = value; } }
        public double xSumPerfFeePerUnit { get { return m_SumPerfFeePerUnit; } set { m_SumPerfFeePerUnit = value; } }

        public bool hasManualPositiveRate()
        {
            return ((!IspRateSourceIDNull()) && (InstrumentByPositiveRateLink.InstrumentName.ToLower().Contains("manual")));
        }
        public bool hasManualNegativeRate()
        {
            return ((!IsnRateSourceIDNull()) && (InstrumentByNegativeRateLink.InstrumentName.ToLower().Contains("manual")));
        }


        private double m_marketWeight;
        private double m_marketWeightCollateral;
        private double m_investedWeight;

        public double xCollateral
        {
            get
            {
                return (!IsCollateralNull()) ? Collateral.Value : 0.0;
            }
        }

        public double xMarketValueCollateral
        {
            get
            {
                return (m_lastPrice + m_priceOffset) * xCollateral;
            }
        }

        private double m_RebateRate = 0.0;
        private bool m_RebateRateLoaded = false;
        public double xRebateRate
        {
            get
            {
                if ((Instrument.InstrumentType.IsFund) && (!m_RebateRateLoaded))
                {
                    m_RebateRateLoaded = true;
                    m_RebateRate = 1 * 0.01;
                }
                return m_RebateRate;
            }
        }


        private bool m_USWHTLoaded = false;
        private TypeManastUSWHT mUSWHT = TypeManastUSWHT.eNotApplicable;

        public TypeManastUSWHT xUSWHT
        {
            get
            {
                if (!m_USWHTLoaded)
                {
                    m_USWHTLoaded = true;
                    if (Instrument.InstrumentType.IsStock)
                        mUSWHT = TypeManastUSWHT.eNotApplicable;
                    else
                        mUSWHT = TypeManastUSWHT.eNotApplicable;
                }
                return mUSWHT;
            }
        }

        public string xUSWHTToString
        {
            get
            {
                TypeManastUSWHT typ = xUSWHT;
                if (typ == TypeManastUSWHT.eNotSpecified)
                    return "Missing";
                else if (typ == TypeManastUSWHT.eYes)
                    return "Yes";
                else if (typ == TypeManastUSWHT.eNo)
                    return "No";
                return "";
            }
        }


        // note: the average price is always in the instrument currency
        // it is multiplied by the rate source factor because of (ask Erik...)
        public double xAveragePrice
        {
            get
            {
                if (this.Instrument.InstrumentType.IsCash)
                {
                    return AveragePrice;
                }
                else
                {
                    return
                        (AveragePrice > 0.0) ?
                        AveragePrice * (this.IsPRateSourceFactorNull() ? 1.0 : PRateSourceFactor.Value)
                        :
                        AveragePrice * (this.IsNRateSourceFactorNull() ? 1.0 : NRateSourceFactor.Value);
                }
            }
        }

        public double xPerformanceOnIndexClose { get { return (m_closePrice == 0.0 || Instrument.InstrumentType.IsCash || Nominal == 0.0) ? 0.0 : (m_lastPrice + m_priceOffset) / (m_closePrice + m_priceOffsetClosed) - 1.0; } }
        public double xPerformanceFromBegin
        {
            get
            {
                return
                    (Instrument.InstrumentType.IsCash || Instrument.InstrumentType.IsInventory || Instrument.InstrumentType.IsFXSpot || Nominal == 0.0)
                    ? 0.0
                    : (m_lastPrice + m_priceOffset) / AveragePrice - 1.0;
            }
        }

        public double xInvestedValue
        {
            get
            {
                if (Instrument.InstrumentType.IsCash)
                {
                    return InvestedValue.HasValue ? InvestedValue.Value : 0;
                }
                else
                {
                    return xAveragePrice * Nominal * Instrument.ContractSize / ((Instrument.InstrumentType.IsBond) ? 100.0 : 1.0);
                }
            }
        }

        public double xLeverage
        {
            get
            {
                double accountMarketValue = (Account.IndexFactorDouble != 0.0) ? (Account.xIndexValue / Account.IndexFactorDouble) : (0.0);
                double leverage = (accountMarketValue != 0.0) ? (xMarketValue / accountMarketValue) : (0.0);
                return leverage;
            }
        }

        public double xMarketValueClosed
        {
            get
            {
                return (m_closePrice + m_priceOffsetClosed) * Nominal * Instrument.ContractSize / ((Instrument.InstrumentType.IsBond) ? 100.0 : 1.0);
            }
        }

        public double xSPI
        {
            get
            {
                double contractSize = Instrument.ContractSize;

                if (Instrument.InstrumentType.IsInventory)
                    return 0.0;
                else if (Instrument.InstrumentType.IsFXSpot)
                    return 0.0;
                else if (Instrument.InstrumentType.IsEquityOption)
                    contractSize = 1.0; // on Feb 5, 2019 Dennis sais : the SPI on the Equity Options need to be without Contract size since when I update the Package in Sophis and use the Equity Options as underliyngs Sophis already multiplies with contract size
                else if (Instrument.InstrumentType.IsBond)
                    contractSize = 0.01;

                double spi = Nominal * contractSize * Account.IndexFactorDouble;
                if ((!Account.IsSPIroundingNull()) && (Account.SPIrounding.Value != 0)) // 0 means no rounding
                    spi = Math.Round(spi, Account.SPIrounding.Value);

                return spi;
            }
        }


        public bool IsSophisPortfolioIdentifierNull()
        {
            return SophisPortfolioIdentifier == null;
        }
        public void SetSophisPortfolioIdentifierNull()
        {
            SophisPortfolioIdentifier = null;
        }

        public bool IsManualInstrumentLastPriceNull()
        {
            return ManualInstrumentLastPrice == null;
        }

        public void SetManualInstrumentLastPriceNull()
        {
            ManualInstrumentLastPrice = null;
        }

        public bool IsManualValueLastPriceNull()
        {
            return ManualValueLastPrice == null;
        }

        public void SetManualValueLastPriceNull()
        {
            ManualValueLastPrice = null;
        }

        public bool IsManualValueLastFXNull()
        {
            return !ManualValueLastFX.HasValue;
        }
        public void SetManualValueLastFXNull()
        {
            ManualValueLastFX = null;
        }


        public double xMarketValue
        {
            get
            {
                return (m_lastPrice + m_priceOffset) * Nominal * Instrument.ContractSize / ((Instrument.InstrumentType.IsBond) ? 100.0 : 1.0);
            }
        }

        public IEnumerable<SwapEvent> EventRowsByPortfolioSource { get => null; }
        public IEnumerable<SwapEvent> EventRowsByPortfolioTarget { get => null; }

        public List<SwapEvent> getSortedEventRowsOfType(List<TypeManastEvent> types)
        {
            var result = new List<SwapEvent>();

            foreach (var er in EventRowsByPortfolioSource)
            {
                if (types.Contains((TypeManastEvent)(er.EventType)))
                    result.Add(er);
            }
            result.Sort((a, b) => a.ExecutionDate.CompareTo(b.ExecutionDate));
            return result;
        }

        public void SetGridProperties(double closePrice, double closePriceCcy, double lastPrice, double lastPriceCcy, double bidPrice, double askPrice, double priceOffset, double priceOffsetClosed, double lastFxRate, double closeFxRate, double delta, double gamma, Dictionary<string, List<PriceTip>> tips)
        {
            // make copy of last price
            m_lastPricePred = m_lastPrice;

            // apply new prices
            m_closePrice = closePrice;
            m_closePriceCcy = closePriceCcy;
            m_lastPrice = lastPrice;
            m_lastPriceCcy = lastPriceCcy;

            m_priceOffset = priceOffset;
            m_priceOffsetClosed = priceOffsetClosed;

            m_LastFxRate = lastFxRate;
            m_CloseFxRate = closeFxRate;

            // if bid or ask = 0, then try close or last field
            m_bid_price = (bidPrice == 0.0) ? (closePrice == 0.0) ? lastPrice : closePrice : bidPrice;
            m_ask_price = (askPrice == 0.0) ? (closePrice == 0.0) ? lastPrice : closePrice : askPrice;

            m_Delta = delta;
            m_Gamma = gamma;

            m_Tips = tips;

            SwapAccountPricing.PortfolioPricer.calculateFeeColumns(this);

            NotifyAllPropertiesChanged();
        }


        private void NotifyAllPropertiesChanged()
        {
            foreach (var prop in this.GetType().GetProperties())
            {
                NotifyPropertyChanged(prop.Name);
            }
        }

        public void SetGridProperties2(double investedWeight, double marketWeight, double marketWeightCollateral)
        {
            m_investedWeight = investedWeight;
            m_marketWeight = marketWeight;
            m_marketWeightCollateral = marketWeightCollateral;
            NotifyAllPropertiesChanged();
        }

        public static readonly Dictionary<string, string> ReportCaptionToPropertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Name", nameof(xInstrumentName) },
            { "RIC", nameof(xRIC) },
            { "ISIN", nameof(xISIN) },
            { "BBG", nameof(xBBG) },
            { "WKN", nameof(xInstrumentWKN) },
            { "SICOVAM", nameof(xInstrumentSicovam) },
            { "Domicile", nameof(xInstrumentCountry) },
            { "Tax", nameof(Tax) },
            { "Type", nameof(xInstrumentType) },
            { "Nominal", nameof(Nominal) },
            { "Sophis Cross Check", nameof(xSophisNominalDifference) },
            { "Sophis Delta Check", nameof(xSophisDeltaCheck) },
            { "Sophis Gross Check", nameof(xSophisGrossDifference) },
            { "Sophis Event Difference", nameof(xSophisEventDifference) },
            { "Event Count Balance", nameof(EventCountBalance) },
            { "Average Price", nameof(xAveragePrice) },
            { "Close Price", nameof(xClosePrice) },
            { "Close Price ccy", nameof(xClosePriceCcy) },
            { "Last Price", nameof(xLastPrice) },
            { "Last Price ccy", nameof(xLastPriceCcy) },
            { "Nominal (Collateral)", nameof(xCollateral) },
            { "Pr. Offset", nameof(xPriceOffset) },
            { "Dirty Price", nameof(xDirtyPrice) },
            { "Offset Value", nameof(xOffsetValue) },
            { "Inst.Curr.", nameof(xForeignExchange) },
            { "FX Rate", nameof(xFxRate) },
            { "Close FX Rate", nameof(xCloseFxRate) },
            { "P.from Begin", nameof(xPerformanceFromBegin) },
            { "P. on Close", nameof(xPerformanceOnIndexClose) },
            { "Invested Value", nameof(xInvestedValue) },
            { "Market Value", nameof(xMarketValue) },
            { "Swap MTM", nameof(xSwapMTM) },
            { "Market Value (Collateral)", nameof(xMarketValueCollateral) },
            { "Leverage", nameof(xLeverage) },
            { "Close Value", nameof(xMarketValueClosed) },
            { "Unrealized PL", nameof(xUnrealPL) },
            { "Unrealized PL Close", nameof(xUnrealPLClose) },
            { "Invested Weight", nameof(xInvestedWeight) },
            { "Market Weight", nameof(xMarketWeight) },
            { "Market Weight (Collateral)", nameof(xMarketWeightCollateral) },
            { "SPI", nameof(xSPI) },
            { "Price (ask)", nameof(xAskPrice) },
            { "Price (bid)", nameof(xBidPrice) },
            { "Bid-ask spread", nameof(xBidAskSpreadPercent) },
            { "Weighted Spread", nameof(xWeightedBidAskSpread) },
            { "SPI Check", nameof(xSPICheck) },
            { "Expiry", nameof(xInstrumentMaturity) },
            { "Days to Expiry", nameof(xInstrumentDaysToMaturity) },
            { "Days to Div", nameof(xDaysToDiv) },
            { "Ex Div Date", nameof(xExDivDate) },
            { "Strike1", nameof(xStrike1) },
            { "Strike2", nameof(xStrike2) },
            { "Delta %", nameof(xDeltaPct) },
            { "Delta", nameof(xDelta) },
            { "OptionType", nameof(xOptionType) },
            { "Tipp Asset Class", nameof(xTippAssetClass) },
            { "Tipp Asset Category", nameof(xTippAssetClassCategory) },
            { "Tipp Asset Risk", nameof(xTippAssetRiskProfile) },
            { "Value Date", nameof(xValueDate) },
            { "Gamma %", nameof(xGammaPct) },
            { "Sum Fee 1", nameof(xSumMgmtFee1) },
            { "Sum Fee 2", nameof(xSumMgmtFee2) },
            { "Sum Fee 3", nameof(xSumMgmtFee3) },
            { "Sum Perf Fee", nameof(xSumPerfFee) },
            { "Sum (Fee 1 per unit)", nameof(xSumMgmtFeePerUnit1) },
            { "Rebate Rate", nameof(xRebateRate) },
            { "871m Relevant", nameof(xUSWHTToString) } };

        public static readonly HashSet<string> ColumnsToSumByCaption = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Sophis Cross Check",
            "Sophis Delta Check",
            "Sophis Gross Check",
            "Sophis Event Difference",
            "Offset Value",
            "Invested Value",
            "Market Value",
            "Market Value (Collateral)",
            "Leverage",
            "Close Value",
            "Unrealized PL",
            "Unrealized PL Close",
            "Invested Weight",
            "Market Weight",
            "Market Weight (Collateral)",
            "SPI Check",
            "Sum Fee 1",
            "Sum Fee 2",
            "Sum Fee 3",
            "Sum Perf Fee",
            "Sum (Fee 1 per unit)", };

        // INotifyPropertyChanged
        public override event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

}
