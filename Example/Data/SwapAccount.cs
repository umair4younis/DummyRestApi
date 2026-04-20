using Puma.MDE.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccount : Entity
    {
        public void Initialize(bool hideClosedPositions)
        {
            m_IncludeClosedPositions = !hideClosedPositions;
        }
        public void ApplyPricingResult(PricingResult r)
        {
            ApplyRowUpdates(r);
            ApplyCashFee(r);
            ApplyAccountTotals(r);
        }

        public bool IsBeingEdited { get; set; } = false;

        private void ApplyRowUpdates(PricingResult result)
        {
            if (result == null || result.RowUpdates == null || result.RowUpdates.Count == 0)
                return;

            foreach (var upd in result.RowUpdates)
            {
                SwapAccountPortfolio pr = null;
                if (pr == null)
                    continue; // Row could be filtered/removed; skip gracefully

                // 1) Prices / offsets / FX / greeks
                pr.SetGridProperties(
                    upd.ClosePrice,
                    upd.ClosePriceCcy,
                    upd.LastPrice,
                    upd.LastPriceCcy,
                    upd.BidPrice,
                    upd.AskPrice,
                    upd.PriceOffset,
                    upd.PriceOffsetClosed,
                    upd.LastFx,
                    upd.CloseFx,
                    upd.Delta,
                    upd.Gamma,
                    upd.Tips);

                // 2) Weights (already normalized in compute step)
                pr.SetGridProperties2(
                    upd.InvestedWeight,
                    upd.MarketWeight,
                    upd.MarketWeightCollateral
                );
            }
        }

        private void ApplyCashFee(PricingResult result)
        {
            var instr = result.CashFee;
            if (instr == null || !instr.HasInstruction) return;

            // Index rows once (if you’re also applying RowUpdates)
            var rows = this.AccountPortfolioRows;

            string error = "";
            SwapAccountPortfolio cashFeePortfolio = null;
            if (cashFeePortfolio == null)
            {
                Engine.Instance.Log.Error($"Add CASH Fee failed for '{this.AccountName}': {error}");
            }
            else
            {
                cashFeePortfolio.Nominal = instr.NewNominal;
                cashFeePortfolio.SetGridProperties(1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 0, 0, 1.0, 1.0, 1.0, 0.0, instr.prCashFeeTips);
            }
        }

        private void ApplyAccountTotals(PricingResult r)
        {
            if (r == null) return;

            // Indices based on latest totals & account’s index factor
            var mvIndex = r.MvSum * this.IndexFactorDouble;
            var mvClosedIndex = r.MvClosedSum * this.IndexFactorDouble;

            this.SetAccountProperties(r.MvSum, r.MvRaw, r.MvCollateralSum, r.MvSum * this.IndexFactorDouble, r.MvClosedSum * this.IndexFactorDouble,
                r.WeightedBidAskSpreadSum, r.MvSum, r.MvCash / r.MvSum, r.MvFutureCash / r.MvSum, r.MvFuture / r.MvSum, r.MvBond / r.MvSum, r.MvStock / r.MvSum,
                r.MvFund / r.MvSum, r.MvETF / r.MvSum, r.MvCertificate / r.MvSum, r.IsIndexComplete,
                r.IsIndexCloseComplete, r.LeverageFactor, r.Sum_5_10_40, r.TippVolatility, r.TippHighRiskExposureByCategory, r.TippHighRiskExposureByAssetClass, r.ArTips);
        }

        public String DbName { get; set; }
        private String m_AccountName { get; set; }
        public String AccountName
        {
            get => m_AccountName;
            set
            {
                if (value != m_AccountName)
                {
                    m_AccountName = value;
                    NotifyPropertyChanged(nameof(AccountName));
                }
            }
        }
        private String m_Description { get; set; }
        public String Description
        {
            get => m_Description; set
            {
                if (value != m_Description)
                {
                    m_Description = value;
                    NotifyPropertyChanged(nameof(Description));
                }
            }
        }
        private string m_currency { get; set; }
        public String Currency
        {
            get => m_currency; set
            {
                if (m_currency != value)
                {
                    m_currency = value;
                    NotifyPropertyChanged(() => Currency);
                }
            }
        }


        private double? xSwapNotional { get; set; }
        public double SwapNotional
        {
            get
            {
                if (xSwapNotional.HasValue) return xSwapNotional.Value;
                else return 0;
            }
            set
            {
                if (xSwapNotional != value)
                {
                    xSwapNotional = value;
                    NotifyPropertyChanged(() => SwapNotional);
                }
            }
        }
        private double? xSwapStrike { get; set; }
        public double SwapStrike
        {
            get
            {
                return xSwapStrike.HasValue ? xSwapStrike.Value : 0;
            }
            set
            {
                if (xSwapStrike != value)
                {
                    xSwapStrike = value;
                    NotifyPropertyChanged(() => SwapStrike);
                }
            }
        }
        private double? m_TNA { get; set; }
        public double? TNA
        {
            get => m_TNA; set
            {
                if (m_TNA != value)
                {
                    m_TNA = value;
                    NotifyPropertyChanged(nameof(TNA));
                }
            }
        }
        public bool IsTNANull()
        {
            return !TNA.HasValue;
        }

        private int? m_ContributeMode { get; set; }
        public int? ContributeMode
        {
            get => m_ContributeMode; set
            {
                if (m_ContributeMode != value)
                {
                    m_ContributeMode = value;
                    NotifyPropertyChanged(nameof(ContributeMode));
                }
            }
        }
        public int? OrderReportStyle { get; set; }
        public bool IsOrderReportStyleNull()
        {
            return !OrderReportStyle.HasValue;
        }
        private int? m_IndexSnapshotMode { get; set; }
        public int? IndexSnapshotMode
        {
            get => m_IndexSnapshotMode; set
            {
                if (m_IndexSnapshotMode != value)
                {
                    m_IndexSnapshotMode = value;
                    NotifyPropertyChanged(nameof(IndexSnapshotMode));
                }
            }
        }
        public bool IsIndexSnapshotModeNull()
        {
            return !IndexSnapshotMode.HasValue;
        }
        private int? m_AccruedUntilDate { get; set; }
        public int? AccruedUntilDate
        {
            get => m_AccruedUntilDate; set
            {
                if (m_AccruedUntilDate != value)
                {
                    m_AccruedUntilDate = value;
                    NotifyPropertyChanged(nameof(AccruedUntilDate));
                }
            }
        }
        public bool IsAccruedUntilDateNull()
        {
            return !AccruedUntilDate.HasValue;
        }

        private bool? UseCloseAsLast { get; set; }
        public bool xUseCloseAsLast
        {
            get
            {
                return UseCloseAsLast.HasValue ? UseCloseAsLast.Value : false;
            }
            set
            {
                if (UseCloseAsLast != value)
                {
                    this.UseCloseAsLast = value;
                    NotifyPropertyChanged(nameof(xUseCloseAsLast));
                }
            }
        }

        public double? _871mTaxPct { get; set; }
        public bool Is_871mTaxPctNull()
        {
            return !_871mTaxPct.HasValue;
        }
        public double x871mTaxPct
        {
            get
            {
                if (Is_871mTaxPctNull())
                    return 0;
                else
                    return _871mTaxPct.Value;
            }
            set
            {
                if (_871mTaxPct != value)
                {
                    _871mTaxPct = value;
                    NotifyPropertyChanged(() => x871mTaxPct);
                }
            }
        }

        public bool? _871mApplyStepInRatio { get; set; }
        public bool Is_871mApplyStepInRatioNull()
        {
            return !_871mApplyStepInRatio.HasValue;
        }
        public bool x871mApplyStepInRatio
        {
            get
            {
                if (Is_871mApplyStepInRatioNull())
                    return false;
                else
                    return _871mApplyStepInRatio.Value;
            }
            set
            {
                if (_871mApplyStepInRatio != value)
                {
                    _871mApplyStepInRatio = value;
                    NotifyPropertyChanged(() => x871mApplyStepInRatio);
                }
            }
        }
        public bool? _871mRelevant { get; set; }
        public bool x871mRelevant
        {
            get
            {
                if (Is871mRelevantNull()) return false;
                else
                    return _871mRelevant.Value;
            }
            set
            {
                if (_871mRelevant != value)
                {
                    _871mRelevant = value;
                    NotifyPropertyChanged(() => x871mRelevant);
                }
            }
        }
        public bool Is871mRelevantNull()
        {
            return !_871mRelevant.HasValue;
        }

        public int? BookDividendMethod { get; set; }
        public bool IsBookDividendMethodNull()
        {
            return !BookDividendMethod.HasValue;
        }

        public TypeBookDividendMethod BookDividendMethodEnum
        {
            get => (TypeBookDividendMethod)BookDividendMethod;
            set
            {
                if (BookDividendMethod != (int)value)
                {
                    BookDividendMethod = (int)value;
                    NotifyPropertyChanged(nameof(BookDividendMethodEnum));
                    NotifyPropertyChanged(nameof(BookDividendMethod));
                }
            }
        }

        public bool? xHas_Pending_MOS_Order { get; set; }

        public bool Has_Pending_MOS_Order
        {
            get
            {
                return xHas_Pending_MOS_Order.HasValue && xHas_Pending_MOS_Order.Value;
            }
            set
            {
                xHas_Pending_MOS_Order = value;
            }
        }



        public IList<SwapAccountCrossWeight> CrossWeightRows { get; set; } = new List<SwapAccountCrossWeight>();
        public ObservableCollection<SwapAccountCrossWeight> m_CrossWeightRowsObservable { get; } = new ObservableCollection<SwapAccountCrossWeight>();
        public ObservableCollection<SwapAccountCrossWeight> CrossWeightRowsObservable
        {
            get
            {
                m_CrossWeightRowsObservable.Clear();
                foreach (var row in CrossWeightRows)
                {
                    m_CrossWeightRowsObservable.Add(row);
                }
                return m_CrossWeightRowsObservable;
            }
        }
        private bool m_IncludeClosedPositions { get; set; } = false;
        public IEnumerable<SwapAccountPortfolio> AccountPortfolioRows { get => null; }
        public int AccountActivePortfolioRowsCount { get => this.Id; }
        public int CloseType { get; set; }
        public TypeManastCloseAsLast CloseTypeEnum
        {
            get => (TypeManastCloseAsLast)CloseType;
            set
            {
                if (CloseType != (int)value)
                {
                    CloseType = (int)value;
                    NotifyPropertyChanged(nameof(CloseTypeEnum));
                    NotifyPropertyChanged(nameof(CloseType));
                    if (CloseType == (int)TypeManastCloseAsLast.DATE && !CloseDate.HasValue)
                    {
                        xCloseDate = DateTime.Now;
                    }
                }
            }
        }

        public DateTime? CloseDate { get; set; }

        public DateTime xCloseDate
        {
            get
            {
                return (!CloseDate.HasValue) ? DateTime.MinValue : CloseDate.Value;
            }
            set
            {
                if (CloseDate != value)
                {
                    CloseDate = value;
                    NotifyPropertyChanged(nameof(xCloseDate));
                }
            }
        }

        public DateTime CreateDate { get; set; }
        public String EventExecTime { get; set; }
        private DateTime m_EventExecuted { get; set; }
        public DateTime EventExecuted
        {
            get => m_EventExecuted;
            set
            {
                if (m_EventExecuted != value)
                {
                    m_EventExecuted = value;
                    NotifyPropertyChanged(nameof(EventExecuted));
                    NotifyPropertyChanged(nameof(EventExecutedString));
                }
            }
        }

        public string EventExecutedString
        {
            get
            {
                return "Last Execution: " + EventExecuted.ToString("dd/MM/yyyy HH:mm");
            }
        }


        private bool? UseWMForValuation { get; set; }
        public bool xUseWMForValuation
        {
            get
            {
                if (IsUseWMForValuationNull()) return false;
                else return UseWMForValuation.Value;
            }
            set
            {
                if (UseWMForValuation != value)
                {
                    UseWMForValuation = value;
                    NotifyPropertyChanged(nameof(xUseWMForValuation));
                }
            }
        }
        public bool IsUseWMForValuationNull()
        {
            return !UseWMForValuation.HasValue;
        }
        private bool? UseWMForEventBooking { get; set; }
        public bool xUseWMForEventBooking
        {
            get
            {
                if (IsUseWMForEventBookingNull()) return false;
                else return UseWMForEventBooking.Value;
            }
            set
            {
                if (UseWMForEventBooking != value)
                {
                    UseWMForEventBooking = value;
                    NotifyPropertyChanged(nameof(xUseWMForEventBooking));
                }
            }
        }
        public bool IsUseWMForEventBookingNull()
        {
            return !UseWMForValuation.HasValue;
        }

        public decimal IndexFactor { get; set; }
        public double IndexFactorDouble
        {
            get
            {
                return (double)IndexFactor;
            }
        }
        private DateTime m_IndexSnapshotTime { get; set; }
        public DateTime IndexSnapShotTime
        {
            get => m_IndexSnapshotTime;
            set
            {
                if (m_IndexSnapshotTime != value)
                {
                    m_IndexSnapshotTime = value;
                    NotifyPropertyChanged(nameof(IndexSnapShotTime));
                    NotifyPropertyChanged(nameof(IndexSnapshotString));
                }
            }
        }
        public string IndexSnapshotString
        {
            get
            {
                return "Last Snapshot: " + IndexSnapShotTime.ToString("dd/MM/yyyy HH:mm");
            }
        }
        public double IndexSnapShotValue { get; set; }
        public decimal InitialIndexValue { get; set; }
        public double InitialIndexValueDouble
        {
            get
            {
                return (double)InitialIndexValue;
            }
        }

        private double m_HighWaterMark { get; set; }
        public double HighWaterMark
        {
            get => m_HighWaterMark; set
            {
                if (m_HighWaterMark != value)
                {
                    m_HighWaterMark = value;
                    NotifyPropertyChanged(nameof(HighWaterMark));
                }
            }
        }
        private double m_PerformanceFee { get; set; } = 0.0;
        public double PerformanceFee
        {
            get => m_PerformanceFee; set
            {
                if (m_PerformanceFee != value)
                {
                    m_PerformanceFee = value;
                    NotifyPropertyChanged(nameof(PerformanceFee));
                }
            }
        }

        public String TippVolatilityInstrument { get; set; }
        public String TippVolatilityInstrument2 { get; set; }
        public double? mTippManualVolatility { get; set; }
        public double TippManualVolatility
        {
            get
            {
                if (IsTippManualVolatilityNull())
                    return 0;
                else
                    return mTippManualVolatility.Value;
            }
            set
            {
                if (mTippManualVolatility != value)
                {
                    mTippManualVolatility = value;
                    NotifyPropertyChanged(() => TippManualVolatility);
                }
            }
        }
        public bool IsTippManualVolatilityNull()
        {
            return !mTippManualVolatility.HasValue;
        }
        public double? mTippManualVolatility2 { get; set; }

        public double TippManualVolatility2
        {
            get
            {
                if (IsTippManualVolatility2Null())
                    return 0;
                else
                    return mTippManualVolatility2.Value;
            }
            set
            {
                if (mTippManualVolatility2 != value)
                {
                    mTippManualVolatility2 = value;
                    NotifyPropertyChanged(() => TippManualVolatility2);
                }
            }
        }
        public bool IsTippManualVolatility2Null()
        {
            return !mTippManualVolatility2.HasValue;
        }

        public int? TippMultiplierDefinition { get; set; }
        public bool IsTippMultiplierDefinitionNull()
        {
            return !TippMultiplierDefinition.HasValue;
        }

        public bool? UseManualFX { get; set; }
        public bool xUseManualFX
        {
            get
            {
                if (IsUseManualFXNull()) return false;
                else return UseManualFX.Value;
            }
            set
            {
                if (UseManualFX != value)
                {
                    UseManualFX = value;
                    NotifyPropertyChanged(nameof(xUseManualFX));
                }
            }
        }
        public bool IsUseManualFXNull()
        {
            return !UseManualFX.HasValue;
        }

        private double? m_StopLoss { get; set; }
        public double? StopLoss
        {
            get => m_StopLoss;
            set
            {
                if (m_StopLoss != value)
                {
                    m_StopLoss = value;
                    NotifyPropertyChanged(nameof(StopLoss));
                }
            }
        }
        public double xStopLoss { get { return IsStopLossNull() ? 0 : StopLoss.Value; } }
        public bool IsStopLossNull() { return !StopLoss.HasValue; }
        private double? m_DayStopLoss { get; set; }
        public double? DayStopLoss
        {
            get => m_DayStopLoss; set
            {
                if (m_DayStopLoss != value)
                {
                    m_DayStopLoss = value;
                    NotifyPropertyChanged(nameof(DayStopLoss));
                }
            }
        }
        public double xDayStopLoss { get { return IsDayStopLossNull() ? 0 : DayStopLoss.Value; } }
        public bool IsDayStopLossNull() { return !DayStopLoss.HasValue; }

        public bool getIndexValue(XStopLossData data, DateTime today, out XIndexValue indexValue)
        {
            DateTime indexDate = DateTime.MinValue; // date on which to take index value from history, used to calculate the performance

            switch (data.mKind)
            {
                case "IntraDay":
                    {
                        indexDate = getYesterday(today);
                        break;
                    }
                case "AllTime":
                    {
                        indexDate = CreateDate;
                        break;
                    }
                case "YearToDate":
                    {
                        indexDate = new DateTime(today.Year, 1, 1);
                        break;
                    }
                case "QuarterToDate":
                    {
                        if (today.Month < 4)
                            indexDate = new DateTime(today.Year, 1, 1);
                        else if (today.Month < 7)
                            indexDate = new DateTime(today.Year, 4, 1);
                        else if (today.Month < 10)
                            indexDate = new DateTime(today.Year, 7, 1);
                        else
                            indexDate = new DateTime(today.Year, 10, 1);
                        break;
                    }
                case "MonthToDate":
                    {
                        indexDate = new DateTime(today.Year, today.Month, 1);
                        break;
                    }
                case "DateToDate":
                    {
                        indexDate = data.mDate;
                        break;
                    }
                default:
                    break;
            }

            if (indexDate <= CreateDate)
            {
                indexValue = new XIndexValue((double)this.InitialIndexValue, this.CreateDate);
                return true;
            }

            indexDate = GetMatchingBusinessDay(indexDate, false /* following */, true); // take index value from first business day that is >= the date that we seek after

            if (indexDate == today)
            {
                indexValue = new XIndexValue(this.m_indexValue, today);
                return true;
            }
            else
            {
                indexValue = new XIndexValue();
                return getIndexValueOnDate(indexDate, false /* following */, ref indexValue); // look in history
            }
        }
        public bool calculateStopLossPerformance(XStopLossData data, DateTime today, ref XStopLossPerformance p)
        {
            p.mPerformance = 0.0;
            bool found = getIndexValue(data, today, out p.mIndexValue);
            if (found)
                p.mPerformance = getPerformance(m_indexValue, p.mIndexValue.mValue);
            return found;
        }

        public DateTime IndexSnapShotSaved { get; set; }
        public int? xMgmFeeCalcType { get; private set; }
        public int MgmFeeCalcType
        {
            get
            {
                if (IsMgmFeeCalcTypeNull())
                    return 0;
                else
                    return xMgmFeeCalcType.Value;
            }
            set
            {
                if (xMgmFeeCalcType != value)
                {
                    xMgmFeeCalcType = value;
                    NotifyPropertyChanged(() => MgmFeeCalcType);
                }
            }
        }
        public TypeManagementFeeCalculation MgmFeeCalcTypeEnum
        {
            get => (TypeManagementFeeCalculation)MgmFeeCalcType;
            set
            {
                if (MgmFeeCalcType != (int)value)
                {
                    MgmFeeCalcType = (int)value;
                    NotifyPropertyChanged(nameof(MgmFeeCalcType));
                    NotifyPropertyChanged(nameof(MgmFeeCalcTypeEnum));
                }
            }
        }

        public bool IsMgmFeeCalcTypeNull() { return !xMgmFeeCalcType.HasValue; }
        private double m_MgmFeeBasis { get; set; }
        public double MgmFeeBasis
        {
            get => m_MgmFeeBasis;
            set
            {
                if (m_MgmFeeBasis != value)
                {
                    m_MgmFeeBasis = value;
                    NotifyPropertyChanged(nameof(MgmFeeBasis));
                }
            }
        }
        public DateTime? MgmFeeStartDate { get; set; }
        public bool IsMgmFeeStartDateNull()
        {
            return !MgmFeeStartDate.HasValue;
        }
        public bool? xApplyMgmFee { get; set; }
        public bool IsApplyMgmFeeNull()
        {
            return !xApplyMgmFee.HasValue;
        }

        public bool ApplyMgmFee
        {
            get
            {
                if (IsApplyMgmFeeNull()) return false;
                else return xApplyMgmFee.Value;
            }
            set
            {
                if (xApplyMgmFee != value)
                {
                    xApplyMgmFee = value;
                    NotifyPropertyChanged(() => ApplyMgmFee);
                }
            }
        }

        private bool? BookEvents { get; set; }
        public bool xBookEvents
        {
            get
            {
                return BookEvents.HasValue ? BookEvents.Value : true;
            }
            set
            {
                if (BookEvents != value)
                {
                    BookEvents = value;
                    NotifyPropertyChanged(nameof(xBookEvents));
                }
            }
        }

        private DateTime? _archivingDate;
        public DateTime? ArchivingDate
        {
            get => _archivingDate;
            set
            {
                if (_archivingDate != value)
                {
                    _archivingDate = value;
                    NotifyPropertyChanged(nameof(IsArchived));
                    NotifyPropertyChanged(nameof(ArchivingDate));
                }
            }
        }
        public bool IsArchivingDateNull()
        {
            return !IsArchived;
        }
        public bool IsArchived
        {
            get => ArchivingDate.HasValue;
            set
            {
                if (value && !ArchivingDate.HasValue)
                {
                    ArchivingDate = DateTime.Today;
                }
                else if (!value) { ArchivingDate = null; }

                NotifyPropertyChanged(nameof(IsArchived));
                NotifyPropertyChanged(nameof(ArchivingDate));
            }
        }
        public TypeTippMultiplierDefinition xTippMultiplierDefinition { get { return (IsTippMultiplierDefinitionNull()) ? TypeTippMultiplierDefinition.None : (TypeTippMultiplierDefinition)(TippMultiplierDefinition); } }

        public bool xIsTipp
        {
            get { return (xTippMultiplierDefinition != TypeTippMultiplierDefinition.None); }
        }

        public bool IsTippManualVolatilityInstrumentNull()
        {
            return TippVolatilityInstrument == null;
        }
        public bool IsTippManualVolatilityInstrument2Null()
        {
            return TippVolatilityInstrument2 == null;
        }

        private double? m_ManualRatio { get; set; }
        public double? ManualRatio
        {
            get => m_ManualRatio; set
            {
                if (m_ManualRatio != value)
                {
                    m_ManualRatio = value;
                    NotifyPropertyChanged(nameof(ManualRatio));
                }
            }
        }
        public bool IsManualRatioNull()
        {
            return !ManualRatio.HasValue;
        }

        public double? SwapDividendRatio { get; set; }
        public bool IsSwapDividendRatioNull()
        {
            return !SwapDividendRatio.HasValue;
        }
        public int? SwapDividendPaymentRule { get; set; }
        public bool IsSwapDividendPaymentRuleNull()
        {
            return !SwapDividendPaymentRule.HasValue;
        }

        private int? m_swapPortfolio { get; set; }
        public int SwapPortfolio
        {
            get
            {
                if (m_swapPortfolio.HasValue) return m_swapPortfolio.Value;
                else return 0;
            }
            set
            {
                if (m_swapPortfolio != value)
                {
                    m_swapPortfolio = value;
                    NotifyPropertyChanged(() => SwapPortfolio);
                }
            }
        }

        private string m_swapReference { get; set; }
        public string SwapReference
        {
            get
            {
                if (IsSwapReferenceNull()) return string.Empty;
                else return m_swapReference;
            }
            set
            {
                if (m_swapReference != value)
                {
                    m_swapReference = value;
                    NotifyPropertyChanged(() => SwapReference);
                }
            }
        }
        public bool IsSwapReferenceNull() { return m_swapReference == null; }

        private String StepInUnderlying { get; set; }
        public String xStepInUnderlying
        {
            get
            {
                return IsStepInUnderlyingNull() ? String.Empty : StepInUnderlying;
            }
        }
        public bool IsStepIn
        {
            get { return (xStepInUnderlying != ""); }
        }
        private bool m_IsDirtyStepIn = true;
        public bool IsDirtyStepIn
        {
            get { return m_IsDirtyStepIn; }
            set { m_IsDirtyStepIn = value; }
        }

        private DateTime? m_NextStepIn = null;
        public DateTime? xNextStepIn
        {
            get
            {
                if (IsDirtyStepIn == true)
                    refreshDirtyStepIn();
                return m_NextStepIn;
            }
        }
        private DateTime? m_LastStepIn = null;
        public DateTime? xLastStepIn
        {
            get
            {
                if (IsDirtyStepIn == true)
                    refreshDirtyStepIn();
                return m_LastStepIn;
            }
        }

        private DateTime? m_NextStepInDiv = null;
        public DateTime? xNextStepInDiv
        {
            get
            {
                if (IsDirtyStepIn == true)
                    refreshDirtyStepIn();
                return m_NextStepInDiv;
            }
        }

        private int m_DaysToNextStepInDiv = -1;
        public int xDaysToNextStepInDiv
        {
            get
            {
                if (IsDirtyStepIn == true)
                    refreshDirtyStepIn();
                return m_DaysToNextStepInDiv;
            }
        }

        public double xPerformance
        {
            get
            {
                return getPerformance(m_indexValue, (double)InitialIndexValue);
            }
        }

        public double xDayPerformance
        {
            get
            {
                DateTime yesterday = getYesterday(DateTime.Now.Date);
                double yesterdayIndexValue = getIndexValueOnDate(yesterday, true); // get yesterday's index value from the account history
                return getPerformance(m_indexValue, yesterdayIndexValue);
            }
        }

        public double xYesterdayPerformance
        {
            get
            {
                DateTime yesterday = getYesterday(DateTime.Now);
                DateTime beforeYesterday = getYesterday(yesterday);

                double yesterdayIndexValue = getIndexValueOnDate(yesterday.Date, true);
                double beforeYesterdayIndexValue = getIndexValueOnDate(beforeYesterday.Date, true);
                return getPerformance(yesterdayIndexValue, beforeYesterdayIndexValue);
            }
        }

        private DateTime getYesterday(DateTime date)
        // return yesterday, make sure it is a business day
        {
            DateTime yesterday = date;
            yesterday = yesterday.AddDays(-1);
            yesterday = GetMatchingBusinessDay(yesterday, true, true);
            return yesterday;
        }

        Dictionary<DateTime, XIndexValue> m_IndexValueCache = new Dictionary<DateTime, XIndexValue>();
        public void ResetIndexValueCache()
        {
            m_IndexValueCache.Clear();
        }

        public double getIndexValueOnDate(DateTime date, bool preceding)
        {
            XIndexValue indexValue = new XIndexValue();
            getIndexValueOnDate(date.Date, preceding, ref indexValue);
            return indexValue.mValue;
        }


        public bool getIndexValueOnDate(DateTime date, bool preceding, ref XIndexValue indexValue)
        {
            // for performance reasons, fetch index value only once and store it in a cache
            if (!m_IndexValueCache.TryGetValue(date.Date, out indexValue))
            {
                indexValue = new XIndexValue();
                double dummy = 0.0;
                m_IndexValueCache[date.Date] = indexValue;
            }

            return (indexValue.mDate != DateTime.MinValue);
        }

        public double getPerformance(double indexValue, double referenceIndexValue)
        {
            if (referenceIndexValue == 0.0)
                return 0.0;
            else
                return (indexValue - referenceIndexValue) / referenceIndexValue;
        }


        public bool IsStepInUnderlyingNull()
        {
            return StepInUnderlying == null;
        }

        private void refreshDirtyStepIn()
        // recalculate the step in fields : step in ratio, next fixing date, last fixing date, next dividend date, ..
        {
            IsDirtyStepIn = false;

            m_StepInRatio = 0.0;
            m_StepInError = string.Empty;
            m_NextStepIn = null;
            m_LastStepIn = null;
            m_NextStepInDiv = null;
            m_DaysToNextStepInDiv = -1;

            if (!IsStepIn)
                return;

            DateTime today = DateTime.Now.Date;

            // fixings
            {
                List<XStepInFixing> fixings = null;
                loadStepInFixings(out fixings);

                // ratio
                if (!XStepInFixing.calculateStepInRatio(fixings, today, out m_StepInRatio, out m_StepInError))
                {
                    string msg = string.Format("{0} for '{1}'", m_StepInError, AccountName);
                    Engine.Instance.Log.Error(msg);
                }

                // next (upcoming)
                m_NextStepIn = null;
                int i = 0;
                while ((i < fixings.Count) && (fixings[i].Date < today))
                    i++;
                if (i < fixings.Count)
                    m_NextStepIn = fixings[i].Date;

                // last
                m_LastStepIn = null;
                if (fixings.Count > 0)
                    m_LastStepIn = fixings[fixings.Count - 1].Date;
            }

            // dividends
            {
                List<XStepInDividend> dividends = null;
                loadStepInDividends(out dividends);

                // next (upcoming)
                m_NextStepInDiv = null;
                int i = 0;
                while ((i < dividends.Count) && (dividends[i].ExDate < DateTime.Now.Date))
                    i++;
                if (i < dividends.Count)
                    m_NextStepInDiv = dividends[i].ExDate;

                // days to next div
                m_DaysToNextStepInDiv = -1;
                if (m_NextStepInDiv.HasValue && m_NextStepInDiv.Value != null)
                {
                    m_DaysToNextStepInDiv = Convert.ToInt32((m_NextStepInDiv.Value.Date - DateTime.Now.Date).TotalDays);
                    if (m_DaysToNextStepInDiv == 0)
                    {
                        string msg = string.Format("Step In dividend today for '{0}'", AccountName);
                        Engine.Instance.Log.Warn(msg);
                    }
                }
            }
        }


        private string m_FeeRatioError = "";
        public string xFeeRatioError
        {
            get { return m_FeeRatioError; }
            set { m_FeeRatioError = value; }
        }

        private double m_StepInRatio = 0.0;
        public double xStepInRatio
        {
            get
            {
                if (IsDirtyStepIn == true)
                    refreshDirtyStepIn();
                return m_StepInRatio;
            }
        }

        private string m_StepInError = "";
        public string xStepInError
        {
            get { return m_StepInError; }
            set { m_StepInError = value; }
        }

        private bool m_IsDirtyFeeRatio = true;
        public bool IsDirtyFeeRatio
        {
            get { return m_IsDirtyFeeRatio; }
            set { m_IsDirtyFeeRatio = value; }
        }
        private double m_FeeRatio = 0.0;
        public double xFeeRatio
        {
            get
            {
                if (IsDirtyFeeRatio == true)
                    refreshDirtyFeeRatio();
                return m_FeeRatio;
            }
        }

        private void refreshDirtyFeeRatio()
        {
            IsDirtyFeeRatio = false;

            m_IsRatioBasedFee = false;
            m_FeeRatio = 0.0;
            m_FeeRatioError = string.Empty;

            int certificateId = 1;
            int optionId = 0;
            m_IsRatioBasedFee = certificateId != 0;

            if (!m_IsRatioBasedFee)
                return;

            DateTime today = DateTime.Now.Date;

            Dictionary<DateTime, double> ratios = null;
            
            if (!ratios.TryGetValue(today, out m_FeeRatio))
            {
                m_FeeRatioError = string.Format("no ratio found on {0} in Sophis (DeltaOne)", today.ToString("dd/MM/yyyy"));
                string msg = string.Format("{0} for '{1}'", m_FeeRatioError, AccountName);
                Engine.Instance.Log.Error(msg);
            }
        }
        private String _certificateReference { get; set; }
        public String CertificateReference
        {
            get => _certificateReference;
            set
            {
                if (_certificateReference != value)
                {
                    _certificateReference = value;
                    IsDirtyFeeRatio = true;
                    NotifyPropertyChanged(() => CertificateReference);
                    NotifyPropertyChanged(() => IsDirtyFeeRatio);
                }
            }
        }
        public String xCertificateReference { get { return CertificateReference == null ? String.Empty : CertificateReference; } }


        private bool m_IsRatioBasedFee = false; // does certificate have a ratio-based fee?
        public bool IsRatioBasedFee
        {
            get
            {
                if (IsDirtyFeeRatio == true)
                    refreshDirtyFeeRatio();
                return m_IsRatioBasedFee;
            }
        }

        public double xRatio
        {
            get
            {
                if ((!IsManualRatioNull()) && (ManualRatio != 0.0))
                    return ManualRatio.Value;
                else if (IsStepIn)
                    return xStepInRatio;
                else if (IsRatioBasedFee)
                    return xFeeRatio;
                return 0.0;
            }
        }

        public string xRatioError
        {
            get
            {
                if ((!IsManualRatioNull()) && (ManualRatio != 0.0))
                    return string.Empty;
                else if (IsStepIn)
                    return xStepInError;
                else if (IsRatioBasedFee)
                    return xFeeRatioError;
                return "";
            }
        }

        public string xUpcomingHoliday
        { // get the next upcoming holiday
            get
            {
                SortedDictionary<DateTime, SortedDictionary<string, bool>> bankHolidays;
                GetBankHolidays(DateTime.Now.Date, out bankHolidays);
                if (bankHolidays.Count > 0)
                    return bankHolidays.ElementAt(0).Key.ToString("dd/MM/yyyy");
                return "";
            }
        }

        public void GetBankHolidays(DateTime beyond, out SortedDictionary<DateTime, SortedDictionary<string, bool>> bankHolidays)
        {
            List<string> calendarNames = new List<string>();
            foreach (SwapAccountHolidayCalendar hol in GetHolidayCalendarRows())
            {
                calendarNames.Add(hol.CalendarName);
            }

            bankHolidays = null;
        }

        public SwapStepInFixing[] GetStepInFixingRows()
        {
            return null;
        }

        public IList<SwapAccountRuleRow> AccountRuleRows { get; set; } = new List<SwapAccountRuleRow>();
        public IList<SwapAccountRuleRow> GetAccountRuleRows()
        {
            return AccountRuleRows;
        }
        public ObservableCollection<SwapAccountRuleRow> m_AccountRuleRowsObservable { get; } = new ObservableCollection<SwapAccountRuleRow>();
        public ObservableCollection<SwapAccountRuleRow> AccountRuleRowsObservable
        {
            get
            {
                m_AccountRuleRowsObservable.Clear();
                foreach (var row in AccountRuleRows)
                {
                    m_AccountRuleRowsObservable.Add(row);
                }
                return m_AccountRuleRowsObservable;
            }

        }
        public List<SwapAccountRuleRow> GetSortedAccountRuleRows()
        {
            List<SwapAccountRuleRow> list = new List<SwapAccountRuleRow>();
            list.AddRange(GetAccountRuleRows());
            list.Sort(delegate (SwapAccountRuleRow t1, SwapAccountRuleRow t2) { return Comparer<Int32>.Default.Compare(t1.Priority, t2.Priority); });
            return list;
        }

        public IList<SwapAccountCrossRuleRow> AccountCrossRuleRows { get; set; } = new List<SwapAccountCrossRuleRow>();
        public IList<SwapAccountCrossRuleRow> GetAccountCrossRuleRows()
        {
            return AccountCrossRuleRows;
        }
        public ObservableCollection<SwapAccountCrossRuleRow> m_AccountCrossRuleRowsObservable { get; } = new ObservableCollection<SwapAccountCrossRuleRow>();
        public ObservableCollection<SwapAccountCrossRuleRow> AccountCrossRuleRowsObservable
        {
            get
            {
                m_AccountCrossRuleRowsObservable.Clear();
                foreach (var row in AccountCrossRuleRows)
                {
                    m_AccountCrossRuleRowsObservable.Add(row);
                }
                return m_AccountCrossRuleRowsObservable;
            }
        }

        public IList<SwapExecutionFeeRow> ExecutionFeeRows { get; set; } = new List<SwapExecutionFeeRow>();

        public ObservableCollection<SwapExecutionFeeRow> m_ExecutionFeeRowsObservable { get; } = new ObservableCollection<SwapExecutionFeeRow>();
        public ObservableCollection<SwapExecutionFeeRow> ExecutionFeeRowsObservable
        {
            get
            {
                m_ExecutionFeeRowsObservable.Clear();
                foreach (var row in ExecutionFeeRows)
                {
                    m_ExecutionFeeRowsObservable.Add(row);
                }
                return m_ExecutionFeeRowsObservable;
            }
        }

        public IList<SwapAccountWeightRow> SwapAccountWeightRows { get; set; } = new List<SwapAccountWeightRow>();

        public ObservableCollection<SwapAccountWeightRow> m_SwapAccountWeightRowsObservable { get; } = new ObservableCollection<SwapAccountWeightRow>();
        public ObservableCollection<SwapAccountWeightRow> SwapAccountWeightRowsObservable
        {
            get
            {
                m_SwapAccountWeightRowsObservable.Clear();
                foreach (var row in SwapAccountWeightRows)
                {
                    m_SwapAccountWeightRowsObservable.Add(row);
                }
                return m_SwapAccountWeightRowsObservable;
            }
        }

        public IEnumerable<SwapTippShareRow> GetTippShareRows() => null;

        public IEnumerable<SwapInvestmentUniverseRow> GetInvestmentUniverseRows() => null;
        public int GetInvestmentUniverseRowsCount() => 1;

        public void loadStepInFixings(out List<XStepInFixing> fixings)
        {
            fixings = new List<XStepInFixing>();
            if (GetStepInFixingRows().Length > 0)
            {
                foreach (SwapStepInFixing r in GetStepInFixingRows())
                    fixings.Add(new XStepInFixing(r.XDate, r.Fixing));
                fixings.Sort(delegate (XStepInFixing f1, XStepInFixing f2) { return Comparer<DateTime>.Default.Compare(f1.Date, f2.Date); }); // sort on Date
            }
        }

        public IList<SwapAccountCrossRuleRow> GetAccountCrossRuleRowsForInstrumentType(int instrumentTypeId)
        // select rules for specified instrument type
        {
            List<SwapAccountCrossRuleRow> list = new List<SwapAccountCrossRuleRow>();
            foreach (SwapAccountCrossRuleRow s in GetAccountCrossRuleRows())
            {
                if (s.getInstrumentTypeIds().Contains(instrumentTypeId))
                    list.Add(s);
            }
            return list;
        }

        SwapAccountStepInDividend[] GetStepInDividendRows()
        {
            return null;
        }
        public void loadStepInDividends(out List<XStepInDividend> dividends)
        {
            dividends = new List<XStepInDividend>();
            if (GetStepInDividendRows().Length > 0)
            {
                foreach (SwapAccountStepInDividend r in GetStepInDividendRows())
                    dividends.Add(new XStepInDividend(r.ExDate, r.PayDate, r.Factor, r.Div, 0.0, r.Booked.Value, r.User, r.RefconsOrEmpty));
                dividends.Sort(delegate (XStepInDividend d1, XStepInDividend d2) { return Comparer<DateTime>.Default.Compare(d1.ExDate, d2.ExDate); }); // sort on Date
            }
        }

        public List<MonthPerformance> GetAccountMonthPerformances()
        // return a list of (month, index on start of month, index on end of month, performance %), eg.  {(201711,100.0,99.0,-1%), (201712,99.0,101.0,2%), (201801,101.0,105.0,5%), ..}
        {
            var query = from h in GetAccountHistoryRows()
                        where h.IndexValue != 0
                        group h by new DateTime(h.xDate.Year, h.xDate.Month, 1) into grp
                        let first = grp.OrderBy(k => k.xDate).First()
                        let last = grp.OrderBy(k => k.xDate).Last()
                        orderby grp.Key
                        select new MonthPerformance
                        {
                            mMonth = grp.Key,
                            mFirstDate = first.xDate,
                            mFirstIndex = first.IndexValue,
                            mLastDate = last.xDate,
                            mLastIndex = last.IndexValue,
                            mPerformance = 0.0
                        };

            List<MonthPerformance> list = new List<MonthPerformance>();
            list = query.ToList();

            // calculate month-over-month performance

            for (int i = 0; i < list.Count; i++)
            {
                if ((i + 1) < list.Count)
                    list[i].mPerformance = (list[i + 1].mFirstIndex /* index value on 1st day of NEXT month */ - list[i].mFirstIndex) / list[i].mFirstIndex;
                else
                    list[i].mPerformance = (list[i].mLastIndex /* most recent index value of THIS month */ - list[i].mFirstIndex) / list[i].mFirstIndex;
            }

            return list;
        }

        private class YearComparer : IComparer<int>
        {
            public YearComparer(bool asc)
            {
                mAsc = asc;
            }

            public int Compare(int x, int y)
            {
                int result = x.CompareTo(y);
                return (mAsc) ? (result) : (-1 * result);
            }

            bool mAsc;
        }
        public SortedDictionary<int, double[]> GetAccountPerformanceTable(List<MonthPerformance> months, bool asc)
        // return {(year, array of performances)} sorted by year (ascending or descending)
        {
            SortedDictionary<int, double[]> table = new SortedDictionary<int, double[]>(new YearComparer(asc));

            try
            {
                int i = 0;
                // while month
                while (i < months.Count)
                {
                    MonthPerformance year = months[i];
                    double[] perf = new double[1 + 12]; // collect 12 month-over-month performances plus 1 year-over-year performance
                    double yearStartIndex = year.mFirstIndex;
                    // while same year
                    while ((i < months.Count) && (months[i].Year() == year.Year()))
                    {
                        perf[months[i].Month()] = months[i].mPerformance; // store month-over-month performance
                        i++;
                    }
                    double yearEndIndex = 0.0;
                    if (i < months.Count)
                        yearEndIndex = months[i].mFirstIndex; // is actually index on the 1st day of the next year
                    else
                        yearEndIndex = months[i - 1].mLastIndex;
                    perf[0] = (yearEndIndex - yearStartIndex) / yearStartIndex; // store year performance under index 0
                    table[year.Year()] = perf;
                }
            }
            catch
            {
            }

            return table;
        }

        public DateTime getPricingDate(DateTime today)
        {
            return getPricingDate(this.xUseCloseAsLast, this.CloseType, this.xCloseDate, today);
        }

        public DateTime getPricingDate(bool _useCloseAsLast, int _closeType, DateTime _closeDate, DateTime _today)
        {
            DateTime pricingDate = new DateTime();
            pricingDate = _today.Date;

            if (_useCloseAsLast)
            {
                switch (_closeType)
                {
                    case (int)TypeManastCloseAsLast.TMINUS1:
                    case (int)TypeManastCloseAsLast.MOSTRECENT: // there is no unique "most recent date", since it be that some (Asian?) assets already have a closing, whereas the Europeans not yet -> return T-1
                        {
                            pricingDate = pricingDate.AddDays(-1.0);
                            pricingDate = GetMatchingBusinessDay(pricingDate, true, true);
                            break;
                        }
                    case (int)TypeManastCloseAsLast.CUSTOM:
                        {
                            foreach (SwapPriceRule r in AccountPriceRuleRows)
                            {
                                DateTime d = new DateTime();
                                TypeManastCloseRefDate refDate = (TypeManastCloseRefDate)r.RefDateId;

                                if (refDate == TypeManastCloseRefDate.eT)
                                    d = _today.Date;
                                else if (refDate == TypeManastCloseRefDate.eTMinus1)
                                    d = _today.Date.AddDays(-1.0);
                                else if (refDate == TypeManastCloseRefDate.eSpecificDate)
                                    d = r.SpecificDate;
                                else
                                    d = _today.Date;

                                pricingDate = (d > pricingDate) ? d : pricingDate; // pricingDate = MAX(d, pricingDate)
                            }
                            pricingDate = GetMatchingBusinessDay(pricingDate, true, true);
                            break;
                        }
                    case (int)TypeManastCloseAsLast.DATE:
                        pricingDate = _closeDate.Date;
                        break;
                }
            }

            return pricingDate;
        }

        public bool IsABusinessDay(DateTime date)
        {
            DateTime adjustedDate = GetMatchingBusinessDay(date, true, true);
            return (adjustedDate.Date == date.Date);
        }


        public List<SwapEvent> GetEventsOfTypeForDate(SwapAccountPortfolio pr, TypeManastEvent eventType, DateTime date)
        {
            List<SwapEvent> eventList = new List<SwapEvent>();

            foreach (SwapEvent er in pr.EventRowsByPortfolioSource)
                if (((TypeManastEvent)er.EventType == eventType) && (er.ExecutionDate == date))
                    eventList.Add(er);

            return eventList;
        }

        public List<SwapEvent> GetEventsOfTypeSortedByID(SwapAccountPortfolio pr, List<TypeManastEvent> eventTypes)
        {
            List<SwapEvent> eventList = new List<SwapEvent>();

            foreach (SwapEvent er in pr.EventRowsByPortfolioSource)
                if (eventTypes.Contains((TypeManastEvent)er.EventType))
                    eventList.Add(er);
            eventList.Sort(
            delegate (SwapEvent er1, SwapEvent er2)
            {
                return Comparer<Int32>.Default.Compare(1, 1);
            }
            );

            return eventList;
        }

        public IList<SwapAccountReport> AccountReportRows { get; set; } = new List<SwapAccountReport>();
        public ObservableCollection<SwapAccountReport> m_AccountReportRowsObservable { get; set; } = new ObservableCollection<SwapAccountReport>();
        public ObservableCollection<SwapAccountReport> AccountReportRowsObservable
        {
            get
            {
                m_AccountReportRowsObservable.Clear();
                foreach (var row in AccountReportRows)
                {
                    m_AccountReportRowsObservable.Add(row);
                }
                return m_AccountReportRowsObservable;
            }
        }
        public IEnumerable<SwapAccountReport> GetAccountReportRows() { return AccountReportRows; }
        public IEnumerable<SwapAccountReport> GetAccountReportRows(int numbOfRecip)
        {
            return AccountReportRows.Where(r => (r.Recipient == numbOfRecip));
        }
        public int GetAccountReportRowsCount() { return AccountReportRows.Count(); }

        public void AddAccountReport(int numbOfRecip, string name, string displayName, bool includeInExcel, bool includeInMail, string value, int decimals)
        {
            AccountReportRows.Add(new SwapAccountReport(this.DbName, this.Id, numbOfRecip, name, displayName, includeInExcel, includeInMail, value, decimals));
            NotifyPropertyChanged(() => AccountReportRows);
            NotifyPropertyChanged(() => AccountReportRowsObservable);
            this.SetToDirty();
        }
        public void ClearAccountReport(int numbOfRecip)
        {
            foreach (var reportRow in AccountReportRows.Where(row => row.Recipient == numbOfRecip))
            {
                AccountReportRows.Remove(reportRow);
            }
            NotifyPropertyChanged(() => AccountReportRows);
            NotifyPropertyChanged(() => AccountReportRowsObservable);
        }

        public SwapAccountHistory AddAccountHistoryRow(string type, System.DateTime date, string description, double indexFactor, double indexValue, double indexSnapShotValue, System.DateTime indexSnapShotSaved, double highWaterMark, double? collateralRatio, double? ratio)
        {
            SetToDirty();
            BeginEdit();
            SwapAccountHistory rowAccountHistoryRow = new SwapAccountHistory();
            rowAccountHistoryRow.Type = type;
            rowAccountHistoryRow.xDate = date;
            rowAccountHistoryRow.Description = description;
            rowAccountHistoryRow.IndexFactor = indexFactor;
            rowAccountHistoryRow.IndexValue = indexValue;
            rowAccountHistoryRow.IndexSnapshotValue = indexSnapShotValue;
            rowAccountHistoryRow.IndexSnapShotSaved = indexSnapShotSaved;
            rowAccountHistoryRow.HighWaterMark = highWaterMark;
            rowAccountHistoryRow.RowVersion = 0;
            rowAccountHistoryRow.CollateralRatio = collateralRatio;
            rowAccountHistoryRow.Ratio = ratio;

            rowAccountHistoryRow.AccountId = this.Id;
            rowAccountHistoryRow.DbName = this.DbName;
            if (!collateralRatio.HasValue)
                rowAccountHistoryRow.SetCollateralRatioNull();
            if (!ratio.HasValue)
                rowAccountHistoryRow.SetRatioNull();
            HistoryRows.Add(rowAccountHistoryRow);
            EndEdit();
            NotifyPropertyChanged(() => HistoryRows);
            return rowAccountHistoryRow;
        }

        public List<SwapStopLossRow> StopLossRows { get; set; } = new List<SwapStopLossRow>();
        public List<SwapStopLossRow> GetAccountStopLossRows() { return StopLossRows; }
        public ObservableCollection<SwapStopLossRow> m_StopLossRowsObservable { get; } = new ObservableCollection<SwapStopLossRow>();
        public ObservableCollection<SwapStopLossRow> StopLossRowsObservable
        {
            get
            {
                m_StopLossRowsObservable.Clear();
                foreach (var row in StopLossRows)
                {
                    m_StopLossRowsObservable.Add(row);
                }
                return m_StopLossRowsObservable;
            }
        }
        public void AddStopLoss(SwapStopLossKindRow kind, double pct, DateTime date)
        {
            StopLossRows.Add(new SwapStopLossRow(this.DbName, this.Id, kind, pct, date));
            NotifyPropertyChanged(() => StopLossRows);
            NotifyPropertyChanged(() => StopLossRowsObservable);
            this.SetToDirty();
        }

        public IList<SwapAccountHolidayCalendar> HolidayCalendarRows { get; set; } = new List<SwapAccountHolidayCalendar>();
        public ObservableCollection<SwapAccountHolidayCalendar> m_HolidayCalendarRowsObservable { get; } = new ObservableCollection<SwapAccountHolidayCalendar>();
        public ObservableCollection<SwapAccountHolidayCalendar> HolidayCalendarRowsObservable
        {
            get
            {
                m_HolidayCalendarRowsObservable.Clear();
                foreach (var row in HolidayCalendarRows)
                {
                    m_HolidayCalendarRowsObservable.Add(row);
                }
                return m_HolidayCalendarRowsObservable;
            }
        }
        public IList<SwapAccountHolidayCalendar> GetHolidayCalendarRows()
        {
            return HolidayCalendarRows;
        }
        public void AddNewCalendar(string name)
        {
            SwapAccountHolidayCalendar newCalendar = new SwapAccountHolidayCalendar();
            newCalendar.AccountId = this.Id;
            newCalendar.DbName = this.DbName;
            newCalendar.CalendarName = name;
            HolidayCalendarRows.Add(newCalendar);
            NotifyPropertyChanged(() => HolidayCalendarRows);
            NotifyPropertyChanged(() => m_HolidayCalendarRowsObservable);
            NotifyPropertyChanged(() => HolidayCalendarRowsObservable);
        }

        public void DeleteCalendar(SwapAccountHolidayCalendar selected)
        {
            HolidayCalendarRows.Remove(selected);
            NotifyPropertyChanged(() => HolidayCalendarRows);
            NotifyPropertyChanged(() => m_HolidayCalendarRowsObservable);
            NotifyPropertyChanged(() => HolidayCalendarRowsObservable);
        }

        public DateTime GetMatchingBusinessDay(DateTime date, bool preceding, bool ensureWeekday)
        {
            List<string> calendarNames = new List<string>();
            foreach (SwapAccountHolidayCalendar hol in HolidayCalendarRows)
                calendarNames.Add(hol.CalendarName);
            return date;
        }

        public List<SwapPriceRule> AccountPriceRuleRows = new List<SwapPriceRule>();
        public List<SwapPriceRule> GetSortedAccountPriceRuleRows()
        {
            List<SwapPriceRule> list = null;
            list.Sort(delegate (SwapPriceRule t1, SwapPriceRule t2) { return Comparer<Int32>.Default.Compare(t1.Priority, t2.Priority); });
            return list;

        }

        public IEnumerable<SwapOrder> OrderRows => null;
        public int OrderRowsCount => 1;
        public ObservableCollection<SwapOrder> m_OrderRowsObservable { get; } = new ObservableCollection<SwapOrder>();
        public ObservableCollection<SwapOrder> OrderRowsObservable
        {
            get
            {
                m_OrderRowsObservable.Clear();
                foreach (var row in OrderRows)
                {
                    m_OrderRowsObservable.Add(row);
                }
                return m_OrderRowsObservable;
            }
        }

        public List<SwapOrder> GetSortedOrderRows(bool sortByTradeDate, bool asc)
        {
            List<SwapOrder> orderList = OrderRows.ToList();
            orderList.Sort(
                    delegate (SwapOrder or1, SwapOrder or2)
                    {
                        if (sortByTradeDate)
                            return Comparer<DateTime>.Default.Compare(or1.TradeDate, or2.TradeDate);
                        else
                            return Comparer<DateTime>.Default.Compare((!or1.DateExecuted.HasValue ? DateTime.MinValue : or1.DateExecuted.Value), (!or2.DateExecuted.HasValue ? DateTime.MinValue : or2.DateExecuted.Value));
                    }
                );

            // reverse the orders in an ascendig (aufsteigend) manner
            if (!asc)
                orderList.Reverse();

            return orderList;
        }
        public List<OrderSnapshotForNominalOfStock> GetOrderSnapshotRowsForNominalOfStock(bool sortByTradeDate, bool asc)
        {
            List<OrderSnapshotForNominalOfStock> res = new List<OrderSnapshotForNominalOfStock>();
            foreach (var order in OrderRows)
            {
                List<TradeSnapshotRow> tradeRows = new List<TradeSnapshotRow>();
                foreach (var trade in order.TradeRows)
                {
                    tradeRows.Add(new TradeSnapshotRow(1, trade.Nominal, trade.Price, trade.Accrued, trade.FXRate, trade.Fee));
                }
                res.Add(new OrderSnapshotForNominalOfStock(order.TradeDate, order.DateExecuted, tradeRows));
            }
            res.Sort(
                    delegate (OrderSnapshotForNominalOfStock or1, OrderSnapshotForNominalOfStock or2)
                    {
                        if (sortByTradeDate)
                            return Comparer<DateTime>.Default.Compare(or1.TradeDate, or2.TradeDate);
                        else
                            return Comparer<DateTime>.Default.Compare((!or1.DateExecuted.HasValue ? DateTime.MinValue : or1.DateExecuted.Value), (!or2.DateExecuted.HasValue ? DateTime.MinValue : or2.DateExecuted.Value));
                    }
                );

            // reverse the orders in an ascendig (aufsteigend) manner
            if (!asc)
                res.Reverse();
            return res;
        }

        public SwapEvent[] GetSortedEventRows(bool asc)
        {
            List<SwapEvent> eventList = new List<SwapEvent>();

            foreach (SwapAccountPortfolio pr in AccountPortfolioRows)
            {
                eventList.AddRange(pr.EventRowsByPortfolioSource);
            }
            SwapEvent[] ers = new SwapEvent[eventList.Count];
            eventList.Sort(
            delegate (SwapEvent er1, SwapEvent er2)
            {
                return Comparer<DateTime>.Default.Compare(er1.ExecutionDate, er2.ExecutionDate);
            }
            );

            // reverse the orders in an ascendig (aufsteigend) manner
            if (!asc)
                eventList.Reverse();

            return ers = eventList.ToArray();
        }

        public SwapAccountPortfolio GetSwapAccountPortfolioByInstrumentId(int instrumentId) => null;

        public double Fee { get; set; }
        public double? Fee2 { get; set; }
        public bool IsFee2Null()
        {
            return !Fee2.HasValue;
        }
        public double xFee2
        {
            get
            {
                if (IsFee2Null()) return 0;
                else return Fee2.Value;
            }
        }
        public double? Fee3 { get; set; }
        public bool IsFee3Null()
        {
            return !Fee3.HasValue;
        }
        public double xFee3
        {
            get
            {
                if (IsFee3Null()) return 0;
                else return Fee3.Value;
            }
        }

        public String Disclaimer { get; set; }
        public bool IsDisclaimerNull()
        {
            return Disclaimer == null;
        }
        public String xDisclaimer
        {
            get
            {
                if (IsDisclaimerNull()) return string.Empty;
                else return Disclaimer;
            }
        }

        public String DisclaimerExecution { get; set; }
        public bool IsDisclaimerExecutionNull()
        {
            return DisclaimerExecution == null;
        }
        public String xDisclaimerExecution
        {
            get
            {
                if (IsDisclaimerExecutionNull()) return string.Empty;
                else return DisclaimerExecution;
            }
        }

        public bool? EmailMustAttachXLS { get; set; }
        public bool xEmailMustAttachXLS
        {
            get { return IsEmailMustAttachXLSNull() ? false : EmailMustAttachXLS.Value; }
            set
            {
                EmailMustAttachXLS = value;
                if (!value)
                {
                    xEmailMustIncludePerformance = false;
                    xEmailMustIncludeUniverse = false;


                }
                NotifyPropertyChanged(() => EmailMustAttachXLS);
                NotifyPropertyChanged(() => xEmailMustAttachXLS);
            }
        }

        public bool IsEmailMustAttachXLSNull()
        {
            return !EmailMustAttachXLS.HasValue;
        }

        public bool? EmailMustAttachPDF { get; set; }
        public bool xEmailMustAttachPDF
        {
            get { return IsEmailMustAttachPDFNull() ? false : EmailMustAttachPDF.Value; }
            set
            {
                EmailMustAttachPDF = value;
                NotifyPropertyChanged(() => EmailMustAttachPDF);
                NotifyPropertyChanged(() => xEmailMustAttachPDF);
            }
        }

        public bool IsEmailMustAttachPDFNull()
        {
            return !EmailMustAttachPDF.HasValue;
        }

        public bool? EmailMustIncludePerformance { get; set; }
        public bool xEmailMustIncludePerformance
        {
            get { return IsEmailMustIncludePerformanceNull() ? false : EmailMustIncludePerformance.Value; }
            set
            {
                EmailMustIncludePerformance = value;
                NotifyPropertyChanged(() => EmailMustIncludePerformance);
                NotifyPropertyChanged(() => xEmailMustIncludePerformance);
            }
        }

        public bool IsEmailMustIncludePerformanceNull()
        {
            return !EmailMustIncludePerformance.HasValue;
        }

        public bool? EmailMustIncludeUniverse { get; set; }
        public bool xEmailMustIncludeUniverse
        {
            get { return IsEmailMustIncludeUniverseNull() ? false : EmailMustIncludeUniverse.Value; }
            set
            {
                EmailMustIncludeUniverse = value;
                NotifyPropertyChanged(() => EmailMustIncludeUniverse);
                NotifyPropertyChanged(() => xEmailMustIncludeUniverse);
            }
        }

        public bool IsEmailMustIncludeUniverseNull()
        {
            return !EmailMustIncludeUniverse.HasValue;
        }

        public string EmailRecipients { get; set; } // valuation report 1
        public string EmailRecipients2 { get; set; } // valuation report 2
        public string EmailRecipients3 { get; set; } // execution report
        public string EmailRecipients6 { get; set; } // pre-advice report
        public string EmailColumns { get; set; }
        public string EmailColumns2 { get; set; }
        public string EmailColumns3 { get; set; }
        public string EmailColumns6 { get; set; }
        public bool? EmailRecipientsBcc { get; set; }
        public bool? EmailRecipientsBcc2 { get; set; }
        public bool? EmailRecipientsBcc3 { get; set; }
        public bool? EmailRecipientsBcc6 { get; set; }

        public bool xEmailRecipientsBcc
        {
            get
            {
                if (IsEmailRecipientsBccNull())
                    return false;
                else
                    return EmailRecipientsBcc.Value;
            }
            set
            {
                EmailRecipientsBcc = value;
                NotifyPropertyChanged(() => EmailRecipientsBcc);
                NotifyPropertyChanged(() => xEmailRecipientsBcc);
            }
        }
        public bool xEmailRecipientsBcc2
        {
            get
            {
                if (IsEmailRecipientsBcc2Null())
                    return false;
                else
                    return EmailRecipientsBcc2.Value;
            }
            set
            {
                EmailRecipientsBcc2 = value;
                NotifyPropertyChanged(() => EmailRecipientsBcc2);
                NotifyPropertyChanged(() => xEmailRecipientsBcc2);
            }
        }

        public bool xEmailRecipientsBcc3
        {
            get
            {
                if (IsEmailRecipientsBcc3Null())
                    return false;
                else
                    return EmailRecipientsBcc3.Value;
            }
            set
            {
                EmailRecipientsBcc3 = value;
                NotifyPropertyChanged(() => EmailRecipientsBcc3);
                NotifyPropertyChanged(() => xEmailRecipientsBcc3);
            }
        }
        public bool xEmailRecipientsBcc6
        {
            get
            {
                if (IsEmailRecipientsBcc6Null())
                    return false;
                else
                    return EmailRecipientsBcc6.Value;
            }
            set
            {
                EmailRecipientsBcc6 = value;
                NotifyPropertyChanged(() => EmailRecipientsBcc6);
                NotifyPropertyChanged(() => xEmailRecipientsBcc6);
            }
        }

        public bool IsEmailRecipientsBccNull()
        {
            return !EmailRecipientsBcc.HasValue;
        }
        public bool IsEmailRecipientsBcc2Null()
        {
            return !EmailRecipientsBcc2.HasValue;
        }
        public bool IsEmailRecipientsBcc3Null()
        {
            return !EmailRecipientsBcc3.HasValue;
        }
        public bool IsEmailRecipientsBcc6Null()
        {
            return !EmailRecipientsBcc6.HasValue;
        }

        public string[] getEmailColumns(int numbRecip)
        {
            char c = ';';
            string s = "";
            if (numbRecip == 1)
                s = this.EmailColumns;
            else if (numbRecip == 2)
                s = this.EmailColumns2;
            else if (numbRecip == 3)
                s = this.EmailColumns3;
            else if (numbRecip == 6)
                s = this.EmailColumns6;
            if (s == null || s == string.Empty) return new string[0];
            string[] arr = s.Split(c);
            return arr;
        }
        public void setEmailColumns(string s, int numbRecip)
        {
            switch (numbRecip)
            {
                case 1:
                    this.EmailColumns = s;
                    SetToDirty();
                    break;
                case 2:
                    this.EmailColumns2 = s;
                    SetToDirty();
                    break;
                case 3:
                    this.EmailColumns3 = s;
                    SetToDirty();
                    break;
                case 6:
                    this.EmailColumns6 = s;
                    SetToDirty();
                    break;
                default:
                    break;
            }
        }

        public bool? MustReportAutomatically1 { get; set; }
        public bool? MustReportAutomatically2 { get; set; }
        public bool? MustReportAutomatically4 { get; set; }
        public bool? MustReportAutomatically5 { get; set; }
        public bool xMustReportAutomatically1
        {
            get { return IsMustReportAutomatically1Null() ? false : MustReportAutomatically1.Value; }
            set
            {
                MustReportAutomatically1 = value;
                NotifyPropertyChanged(() => MustReportAutomatically1);
                NotifyPropertyChanged(() => xMustReportAutomatically1);
            }
        }
        public bool xMustReportAutomatically2
        {
            get { return IsMustReportAutomatically2Null() ? false : MustReportAutomatically2.Value; }
            set
            {
                MustReportAutomatically2 = value;
                NotifyPropertyChanged(() => MustReportAutomatically2);
                NotifyPropertyChanged(() => xMustReportAutomatically2);
            }
        }
        public bool xMustReportAutomatically4
        {
            get { return IsMustReportAutomatically4Null() ? false : MustReportAutomatically4.Value; }
            set
            {
                MustReportAutomatically4 = value;
                NotifyPropertyChanged(() => MustReportAutomatically4);
                NotifyPropertyChanged(() => xMustReportAutomatically4);
            }
        }
        public bool xMustReportAutomatically5
        {
            get { return IsMustReportAutomatically5Null() ? false : MustReportAutomatically5.Value; }
            set
            {
                MustReportAutomatically5 = value;
                NotifyPropertyChanged(() => MustReportAutomatically5);
                NotifyPropertyChanged(() => xMustReportAutomatically5);
            }
        }
        public bool IsMustReportAutomatically1Null()
        {
            return !MustReportAutomatically1.HasValue;
        }
        public bool IsMustReportAutomatically2Null()
        {
            return !MustReportAutomatically2.HasValue;
        }
        public bool IsMustReportAutomatically4Null()
        {
            return !MustReportAutomatically4.HasValue;
        }
        public bool IsMustReportAutomatically5Null()
        {
            return !MustReportAutomatically5.HasValue;
        }

        public IList<SwapManagementFee1> ManagementFee1Rows { get; set; } = new List<SwapManagementFee1>();
        public IList<SwapManagementFee2> ManagementFee2Rows { get; set; } = new List<SwapManagementFee2>();
        public IList<SwapManagementFee3> ManagementFee3Rows { get; set; } = new List<SwapManagementFee3>();
        public ObservableCollection<SwapManagementFee1> m_ManagementFee1RowsObservable { get; } = new ObservableCollection<SwapManagementFee1>();
        public ObservableCollection<SwapManagementFee2> m_ManagementFee2RowsObservable { get; } = new ObservableCollection<SwapManagementFee2>();
        public ObservableCollection<SwapManagementFee3> m_ManagementFee3RowsObservable { get; } = new ObservableCollection<SwapManagementFee3>();
        public ObservableCollection<SwapManagementFee1> ManagementFee1RowsObservable
        {
            get
            {
                m_ManagementFee1RowsObservable.Clear();
                foreach (var row in ManagementFee1Rows) m_ManagementFee1RowsObservable.Add(row);
                return m_ManagementFee1RowsObservable;
            }
        }
        public ObservableCollection<SwapManagementFee2> ManagementFee2RowsObservable
        {
            get
            {
                m_ManagementFee2RowsObservable.Clear();
                foreach (var row in ManagementFee2Rows) m_ManagementFee2RowsObservable.Add(row);
                return m_ManagementFee2RowsObservable;
            }
        }
        public ObservableCollection<SwapManagementFee3> ManagementFee3RowsObservable
        {
            get
            {
                m_ManagementFee3RowsObservable.Clear();
                foreach (var row in ManagementFee3Rows) m_ManagementFee3RowsObservable.Add(row);
                return m_ManagementFee3RowsObservable;
            }
        }
        public IList<SwapManagementFee1> GetManagementFee1Rows()
        {
            return ManagementFee1Rows;
        }
        public IList<SwapManagementFee2> GetManagementFee2Rows()
        {
            return ManagementFee2Rows;
        }
        public IList<SwapManagementFee3> GetManagementFee3Rows()
        {
            return ManagementFee3Rows;
        }

        public void AddManagementFee1(double floor, double feePct)
        {
            this.SetToDirty();
            ManagementFee1Rows.Add(new SwapManagementFee1(this.DbName, this.Id, floor, feePct));
            NotifyPropertyChanged(nameof(ManagementFee1Rows));
            NotifyPropertyChanged(nameof(ManagementFee1RowsObservable));
        }
        public void AddManagementFee2(double floor, double feePct)
        {
            this.SetToDirty();
            ManagementFee2Rows.Add(new SwapManagementFee2(this.DbName, this.Id, floor, feePct));
            NotifyPropertyChanged(nameof(ManagementFee2Rows));
            NotifyPropertyChanged(nameof(ManagementFee2RowsObservable));

        }
        public void AddManagementFee3(double floor, double feePct)
        {
            this.SetToDirty();
            ManagementFee3Rows.Add(new SwapManagementFee3(this.DbName, this.Id, floor, feePct));
            NotifyPropertyChanged(nameof(ManagementFee3Rows));
            NotifyPropertyChanged(nameof(ManagementFee3RowsObservable));
        }

        public IList<SwapSophisPortfolioRow> SophisPortfolioRows { get; set; } = new List<SwapSophisPortfolioRow>();
        public IList<SwapSophisPortfolioRow> GetSophisPortfolioRows()
        {
            return SophisPortfolioRows;
        }

        public ObservableCollection<SwapSophisPortfolioRow> m_SophisPortfolioRowsObservable { get; } = new ObservableCollection<SwapSophisPortfolioRow>();
        public ObservableCollection<SwapSophisPortfolioRow> SophisPortfolioRowsObservable
        {
            get
            {
                m_SophisPortfolioRowsObservable.Clear();
                foreach (var row in SophisPortfolioRows)
                {
                    m_SophisPortfolioRowsObservable.Add(row);
                }
                return m_SophisPortfolioRowsObservable;
            }
        }
        public void DeleteSophisPortfolio(SwapSophisPortfolioRow selected)
        {
            SophisPortfolioRows.Remove(selected);
            NotifyPropertyChanged(() => SophisPortfolioRows);
        }

        public IList<SwapUploadSPIRow> UploadSPIRows { get; set; } = new List<SwapUploadSPIRow>();
        public IList<SwapUploadSPIRow> GetUploadSPIRows()
        {
            return UploadSPIRows;
        }
        public ObservableCollection<SwapUploadSPIRow> m_UploadSPIRowsObservable { get; set; } = new ObservableCollection<SwapUploadSPIRow>();
        public ObservableCollection<SwapUploadSPIRow> UploadSPIRowsObservable
        {
            get
            {
                m_UploadSPIRowsObservable.Clear();
                foreach (var row in UploadSPIRows)
                {
                    m_UploadSPIRowsObservable.Add(row);
                }
                return m_UploadSPIRowsObservable;
            }
        }

        public IList<SwapManualFX> ManualFXRows { get; set; } = new List<SwapManualFX>();
        public ObservableCollection<SwapManualFX> m_ManualFXRowsObservable { get; } = new ObservableCollection<SwapManualFX>();
        public ObservableCollection<SwapManualFX> ManualFXRowsObservable
        {
            get
            {
                m_ManualFXRowsObservable.Clear();
                foreach (var row in ManualFXRows)
                {
                    m_ManualFXRowsObservable.Add(row);
                }
                return m_ManualFXRowsObservable;
            }
        }
        public bool? StopUpdating { get; set; }
        public bool xStopUpdating
        {
            get
            {
                if (IsStopUpdatingNull()) return false;
                else return StopUpdating.Value;
            }
        }

        private bool m_contribute = false;
        public bool xContribute
        {
            get
            {
                return m_contribute;
            }
            set
            {
                m_contribute = value;
            }
        }

        public string ContributeID { get; set; }
        public string xContributeID
        {
            get { if (IsContributeIDNull()) return string.Empty; else return ContributeID; }
        }
        public bool IsContributeIDNull() { return ContributeID != null; }

        public bool IsStopUpdatingNull() { return !StopUpdating.HasValue; }
        public DateTime? StopUpdatingBegin { get; set; }
        public bool IsStopUpdatingBeginNull() { return !StopUpdatingBegin.HasValue; }
        public DateTime? StopUpdatingStop { get; set; }
        public bool IsStopUpdatingStopNull() { return !StopUpdatingStop.HasValue; }

        public double JumpBarrier { get; set; }

        public bool? ContributeEOD { get; set; }
        public bool xContributeEOD
        {
            get
            {
                if (IsContributeEODNull()) return false; else return ContributeEOD.Value;
            }
        }
        public bool IsContributeEODNull() { return !ContributeEOD.HasValue; }

        public int ContributeDecimals { get; set; }
        public string ContributeInterval { get; set; }

        public bool? m_CrossCheckSophisSPI { get; set; }
        public bool CrossCheckSophisSPI
        {
            get
            {
                if (IsCrossCheckSophisSPINull()) return false;
                else return m_CrossCheckSophisSPI.Value;
            }
            set
            {
                m_CrossCheckSophisSPI = value;
                NotifyPropertyChanged(nameof(CrossCheckSophisSPI));
                NotifyPropertyChanged(() => CrossCheckSophisSPI);
            }
        }
        public bool IsCrossCheckSophisSPINull() { return !m_CrossCheckSophisSPI.HasValue; }
        public bool? m_UploadSPIAutomatic { get; set; }
        public bool UploadSPIAutomatic
        {
            get
            {
                if (IsUploadSPIAutomaticNull()) return false;
                else return m_UploadSPIAutomatic.Value;
            }
            set
            {
                m_UploadSPIAutomatic = value;
                NotifyPropertyChanged(nameof(UploadSPIAutomatic));
                NotifyPropertyChanged(() => UploadSPIAutomatic);
            }
        }
        public bool IsUploadSPIAutomaticNull() { return !m_UploadSPIAutomatic.HasValue; }

        public int? m_SPIRounding { get; set; }
        public int SPIRounding
        {
            get
            {
                if (IsSPIRoundingNull()) return 0;
                else return m_SPIRounding.Value;
            }
            set
            {
                m_SPIRounding = value;
                NotifyPropertyChanged(() => SPIRounding);
            }
        }
        public bool IsSPIRoundingNull() { return !m_SPIRounding.HasValue; }

        public int? m_ApplyAlertToColumn { get; set; }
        public int ApplyAlertToColumn
        {
            get
            {
                if (IsApplyAlertToColumnNull()) return 0;
                else return m_ApplyAlertToColumn.Value;
            }
            set
            {
                m_ApplyAlertToColumn = value;
                NotifyPropertyChanged(() => m_ApplyAlertToColumn);
                NotifyPropertyChanged(() => ApplyAlertToColumn);
            }
        }
        public bool IsApplyAlertToColumnNull() { return !m_ApplyAlertToColumn.HasValue; }

        public TypeApplyAlertToColumn ApplyAlertToColumnEnum
        {
            get => (TypeApplyAlertToColumn)ApplyAlertToColumn;
            set
            {
                ApplyAlertToColumn = (int)value;
                NotifyPropertyChanged(nameof(ApplyAlertToColumnEnum));
                NotifyPropertyChanged(nameof(ApplyAlertToColumn));
            }
        }

        private DateTime? m_SPIUploaded { get; set; }
        public DateTime? SPIUploaded
        {
            get => m_SPIUploaded;
            set
            {
                m_SPIUploaded = value;
                NotifyPropertyChanged(() => SPIUploaded);
                NotifyPropertyChanged(() => SPIUploadedString);
            }
        }
        public bool IsSPIUploadedNull()
        {
            return !SPIUploaded.HasValue;
        }

        public DateTime? UploadSPITime { get; set; }

        public string SPIUploadedString
        {
            get
            {
                if (SPIUploaded.HasValue)
                {
                    return "Last uploaded: " + SPIUploaded.Value.ToString("dd/MM/yyyy HH:mm");
                }
                else
                {
                    return "Last uploaded: never";
                }
            }
        }


        // Account properties

        private double m_marketValue = 0.0;
        private double m_marketValueRaw = 0.0;
        private double m_marketValueCollateral = 0.0;
        private double m_indexValue = 0.0;
        public double m_lastContributedIndexValue = 0.0;
        private double m_indexValueClosed = 0.0;
        private double m_weightedBidAskSpread = 0.0;
        private double m_volume;
        public DateTime xLastContribute = DateTime.MinValue;
        private double m_cash_weight;
        private double m_future_cash_weight;
        private double m_future_weight;
        private double m_bond_weight;
        private double m_stock_weight;
        private double m_fund_weight;
        private double m_etf_weight;
        private double m_certificate_weight;

        private bool m_isIndexComplete = false;
        private bool m_isIndexCloseComplete = false;

        private int m_sophisEventDifference = 0;
        private int m_sophisPositionDifference = 0;
        private double m_sophisCertificateDifference = double.NaN;
        private double m_leverageFactor = 0.0;
        public double xLeverageFactor { get { return m_leverageFactor; } }

        public double xCashWeight { get { return m_cash_weight; } }
        public double xFutureCashWeight { get { return m_future_cash_weight; } }
        public double xFutureWeight { get { return m_future_weight; } }
        public double xBondWeight { get { return m_bond_weight; } }
        public double xStockWeight { get { return m_stock_weight; } }
        public double xFundWeight { get { return m_fund_weight; } }
        public double xETFWeight { get { return m_etf_weight; } }
        public double xCertificateWeight { get { return m_certificate_weight; } }


        public double xCertificatesValue { get { return (IndexFactorDouble == 0.0) ? 0.0 : (InitialIndexValueDouble * 1.0) / IndexFactorDouble; } }
        public double xCertificates { get { return Math.Round((IndexFactorDouble == 0.0) ? (0.0) : (1.0 / IndexFactorDouble), 4); } } // #26823
        public double xCertificatesCalculated { get => 1; }
        public int xSophisEventDifference { get { return m_sophisEventDifference; } set { m_sophisEventDifference = value; } }
        public int xSophisPositionDifference { get { return m_sophisPositionDifference; } set { m_sophisPositionDifference = value; } }
        public double xSophisCertificateDifference { get { return m_sophisCertificateDifference; } set { m_sophisCertificateDifference = value; } }


        private double m_5_10_40 = 0.0;
        public double x5_10_40 { get { return m_5_10_40; } }

        private bool? m_askToBookBasket { get; set; }
        private bool? m_askToUploadSPI { get; set; }
        private bool? m_askToExecuteOrder { get; set; }
        private bool? m_bookInternalTrades { get; set; }
        private bool? m_bookOppositeQuantity { get; set; }
        
        public bool m_opusEnabled = false;
        public bool OpusEnabled
        {
            get { return m_opusEnabled; }
            set
            {
                NotifyPropertyChangedDirty(ref m_opusEnabled, value, () => OpusEnabled);
            }
        }

        private string m_opusAssetCompositionId;
        public string OpusAssetCompositionId
        {
            get
            {
                if (!OpusEnabled) return string.Empty; else return m_opusAssetCompositionId;
            }
            set
            {
                NotifyPropertyChangedDirty(ref m_opusAssetCompositionId, value, () => OpusAssetCompositionId);
            }
        }

        public bool AskToBookBasket
        {
            get
            {
                if (IsAskToBookBasketNull()) return false;
                else return m_askToBookBasket.Value;
            }
            set
            {
                if (m_askToBookBasket != value)
                {
                    m_askToBookBasket = value;
                    NotifyPropertyChanged(() => AskToBookBasket);
                }
            }
        }
        public bool IsAskToBookBasketNull() { return !m_askToBookBasket.HasValue; }
        public bool AskToUploadSPI
        {
            get
            {
                if (IsAskToUploadSPINull()) return false;
                else return m_askToUploadSPI.Value;
            }
            set
            {
                if (m_askToUploadSPI != value)
                {
                    m_askToUploadSPI = value;
                    NotifyPropertyChanged(() => AskToUploadSPI);
                }
            }
        }
        public bool IsAskToUploadSPINull() { return !m_askToUploadSPI.HasValue; }

        public bool AskToExecuteOrder
        {
            get
            {
                if (IsAskToExecuteOrderNull()) return false;
                else return m_askToExecuteOrder.Value;
            }
            set
            {
                if (m_askToExecuteOrder != value)
                {
                    m_askToExecuteOrder = value;
                    NotifyPropertyChanged(() => AskToExecuteOrder);
                }
            }
        }
        public bool IsAskToExecuteOrderNull() { return !m_askToExecuteOrder.HasValue; }

        public bool BookInternalTrades
        {
            get
            {
                if (IsBookInternalTradesNull()) return false;
                else return m_bookInternalTrades.Value;
            }
            set
            {
                if (m_bookInternalTrades != value)
                {
                    m_bookInternalTrades = value;
                    NotifyPropertyChanged(() => BookInternalTrades);
                }
            }
        }
        public bool IsBookInternalTradesNull() { return !m_bookInternalTrades.HasValue; }

        public bool BookOppositeQuantity
        {
            get
            {
                if (IsBookOppositeQuantityNull()) return false;
                else return m_bookOppositeQuantity.Value;
            }
            set
            {
                if (m_bookOppositeQuantity != value)
                {
                    m_bookOppositeQuantity = value;
                    NotifyPropertyChanged(() => BookOppositeQuantity);
                }
            }
        }
        public bool IsBookOppositeQuantityNull() { return !m_bookOppositeQuantity.HasValue; }

        private int? m_SwapMiFID { get; set; }
        public int SwapMiFID
        {
            get
            {
                if (IsSwapMiFIDNull()) return 0;
                else return m_SwapMiFID.Value;
            }
            set
            {
                if (m_SwapMiFID != value)
                {
                    m_SwapMiFID = value;
                    NotifyPropertyChanged(() => SwapMiFID);
                }
            }
        }
        public bool IsSwapMiFIDNull() { return !m_SwapMiFID.HasValue; }

        public string SophisCounterparty { get; set; }
        public string SophisProcessCode { get; set; }
        public string SophisBroker { get; set; }

        public TypeManastContributeMode xContributeMode { get { return (TypeManastContributeMode)ContributeMode; } }
        public TypeManastOrderReportStyle xOrderReportStyle { get { return (TypeManastOrderReportStyle)OrderReportStyle; } }

        public bool OrderReportFixedLayout
        {
            get
            {
                return xOrderReportStyle == TypeManastOrderReportStyle.FixedLayout;
            }
            set
            {
                OrderReportStyle = value ? (int)TypeManastOrderReportStyle.FixedLayout : (int)TypeManastOrderReportStyle.FlexibleLayout;
                NotifyPropertyChanged(() => OrderReportStyle);
                NotifyPropertyChanged(() => xOrderReportStyle);
                NotifyPropertyChanged(() => OrderReportFixedLayout);
                NotifyPropertyChanged(() => OrderReportFlexibleLayout);
            }
        }
        public bool OrderReportFlexibleLayout
        {
            get
            {
                return xOrderReportStyle == TypeManastOrderReportStyle.FlexibleLayout;
            }
            set
            {
                OrderReportStyle = value ? (int)TypeManastOrderReportStyle.FlexibleLayout : (int)TypeManastOrderReportStyle.FixedLayout;
                NotifyPropertyChanged(() => OrderReportStyle);
                NotifyPropertyChanged(() => xOrderReportStyle);
                NotifyPropertyChanged(() => OrderReportFixedLayout);
                NotifyPropertyChanged(() => OrderReportFlexibleLayout);
            }
        }
        public TypeManastIndexSnapshotMode xIndexSnapshotMode
        {
            get { return (TypeManastIndexSnapshotMode)IndexSnapshotMode; }
            set
            {
                IndexSnapshotMode = (int)value;
                NotifyPropertyChanged(nameof(xIndexSnapshotMode));
                NotifyPropertyChanged(nameof(IndexSnapshotMode));
            }
        }
        public TypeAccruedUntilDate xAccruedUntilDate
        {
            get { return (TypeAccruedUntilDate)AccruedUntilDate; }
            set
            {
                AccruedUntilDate = (int)value;
                NotifyPropertyChanged(nameof(xAccruedUntilDate));
                NotifyPropertyChanged(nameof(AccruedUntilDate));
            }
        }

        private bool m_IsDirtyAccruedPremiumAmount = true;
        public bool IsDirtyAccruedPremiumAmount
        {
            get { return m_IsDirtyAccruedPremiumAmount; }
            set
            {
                if (m_IsDirtyAccruedPremiumAmount != value)
                {
                    m_IsDirtyAccruedPremiumAmount = value;
                    NotifyPropertyChanged(nameof(IsDirtyAccruedPremiumAmount));
                }
            }
        }

        private double m_AccruedPremiumAmount = 0.0; // cache

        public double xAccruedPremiumAmount
        {
            get
            {
                if (IsDirtyAccruedPremiumAmount == true)
                {
                    IsDirtyAccruedPremiumAmount = false;
                    m_AccruedPremiumAmount = 0.0;
                    string dummy = "";
                    GetAccruedPremiumAmount(out m_AccruedPremiumAmount, out dummy);
                }
                return m_AccruedPremiumAmount;
            }
        }

        public bool GetAccruedPremiumAmount(out double value, out string reason)
        {
            value = 0.0;
            reason = "";

            DateTime today = DateTime.Now.Date;
            SortedDictionary<DateTime, PremiumValue> values;
            CalculatePremiumValues(today, out values);

            foreach (KeyValuePair<DateTime, PremiumValue> v in values)
            {
                if (v.Value.Error != PremiumValue.ErrorCode.eNoError)
                {
                    reason = v.Value.xErrrorString;
                    return false;
                }
                value += v.Value.xTotalFee;
            }
            return true;
        }

        public bool CalculatePremiumValues(DateTime today, out SortedDictionary<DateTime, PremiumValue> values)
        {
            DateTime from, to;
            GetPremiumDateRange(today, out from, out to);
            return CalculatePremiumValues(from, to, out values);
        }

        public void GetPremiumDateRange(DateTime today, out DateTime from, out DateTime to)
        {
            GetPremiumDateRange(today, xAccruedUntilDate, out from, out to);
        }

        public void GetPremiumDateRange(DateTime today, TypeAccruedUntilDate untilDate, out DateTime from, out DateTime to)
        {
            switch (untilDate)
            {
                case TypeAccruedUntilDate.IncludingPricingDate:
                    to = getPricingDate(today);
                    break;
                case TypeAccruedUntilDate.ExcludingPricingDate:
                    to = getPricingDate(today);
                    to = to.AddDays(-1.0);
                    to = GetMatchingBusinessDay(to, true, true);
                    break;
                default: // IncludingToday
                    to = today;
                    break;
            }

            from = to;

            List<SwapAccountPremium> rates = GetSortedPremiumRates(true); // true : sorted ascendingly
            int r = 0;
            while ((r < rates.Count) && (rates[r].xDate.Value.Date <= today.Date))
                r++;
            if (r > 0)
                from = rates[r - 1].xDate.Value;
            else if (rates.Count > 0)
                from = rates[0].xDate.Value; // take date of very first row
        }

        public IList<SwapAccountPremium> PremiumRows { get; set; } = new List<SwapAccountPremium>();

        public List<SwapAccountPremium> GetSortedPremiumRates(bool asc)
        {
            IList<SwapAccountPremium> accountPremiumRows = PremiumRows;
            List<SwapAccountPremium> list = new List<SwapAccountPremium>();

            list.AddRange(accountPremiumRows);

            list.Sort(
                delegate (SwapAccountPremium p1, SwapAccountPremium p2)
                {
                    return Comparer<DateTime>.Default.Compare(p1.xDate.Value, p2.xDate.Value);
                }
            );

            if (!asc)
                list.Reverse();

            return list;
        }

        public bool CalculatePremiumValues(DateTime from, DateTime to, out SortedDictionary<DateTime, PremiumValue> values)
        {
            values = new SortedDictionary<DateTime, PremiumValue>();
            bool success = true;

            List<SwapAccountHistory> indexHistory = GetSortedAccountHistoryRows(true); // true : sorted ascendingly
            int h = 0;

            List<SwapAccountPremium> rates = GetSortedPremiumRates(true); // true : sorted ascendingly
            int r = 0;

            List<SwapAccountPremiumFee> fees = GetSortedPremiumFees(true); // true : sorted ascendingly
            int f = 0;

            for (DateTime d = from.Date; d.Date <= to.Date; d = d.AddDays(1))
            {
                PremiumValue v = new PremiumValue();
                v.NotionalCalc = TypeAccountPremiumNotionalCalc.None;
                v.DayCalc = TypeAccountPremiumDayCalc.Standard;
                v.Days = 1.0;
                v.Date = d;
                v.IndexValue = 0.0;
                v.IndexDate = d;
                v.Certificates = 0.0;
                v.SwapNotional = 0.0;
                v.Rate = 0.0;
                v.Basis = 0.0;
                v.AdditionalFee = 0.0;
                v.Error = PremiumValue.ErrorCode.eNoError;

                while ((r < rates.Count) && (rates[r].xDate.Value.Date <= d))
                    r++;
                if (r > 0)
                {
                    v.Rate = rates[r - 1].Rate.Value;
                    v.Basis = rates[r - 1].Basis.Value;
                    v.NotionalCalc = (TypeAccountPremiumNotionalCalc)rates[r - 1].NotionalCalc;
                    v.DayCalc = (TypeAccountPremiumDayCalc)rates[r - 1].DayCalc;
                }
                else
                {
                    v.Error = PremiumValue.ErrorCode.eMissingRate;
                    success = false;
                }

                if ((v.DayCalc == TypeAccountPremiumDayCalc.Forward) && (!IsABusinessDay(d)))
                    continue;

                v.Tomorrow = v.Date.AddDays(1);
                v.Tomorrow = GetMatchingBusinessDay(v.Tomorrow, false, true); // next business day

                if (v.DayCalc == TypeAccountPremiumDayCalc.Forward)
                    v.Days = (v.Tomorrow - v.Date).TotalDays; // typically 1, but can also be 3 in case d is Fri so d+1 is Mo so we count Fri+Sat+Sun
                else // Standard
                    v.Days = 1.0;

                while ((f < fees.Count) && (fees[f].xDate.Value.Date <= d))
                    f++;
                if ((f > 0) && (fees[f - 1].xDate.Value.Date == d))
                {
                    v.AdditionalFee = fees[f - 1].AdditionalFee.Value;
                }

                switch (v.NotionalCalc)
                {
                    case TypeAccountPremiumNotionalCalc.Certificates:
                        {
                            v.Certificates = this.xCertificates;
                            break;
                        }
                    case TypeAccountPremiumNotionalCalc.SwapNotional:
                        {
                            v.SwapNotional = this.SwapNotional;
                            break;
                        }
                    case TypeAccountPremiumNotionalCalc.Standard:
                        {
                            v.IndexDate = DateTime.MinValue;
                            if (xUseCloseAsLast)
                                v.IndexDate = v.Tomorrow; // if account uses close prices, the index value for d is only saved on d+1 AND is stored in the history on date d+1
                            else
                                v.IndexDate = d;

                            while ((h < indexHistory.Count) && (indexHistory[h].xDate.Date < v.IndexDate))
                                h++;
                            if ((h < indexHistory.Count) && (indexHistory[h].xDate.Date == v.IndexDate))
                            {
                                v.IndexValue = indexHistory[h].IndexValue;
                                v.Certificates = indexHistory[h].xCertificates;
                            }
                            else
                            {
                                v.Error = PremiumValue.ErrorCode.eMissingIndexValue;
                                success = false;
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                values[d] = v;
            }

            return success;
        }


        public IList<SwapAccountPremiumFee> PremiumFeeRows { get; set; } = new List<SwapAccountPremiumFee>();
        public List<SwapAccountPremiumFee> GetSortedPremiumFees(bool asc)
        {
            List<SwapAccountPremiumFee> list = new List<SwapAccountPremiumFee>();

            list.AddRange(PremiumFeeRows);

            list.Sort(
                delegate (SwapAccountPremiumFee p1, SwapAccountPremiumFee p2)
                {
                    return Comparer<DateTime>.Default.Compare(p1.xDate.Value, p2.xDate.Value);
                }
            );

            if (!asc)
                list.Reverse();

            return list;
        }

        public double xSwapUnits
        {
            get
            {
                if (SwapStrike != 0.0)
                    return (SwapNotional / SwapStrike);
                return 0.0;
            }
        }

        public double xFeeCalcOffset
        {
            get
            {
                if (xSwapUnits != 0.0)
                    return (xAccruedPremiumAmount / xSwapUnits);
                return 0.0;
            }
        }

        public double xSwapValue
        {
            get
            {
                if (SwapStrike != 0.0)
                    return (xIndexValue - xFeeCalcOffset - SwapStrike) / SwapStrike;
                return 0.0;
            }
        }

        public double xSwapValue2
        {
            get
            {
                if (SwapNotional != 0.0)
                    return (SwapNotional * xIndexValue + xAccruedPremiumAmount) / SwapNotional - 1;
                return 0.0;
            }
        }

        public double xLeverage
        {
            get
            {
                double accountMarketValue = (IndexFactorDouble != 0.0) ? (xIndexValue / IndexFactorDouble) : (0.0);
                double leverage = (accountMarketValue != 0.0) ? (xMarketValue / accountMarketValue) : (0.0);
                return leverage;
            }
        }

        public DateTime? m_SwapMaturity { get; set; }
        public DateTime? SwapMaturity
        {
            get => m_SwapMaturity; set
            {
                if (m_SwapMaturity != value)
                {
                    m_SwapMaturity = value;
                    NotifyPropertyChanged(nameof(SwapMaturity));
                }
            }
        }

        public bool IsSwapMaturityNull() { return !SwapMaturity.HasValue; }

        public int xDaysToSwapMaturity
        {
            get
            {
                if (!IsSwapMaturityNull())
                {
                    double d = (SwapMaturity.Value - DateTime.Now.Date).TotalDays;
                    int days = Convert.ToInt32(d);
                    return days;
                }
                return -1;
            }
        }
        public string xDaysToSwapMaturityString
        {
            get
            {
                int val = xDaysToSwapMaturity;
                return val < 0 ? string.Empty : val.ToString();
            }
        }

        public DateTime? CertificateMaturity { get; set; }
        public bool IsCertificateMaturityNull()
        {
            return !CertificateMaturity.HasValue;
        }

        public double xMtM
        {
            get
            {
                return (m_indexValue - SwapStrike) / IndexFactorDouble;
            }
        }

        public double xPerformanceValue
        {
            get
            {
                if (IndexFactorDouble == 0.0)
                    return 0.0;
                else
                    return (m_indexValue - InitialIndexValueDouble) / IndexFactorDouble;
            }
        }

        private bool m_IsDirtyRebalance = true;
        public bool IsDirtyRebalance
        {
            get { return m_IsDirtyRebalance; }
            set { m_IsDirtyRebalance = value; }
        }

        private void refreshDirtyRebalance()
        // - load dates from table UNI_INDEX_REBALANCING_DATE into "xRebalanceDates"
        // - calculate "xNextRebalance", "xDaysToNextRebalance"
        {
            IsDirtyRebalance = false;

            DateTime today = DateTime.Now.Date;

            m_RebalanceError = "";
            if (!loadRebalanceDates(ref m_RebalanceDates, out m_RebalanceError))
            {
                string msg = string.Format("{0} for '{1}'", m_RebalanceError, AccountName);
                // TODO aner
                //Logger.TraceMessage(Logger.ElogLevel.ERROR, msg);
            }

            // next (upcoming)
            m_NextRebalance = null;
            int i = 0;
            while ((i < m_RebalanceDates.Count) && (m_RebalanceDates[i] < today))
                i++;
            if (i < m_RebalanceDates.Count)
                m_NextRebalance = m_RebalanceDates[i];

            // days to next div
            m_DaysToNextRebalance = -1;
            if (m_NextRebalance != null)
            {
                m_DaysToNextRebalance = Convert.ToInt32((m_NextRebalance.Value.Date - today).TotalDays);
                if (m_DaysToNextRebalance == 0)
                {
                    string msg = string.Format("Index Rebalance today for '{0}'", AccountName);
                    // TODO aner
                    //Logger.TraceMessage(Logger.ElogLevel.WARNING, msg);
                }
            }
        }

        private string m_RebalanceError = string.Empty;
        public string xRebalanceError
        {
            get { return m_RebalanceError; }
            set { m_RebalanceError = value; }
        }


        private List<DateTime> m_RebalanceDates = new List<DateTime>();
        public List<DateTime> xRebalanceDates
        {
            get
            {
                if (IsDirtyRebalance == true)
                    refreshDirtyRebalance();
                return m_RebalanceDates;
            }
        }


        private DateTime? m_NextRebalance = null;
        public DateTime? xNextRebalance
        {
            get
            {
                if (IsDirtyRebalance == true)
                    refreshDirtyRebalance();
                return m_NextRebalance;
            }
        }

        private int m_DaysToNextRebalance = -1;
        public int xDaysToNextRebalance
        {
            get
            {
                if (IsDirtyRebalance == true)
                    refreshDirtyRebalance();
                return m_DaysToNextRebalance;
            }
        }

        public bool loadRebalanceDates(ref List<DateTime> dates, out string error)
        {
            dates.Clear();
            error = "";

            if (GetUploadSPIRows().Count == 0)
                return true;

            SwapUploadSPIRow r = GetUploadSPIRows()[0]; // take 1st row

            int sicovam = 1;
            if (sicovam == 0)
            {
                error = string.Format("Rebalance basket '{0}' not in Sophis", r.InstrumentReference);
                return false;
            }

            return true;
        }

        public double? xLastReportedIndex { get; set; }
        public bool IsLastReportedIndexNull()
        {
            return !xLastReportedIndex.HasValue;
        }
        public double LastReportedIndex
        {
            get
            {
                if (!IsLastReportedIndexNull())
                    return xLastReportedIndex.Value;
                return 0.0;
            }
            set
            {
                xLastReportedIndex = value;
                NotifyPropertyChanged(() => xLastReportedIndex);
                NotifyPropertyChanged(() => LastReportedIndex);
            }
        }

        public DateTime? ReportingTime1 { get; set; }
        public DateTime? ReportingTime2 { get; set; }

        public DateTime? LastReportingTime1 { get; set; }
        public DateTime? LastReportingTime2 { get; set; }

        public bool IsLastReportingTime1Null()
        {
            return !LastReportingTime1.HasValue;
        }
        public bool IsLastReportingTime2Null()
        {
            return !LastReportingTime2.HasValue;
        }

        public string LastReportingTime1String
        {
            get
            {
                return "Last sent: " + (IsLastReportingTime1Null() ? DateTime.MinValue.ToString("dd/MM/yyyy HH:mm") : ReportingTime1.Value.ToString("dd/MM/yyyy HH:mm"));
            }
        }

        public string LastReportingTime2String
        {
            get
            {
                return "Last sent: " + (IsLastReportingTime2Null() ? DateTime.MinValue.ToString("dd/MM/yyyy HH:mm") : ReportingTime2.Value.ToString("dd/MM/yyyy HH:mm"));
            }
        }

        public string FTPInfo { get; set; }

        public DateTime xLastReportingTime
        { // return MAX of the 2 dates
            get
            {
                if (IsLastReportingTime1Null())
                    return LastReportingTime2.Value;
                else if (IsLastReportingTime2Null())
                    return LastReportingTime1.Value;
                else
                    return (System.DateTime.Compare(LastReportingTime1.Value, LastReportingTime2.Value) < 0) ? (LastReportingTime2.Value) : (LastReportingTime1.Value);
            }
        }
        public double xMarketValue { get { return m_marketValue; } }
        public double xMarketValueRaw { get { return m_marketValueRaw; } }
        public double xMarketValueCollateral { get { return m_marketValueCollateral; } }

        public double xCollateralRatio { get { return (xMarketValue == 0.0) ? 0.0 : xMarketValueCollateral / xMarketValue; } }
        public double xIndexValue { get { return m_indexValue; } }
        public double xIndexSnapshotToSave
        {
            get
            {
                if ((xIndexSnapshotMode == TypeManastIndexSnapshotMode.FromReport) && (!IsLastReportedIndexNull()))
                    return LastReportedIndex;
                else
                    return xIndexValue;
            }
        }


        // Tipp volatility

        //private Dictionary<TypeTippAssetClassCategory, SwapAccountPricing.Price> m_tippVolatility = null;
        //private double getTippVolatility(TypeTippAssetClassCategory cat)
        //{
        //    SwapAccountPricing.Price volat = null;
        //    if ((m_tippVolatility != null) && (m_tippVolatility.TryGetValue(cat, out volat)) && (volat != null))
        //        return volat.Value;
        //    else
        //        return 0.0;
        //}

        // Tipp High Risk Exposure by Asset Class Category

        private Dictionary<TypeTippAssetClassCategory, double> m_tippHighRiskExposureByCategory = null;
        public double getTippHighRiskExposure(TypeTippAssetClassCategory cat)
        {
            if ((m_tippHighRiskExposureByCategory != null) && m_tippHighRiskExposureByCategory.ContainsKey(cat))
                return m_tippHighRiskExposureByCategory[cat];
            else
                return 0.0;
        }

        // Tipp High Risk Exposure by Asset Class Class

        private Dictionary<int, double> m_tippHighRiskExposureByAssetClass = null;
        public double getTippHighRiskExposure(int assetClassId)
        {
            if ((m_tippHighRiskExposureByAssetClass != null) && m_tippHighRiskExposureByAssetClass.ContainsKey(assetClassId))
                return m_tippHighRiskExposureByAssetClass[assetClassId];
            else
                return 0.0;
        }

        public Dictionary<string, List<PriceTip>> m_Tips = new Dictionary<string, List<PriceTip>>();

        /// <summary>
        /// Gets a value indicating whether orders were applied to this account or not.
        /// </summary>
        public bool IsVirgin { get { return ((double.IsNaN(m_indexValue)) || (Math.Abs(m_indexValue) < 0.001)); } }

        public bool? BookDividendInAccountCcy { get; set; }
        public bool xBookDividendInAccountCcy
        {
            get
            {
                return BookDividendInAccountCcy.HasValue ? BookDividendInAccountCcy.Value : false;
            }
            set
            {
                BookDividendInAccountCcy = value;
                NotifyPropertyChanged(() => BookDividendInAccountCcy);
                NotifyPropertyChanged(() => xBookDividendInAccountCcy);
            }
        }

        public int? SPIrounding { get; set; }
        public bool IsSPIroundingNull()
        {
            return !SPIrounding.HasValue;
        }

        public IList<SwapAccountHistory> HistoryRows { get; set; } = new List<SwapAccountHistory>();

        public IList<SwapAccountHistory> GetAccountHistoryRows()
        {
            return HistoryRows;
        }
        /// <summary>
        /// Gets the sorted account history rows.
        /// </summary>
        /// <param name="asc">if set to <c>true</c> [asc].</param>
        /// <returns></returns>
        public List<SwapAccountHistory> GetSortedAccountHistoryRows(bool asc)
        {
            List<SwapAccountHistory> accountHistoryList = new List<SwapAccountHistory>();

            accountHistoryList.AddRange(GetAccountHistoryRows());

            accountHistoryList.Sort(
                delegate (SwapAccountHistory ahr1, SwapAccountHistory ahr2)
                {
                    return Comparer<DateTime>.Default.Compare(ahr1.xDate, ahr2.xDate);
                }
            );

            // reverse the orders in an ascendig (aufsteigend) manner
            if (!asc)
                accountHistoryList.Reverse();

            return accountHistoryList;
        }


        public void SetAccountProperties(double marketValue, double marketValueRaw, double marketValueCollateral, double indexValue, double indexValueClosed,
                double weightedBidAskSpread, double volume, double cashWeight, double futureCashWeight, double futureWeight, double bondWeight,
                double stockWeight, double fundWeight, double etfWeight, double certificateWeight,
                bool isIndexComplete, bool isIndexCloseComplete, double leverageFactor, double sum_5_10_40,
                Dictionary<TypeTippAssetClassCategory, SwapAccountPricing.Price> tippVolatility, Dictionary<TypeTippAssetClassCategory, double> tippHighRiskExposureByCategory, Dictionary<int, double> tippHighRiskExposureByAssetClass,
                Dictionary<string, List<PriceTip>> tips)
        {

            m_marketValue = marketValue;
            m_marketValueRaw = marketValueRaw;
            m_marketValueCollateral = marketValueCollateral;
            m_indexValue = indexValue;
            m_indexValueClosed = indexValueClosed;
            m_weightedBidAskSpread = weightedBidAskSpread;
            m_volume = volume;

            m_cash_weight = cashWeight;
            m_future_cash_weight = futureCashWeight;
            m_future_weight = futureWeight;
            m_bond_weight = bondWeight;
            m_stock_weight = stockWeight;
            m_fund_weight = fundWeight;
            m_etf_weight = etfWeight;
            m_certificate_weight = certificateWeight;

            m_isIndexComplete = isIndexComplete;
            m_isIndexCloseComplete = isIndexCloseComplete;

            m_leverageFactor = leverageFactor;
            m_5_10_40 = sum_5_10_40;
            //m_tippVolatility = tippVolatility;
            m_tippHighRiskExposureByCategory = tippHighRiskExposureByCategory;
            m_tippHighRiskExposureByAssetClass = tippHighRiskExposureByAssetClass;
            m_Tips = tips;

            NotifyPropertyChanged(() => m_marketValue);
            NotifyPropertyChanged(() => m_marketValueRaw);
            NotifyPropertyChanged(() => m_marketValueCollateral);
            NotifyPropertyChanged(() => m_indexValueClosed);
            NotifyPropertyChanged(() => m_weightedBidAskSpread);
            NotifyPropertyChanged(() => m_volume);
            NotifyPropertyChanged(() => m_cash_weight);
            NotifyPropertyChanged(() => m_future_cash_weight);
            NotifyPropertyChanged(() => m_future_weight);
            NotifyPropertyChanged(() => m_bond_weight);
            NotifyPropertyChanged(() => m_stock_weight);
            NotifyPropertyChanged(() => m_fund_weight);
            NotifyPropertyChanged(() => m_etf_weight);
            NotifyPropertyChanged(() => m_certificate_weight);
            NotifyPropertyChanged(() => m_isIndexComplete);
            NotifyPropertyChanged(() => m_isIndexCloseComplete);
            NotifyPropertyChanged(() => m_leverageFactor);
            NotifyPropertyChanged(() => m_5_10_40);
            NotifyPropertyChanged(() => m_tippHighRiskExposureByCategory);
            NotifyPropertyChanged(() => m_tippHighRiskExposureByAssetClass);
            NotifyPropertyChanged(() => m_Tips);

            NotifyPropertyChanged(() => xMarketValue);
            NotifyPropertyChanged(() => xMarketValueRaw);
            NotifyPropertyChanged(() => xMarketValueCollateral);
            NotifyPropertyChanged(() => xCollateralRatio);
            NotifyPropertyChanged(() => xIndexValue);
            NotifyPropertyChanged(() => xIndexSnapshotToSave);

            NotifyPropertyChanged(() => xCashWeight);
            NotifyPropertyChanged(() => xFutureCashWeight);
            NotifyPropertyChanged(() => xFutureWeight);
            NotifyPropertyChanged(() => xBondWeight);
            NotifyPropertyChanged(() => xStockWeight);
            NotifyPropertyChanged(() => xStockWeight);
            NotifyPropertyChanged(() => xFundWeight);
            NotifyPropertyChanged(() => xETFWeight);
            NotifyPropertyChanged(() => xCertificateWeight);

            NotifyPropertyChanged(() => xLeverageFactor);
            NotifyPropertyChanged(() => x5_10_40);

            NotifyPropertyChanged(() => xLeverage);
            NotifyPropertyChanged(() => xSwapValue);
            NotifyPropertyChanged(() => xSwapValue2);
            NotifyPropertyChanged(() => xPerformance);
            NotifyPropertyChanged(() => xDayPerformance);
            NotifyPropertyChanged(() => xPerformanceValue);
            NotifyPropertyChanged(() => xMtM);
        }


        public void UpdateSophisReference(string reference, string sicovam, string maturity)
        {
            SwapReference = reference;

            if (maturity != null)
            {
                DateTime m;
                // do not use "DateTime.TryParse(tmpStr, out m)" because this assumes the string is formatted as MM/dd/yyyy since this is the date format of the "en-US" culture
                if (DateTime.TryParseExact(maturity, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out m))
                {
                    //SwapMaturity.SelectedDateFormat = "dd/MM/yyyy";
                    SwapMaturity = m;
                }
            }
        }

        public bool hasStopLossEvent(out List<string> events)
        {
            events = new List<string>();

            if (GetAccountStopLossRows().Count > 0)
            {
                foreach (SwapStopLossRow stop in GetAccountStopLossRows())
                {
                    SwapStopLossKindRow kind = stop.Kind;
                    if (kind == null)
                        continue;
                    XStopLossPerformance p = new XStopLossPerformance();
                    if ((calculateStopLossPerformance(stop.getData(), DateTime.Now.Date, ref p)) && (p.mPerformance < stop.Pct))
                    {
                        events.Add(string.Format("Stop Loss event: {0} performance ({1:P4}) < stop loss ({2:P4}) !", kind.xName, p.mPerformance, stop.Pct));
                    }
                }
            }
            return (events.Count > 0);
        }

        public bool hasCollateralEvent(out List<string> events)
        {
            events = new List<string>();

            List<SwapCollateralCheckRow> rows = GetCollateralCheckRows();

            foreach (SwapCollateralCheckRow r in rows)
            {
                bool passed;
                doCollateralCheck(r.Lookback, r.RatioLimit, DateTime.Now, out passed);
                if (!passed)
                {
                    events.Add(string.Format("Collateral event: all collateral ratios over past {0:N0} calendar days are < ratio limit ({1:P2}) !", r.Lookback, r.RatioLimit));
                }
            }
            return (events.Count > 0);
        }

        public void doCollateralCheck(int lookback, double ratioLimit, DateTime today, out bool passed)
        {
            passed = false;

            DateTime lookbackDate = today.AddDays(-1 * lookback);

            foreach (SwapAccountHistory h in GetAccountHistoryRows())
            {
                if (h.xDate.Date < lookbackDate.Date)
                    continue;
                if ((!h.IsCollateralRatioNull()) && (h.CollateralRatio >= ratioLimit))
                {
                    passed = true;
                    return;
                }
            }
        }

        public List<SwapCollateralCheckRow> GetCollateralCheckRows()
        {
            return new List<SwapCollateralCheckRow>();
        }

        public double getIndexWithoutAccrued()
        {
            double sumFeePerUnit1 = getSumFee(TypeManastEvent.ManagementFee1, true);
            double value = (xIndexValue - sumFeePerUnit1);
            return value;
        }

        public double getPerformanceSwapPrice()
        {
            double indexWithoutAccrued = getIndexWithoutAccrued();
            double value = 0.0;
            if (SwapStrike != 0.0)
                value = (indexWithoutAccrued / SwapStrike - 1.0);
            return value;
        }

        public double getPutPrice()
        {
            DateTime pricingDate = getPricingDate(DateTime.Now.Date);
            DateTime lastBusinessDay = new DateTime();
            lastBusinessDay = GetMatchingBusinessDay(lastBusinessDay, true, true); // last business day of month

            // eg. if pricingDate is Wed, April 2, 2019 then lastBusinessDay is Tue, April 30, 2019 so we get "0.3 * 0.01 * (1 - 2 / 30) / 12"
            double a = pricingDate.Day; // day of the month 1..31 
            double b = lastBusinessDay.Day;
            double value = (0.3 * 0.01 * (1 - a / b) / 12.0);
            return value;
        }

        public double getSumFee(TypeManastEvent eventType, bool perUnit)
        {
            double sum = 0.0;
            foreach (SwapAccountPortfolio pr in AccountPortfolioRows)
            {
                switch (eventType)
                {
                    case TypeManastEvent.ManagementFee1:
                        if (perUnit)
                            sum += pr.xSumMgmtFeePerUnit1;
                        else
                            sum += pr.xSumMgmtFee1;
                        break;
                    case TypeManastEvent.ManagementFee2:
                        if (perUnit)
                            sum += pr.xSumMgmtFeePerUnit2;
                        else
                            sum += pr.xSumMgmtFee2;
                        break;
                    case TypeManastEvent.ManagementFee3:
                        if (perUnit)
                            sum += pr.xSumMgmtFeePerUnit3;
                        else
                            sum += pr.xSumMgmtFee3;
                        break;
                    case TypeManastEvent.PerformanceFee:
                        if (perUnit)
                            sum += pr.xSumPerfFeePerUnit;
                        else
                            sum += pr.xSumPerfFee;
                        break;
                    default:
                        break;
                }
            }
            return sum;
        }

        public bool GetMTMfromFinancing(out double value, out string reason)
        {
            value = 0.0;
            reason = "";

            DateTime today = DateTime.Now.Date;
            SortedDictionary<DateTime, PremiumValue> values;
            CalculatePremiumValues(today, out values);

            foreach (KeyValuePair<DateTime, PremiumValue> v in values)
            {
                if (v.Value.Error != PremiumValue.ErrorCode.eNoError)
                {
                    reason = v.Value.xErrrorString;
                    return false;
                }
                value += v.Value.xMTMfromFinancing;
            }
            return true;
        }
    }
}
