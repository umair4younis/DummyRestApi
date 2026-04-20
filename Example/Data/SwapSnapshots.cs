using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Puma.MDE.Data
{

    /// <summary>
    /// Immutable (by convention) snapshot of everything the pricing engine needs per account.
    /// Build this on a single thread (from NH entities), then pass it into Parallel.ForEach.
    /// </summary>
    public sealed class AccountSnapshot
    {
        // --- Identity / display ---
        public int AccountId { get; private set; }
        public string AccountName { get; private set; }
        public string Currency { get; private set; }
        public int CloseType { get; private set; }
        public DateTime? CloseDate { get; private set; }
        public DateTime PricingDate { get; private set; }


        // --- General pricing flags & factors ---
        public bool IsTipp { get; private set; }
        public double IndexFactor { get; private set; }
        public double? TNA { get; private set; }
        public bool IsTNANull() { return TNA == null; }

        public bool? UseManualFX { get; private set; }
        public bool UseCloseAsLast { get; private set; }
        public bool UseWMForValuation { get; private set; }

        public bool? ApplyMgmFee { get; private set; }
        public bool IsApplyMgmFeeNull() { return ApplyMgmFee == null; }
        public DateTime? MgmFeeStartDate { get; private set; }
        public double MgmFeeBasis { get; private set; }
        public int? MgmFeeCalcType { get; private set; }

        public bool IsMgmFeeStartDateNull() { return MgmFeeStartDate == null; }

        // --- TIPP / volatility inputs ---
        public double? TippManualVolatility { get; private set; }
        public bool IsTippManualVolatilityNull() { return TippManualVolatility == null; }
        public double? TippManualVolatility2 { get; private set; }
        public bool IsTippManualVolatility2Null() { return TippManualVolatility2 == null; }
        public string TippVolatilityInstrument { get; private set; }
        public string TippVolatilityInstrument2 { get; private set; }


        public TypeTippMultiplierDefinition TippMultiplierDefinition { get; private set; }

        public long RowVersion { get; private set; }


        public IReadOnlyList<ManagementFee1Snapshot> ManagementFees1 { get; private set; }
        public IReadOnlyList<CrossWeightSnapshot> CrossWeights { get; private set; }

        public IReadOnlyList<PortfolioRowSnapshot> PortfolioRows { get; private set; }
        public IReadOnlyList<PriceRuleSnapshot> PriceRuleRows { get; private set; }
        public IReadOnlyList<ManualFXRowSnapshot> ManualFXRows { get; private set; }

        public AccountSnapshot(
            int accountId,
            string accountName,
            string currency,
            int closeType,
            DateTime? closeDate,
            DateTime pricingDate,
            bool isTipp,
            double indexFactor,
            double? tna,
            bool? useManualFX,
            bool useCloseAsLast,
            bool xUseWMForValuation,
            bool? applyMgmFee,
            DateTime? mgmFeeStartDate,
            double mgmFeeBasis,
            int? mgmFeeCalcType,
            double? tippManualVolatility,
            double? tippManualVolatility2,
            string tippVolatilityInstrument,
            string tippVolatilityInstrument2,
            TypeTippMultiplierDefinition typeTippMultiplierDefinition,
            long rowVersion,
            IEnumerable<ManagementFee1Snapshot> managementFees1,
            IEnumerable<CrossWeightSnapshot> crossWeights,
            IEnumerable<PortfolioRowSnapshot> portfolioRows,
            IEnumerable<PriceRuleSnapshot> priceRuleRows,
            IEnumerable<ManualFXRowSnapshot> manualFxRows)
        {
            AccountId = accountId;
            AccountName = accountName ?? string.Empty;
            Currency = currency ?? string.Empty;
            CloseType = closeType;
            CloseDate = closeDate;
            PricingDate = pricingDate;

            IsTipp = isTipp;
            IndexFactor = indexFactor;
            TNA = tna;

            UseManualFX = useManualFX;
            UseCloseAsLast = useCloseAsLast;
            UseWMForValuation = xUseWMForValuation;

            ApplyMgmFee = applyMgmFee;
            MgmFeeStartDate = mgmFeeStartDate;
            MgmFeeBasis = mgmFeeBasis;
            MgmFeeCalcType = mgmFeeCalcType;

            TippManualVolatility = tippManualVolatility;
            TippManualVolatility2 = tippManualVolatility2;
            TippVolatilityInstrument = tippVolatilityInstrument ?? string.Empty;
            TippVolatilityInstrument2 = tippVolatilityInstrument2 ?? string.Empty;

            TippMultiplierDefinition = typeTippMultiplierDefinition;

            RowVersion = rowVersion;

            // materialize as read-only to prevent accidental mutation
            ManagementFees1 = ToReadOnly(managementFees1);
            CrossWeights = ToReadOnly(crossWeights);
            PortfolioRows = ToReadOnly(portfolioRows);
            PriceRuleRows = ToReadOnly(priceRuleRows);
            ManualFXRows = ToReadOnly(manualFxRows);
        }

        private static IReadOnlyList<T> ToReadOnly<T>(IEnumerable<T> items)
        {
            if (items == null) return new ReadOnlyCollection<T>(new List<T>(0));
            var list = (items as IList<T>) ?? items.ToList();
            return new ReadOnlyCollection<T>(list);
        }
        public AccountSnapshotForLastPrice GetSnapshotForLastPrice()
        {
            return new AccountSnapshotForLastPrice(
                UseCloseAsLast,
                CloseType,
                CloseDate,
                PriceRuleRows
                );
        }
    }

    public sealed class AccountSnapshotForLastPrice
    {
        public bool UseCloseAsLast { get; set; }
        public int CloseType { get; private set; }
        public DateTime? CloseDate { get; private set; }
        public IReadOnlyList<PriceRuleSnapshot> PriceRuleRows { get; private set; }
        public AccountSnapshotForLastPrice(bool useCloseAsLast, int closeType, DateTime? closeDate, IReadOnlyList<PriceRuleSnapshot> priceRuleRows)
        {
            UseCloseAsLast = useCloseAsLast;
            CloseType = closeType;
            CloseDate = closeDate;
            PriceRuleRows = priceRuleRows;
        }
    }

    public sealed class PriceRuleSnapshot
    {
        public int? RegionId { get; private set; }
        public int? InstrumentTypeId { get; private set; }
        public int PriceFieldId { get; private set; }
        public int RefDateId { get; private set; }
        public DateTime SpecificDate { get; private set; }
        public PriceRuleSnapshot(
            int? regionId, int? instrumentTypeId, int priceFieldId, int refDateId, DateTime specificDate)
        {
            RegionId = regionId;
            InstrumentTypeId = instrumentTypeId;
            PriceFieldId = priceFieldId;
            RefDateId = refDateId;
            SpecificDate = specificDate;
        }
    }

    public sealed class ManualFXRowSnapshot
    {
        public string Currency { get; private set; }
        public double Value { get; private set; }
        public InstrumentRowSnapshot Instrument { get; private set; }
        public ManualFXRowSnapshot(string currency, double value, InstrumentRowSnapshot instrument)
        {
            Currency = currency;
            Value = value;
            Instrument = instrument;
        }
    }

    /// <summary>
    /// Snapshot of a single portfolio position row used by the pricing engine.
    /// Contains only data (no NH proxies, no methods).
    /// </summary>
    public sealed class PortfolioRowSnapshot
    {
        public int PortfolioRowId { get; private set; }
        public int InstrumentId { get; private set; }
        public InstrumentRowSnapshot InstrumentSnapshot { get; private set; }

        public InstrumentRowSnapshot InstrumentByLastPriceInstrumentLink { get; private set; }
        public InstrumentRowSnapshot InstrumentByFXInstrumentLink { get; private set; }

        public bool IsFXInstrumentNull()
        {
            return InstrumentByFXInstrumentLink == null;
        }

        public string StockExchange { get; private set; }
        public string Currency { get; private set; }
        public string AccountCurrency { get; private set; }
        public bool UseWMForValuation { get; private set; }

        public double Nominal { get; private set; }
        public double AveragePrice { get; private set; }

        public bool? UseCloseValue { get; private set; }
        public bool IsUseCloseValueNull()
        {
            return !UseCloseValue.HasValue;
        }
        public double? CloseValue { get; private set; }
        public bool IsCloseValueNull()
        {
            return !CloseValue.HasValue;
        }

        public double? ManualValueLastFX { get; private set; }
        public bool IsManualValueLastFXNull() { return !ManualValueLastFX.HasValue; }

        public double? ManualValueLastPrice { get; private set; }
        public bool IsManualValueLastPriceNull() { return !ManualValueLastPrice.HasValue; }

        public double Collateral { get; private set; }
        public double InvestedValue { get; private set; }
        public double MarketWeight { get; private set; }

        // TIPP classification (used by your risk category aggregation)
        public int? TippAssetRiskProfileId { get; private set; }
        public int? TippAssetClassCategoryId { get; private set; }
        public int? TippAssetClassId { get; private set; }
        public bool IsTippAssetRiskProfileIDNull() { return TippAssetRiskProfileId == null; }
        public bool IsTippAssetClassCategoryIDNull() { return TippAssetClassCategoryId == null; }
        public bool IsTippAssetClassIDNull() { return TippAssetClassId == null; }


        public PortfolioRowSnapshot(
            int portfolioRowId,
            int instrumentId,
            InstrumentRowSnapshot instrSnapshot,
            InstrumentRowSnapshot instrumentByLastPriceInstrumentLink,
            InstrumentRowSnapshot instrumentByFXInstrumentLink,
            string stockExchange,
            string currency,
            string accountCurrency,
            bool useWMForValuation,
            double nominal,
            double averagePrice,
            bool? useCloseValue,
            double? closeValue,
            double? manualValueLastFX,
            double? manualValueLastPrice,
            double collateral,
            double investedValue,
            double marketWeight,
            int? tippAssetRiskProfileId,
            int? tippAssetClassCategoryId,
            int? tippAssetClassId)
        {
            PortfolioRowId = portfolioRowId;
            InstrumentId = instrumentId;
            InstrumentSnapshot = instrSnapshot;

            InstrumentByLastPriceInstrumentLink = instrumentByLastPriceInstrumentLink;
            InstrumentByFXInstrumentLink = instrumentByFXInstrumentLink;

            StockExchange = stockExchange;
            Currency = currency ?? string.Empty;
            AccountCurrency = accountCurrency ?? string.Empty;
            UseWMForValuation = useWMForValuation;

            Nominal = nominal;
            AveragePrice = averagePrice;
            UseCloseValue = useCloseValue;
            CloseValue = closeValue;
            ManualValueLastFX = manualValueLastFX;
            ManualValueLastPrice = manualValueLastPrice;

            Collateral = collateral;
            InvestedValue = investedValue;
            MarketWeight = marketWeight;

            TippAssetRiskProfileId = tippAssetRiskProfileId;
            TippAssetClassCategoryId = tippAssetClassCategoryId;
            TippAssetClassId = tippAssetClassId;
        }
    }

    public sealed class InstrumentRowSnapshot
    {
        public int InstrumentTypeId { get; private set; }
        public string InstrumentName { get; private set; }
        public string InstrumentTypeName { get; private set; }
        public string RIC { get; private set; }
        public string SICOVAM { get; private set; }
        public string PriceProvider { get; private set; }
        public int? CountryRegionId { get; private set; }

        public double ContractSize { get; private set; }
        public DateTime? BondMaturity { get; private set; }
        public bool IsBondMaturityNull() { return BondMaturity == null; }
        public double? BondCoupon { get; private set; }

        public bool IsRealCash { get; private set; }
        public bool IsInventory { get; private set; }
        public bool IsCash { get; private set; }
        public bool IsCashFee { get; private set; }
        public bool IsBond { get; private set; }
        public bool IsETF { get; private set; }
        public bool IsFund { get; private set; }
        public bool IsStock { get; private set; }
        public bool IsFuture { get; private set; }
        public bool IsFXOpt { get; private set; }
        public bool IsFXSpread { get; private set; }
        public bool IsFXSpot { get; private set; }
        public bool IsFXDerivative { get; private set; }

        public InstrumentRowSnapshot(
            SwapAccountInstrument instr)
        {
            InstrumentTypeId = instr.InstrumentTypeId;
            InstrumentTypeName = instr.InstrumentType.TypeName;
            InstrumentName = instr.InstrumentName;
            RIC = instr.RIC;
            SICOVAM = instr.SICOVAM;
            PriceProvider = instr.PriceProvider;
            if (instr.Country != null && instr.Country.Region != null)
            {
                CountryRegionId = instr.Country.Region.Id;
            }
            else
            {
                CountryRegionId = null;
            }

            ContractSize = instr.ContractSize;
            BondMaturity = instr.BondMaturity;
            BondCoupon = instr.BondCoupon;

            IsRealCash = instr.IsRealCash;
            IsInventory = instr.InstrumentType.IsInventory;
            IsCash = instr.InstrumentType.IsCash;
            IsCashFee = instr.InstrumentType.IsCashFee;
            IsBond = instr.InstrumentType.IsBond;
            IsETF = instr.InstrumentType.IsETF;
            IsFund = instr.InstrumentType.IsFund;
            IsStock = instr.InstrumentType.IsStock;
            IsFuture = instr.InstrumentType.IsFuture;
            IsFXOpt = instr.InstrumentType.IsFXOpt;
            IsFXSpread = instr.InstrumentType.IsFXSpread;
            IsFXSpot = instr.InstrumentType.IsFXSpot;
            IsFXDerivative = instr.InstrumentType.IsFXDerivative;
        }
    }
    /// <summary>
    /// Minimal snapshot for management fee band 1 (you can add Fee2/Fee3 snapshots similarly if used).
    /// </summary>
    public sealed class ManagementFee1Snapshot
    {
        public double FeePct { get; private set; }

        public ManagementFee1Snapshot(double feePct)
        {
            FeePct = feePct;
        }
    }

    /// <summary>
    /// Snapshot of cross-weight constraints used by the "5/10/40" style checks.
    /// </summary>
    public sealed class CrossWeightSnapshot
    {
        public double? MinWeight { get; private set; }
        public string InstrumentTypes { get; private set; }

        public CrossWeightSnapshot(double? minWeight, string instrumentTypes)
        {
            MinWeight = minWeight;
            InstrumentTypes = instrumentTypes;
        }

        public List<int> GetInstrumentTypeIds(out string error)
        {
            List<int> ids = new List<int>();
            error = "";

            if (string.IsNullOrEmpty(InstrumentTypes))
                return ids;

            string[] names = InstrumentTypes.Split(";,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); // eg. string "Stock,Certificate;Fund" gives array {"Stock","Certificate","Fund"}
            foreach (string n in names)
            {
                error = string.Format("unknown instrument type '{0}'", n.Trim());
                return ids;
            }
            ids.Sort();
            return ids;
        }
    }

    public sealed class OrderSnapshotForNominalOfStock
    {
        public DateTime TradeDate { get; private set; }
        public DateTime? DateExecuted { get; private set; }
        public IReadOnlyList<TradeSnapshotRow> TradeSnapshotRows { get; private set; }
        public OrderSnapshotForNominalOfStock(DateTime tradeDate, DateTime? dateExecuted, IReadOnlyList<TradeSnapshotRow> tradeSnapshotRows)
        {
            TradeDate = tradeDate;
            DateExecuted = dateExecuted;
            TradeSnapshotRows = tradeSnapshotRows;
        }
    }

    public sealed class TradeSnapshotRow
    {
        public int InstrumentId { get; private set; }
        public double Nominal { get; private set; }
        public double Price { get; private set; }
        public double Accrued { get; private set; }
        public double FXRate { get; private set; }
        public double Fee { get; private set; }
        public TradeSnapshotRow(int instrumentId, double nominal, double price, double accrued, double fXRate, double fee)
        {
            InstrumentId = instrumentId;
            Nominal = nominal;
            Price = price;
            Accrued = accrued;
            FXRate = fXRate;
            Fee = fee;
        }
    }


    public sealed class PricingResult
    {
        public int AccountId { get; private set; }

        // Account-level totals
        public double MvSum { get; private set; }
        public double MvRaw { get; private set; }
        public double MvCollateralSum { get; private set; }
        public double MvClosedSum { get; private set; }
        public double WeightedBidAskSpreadSum { get; private set; }

        public double MvCash { get; private set; }
        public double MvFutureCash { get; private set; }
        public double MvFuture { get; private set; }
        public double MvBond { get; private set; }
        public double MvStock { get; private set; }
        public double MvFund { get; private set; }
        public double MvETF { get; private set; }
        public double MvCertificate { get; private set; }

        public bool IsIndexComplete { get; private set; }
        public bool IsIndexCloseComplete { get; private set; }
        public double LeverageFactor { get; private set; }
        public double Sum_5_10_40 { get; private set; }

        public Dictionary<TypeTippAssetClassCategory, double> TippHighRiskExposureByCategory { get; private set; }
        public Dictionary<int, double> TippHighRiskExposureByAssetClass { get; private set; }
        public Dictionary<TypeTippAssetClassCategory, SwapAccountPricing.Price> TippVolatility { get; private set; }
        // Per-row updates
        public IReadOnlyList<RowUpdate> RowUpdates { get; private set; }

        public CashFeeInstruction CashFee { get; private set; }
        public Dictionary<string, List<PriceTip>> ArTips { get; private set; }

        public PricingResult(
            int accountId,
            double mvSum,
            double mvRaw,
            double mvCollateralSum,
            double mvClosedSum,
            double weightedBidAskSpreadSum,
            double mvCash,
            double mvFutureCash,
            double mvFuture,
            double mvBond,
            double mvStock,
            double mvFund,
            double mvETF,
            double mvCertificate,
            bool isIndexComplete,
            bool isIndexCloseComplete,
            double leverageFactor,
            double sum_5_10_40,
            Dictionary<TypeTippAssetClassCategory, double> tippHighRiskExposureByCategory,
            Dictionary<int, double> tippHighRiskExposureByAssetClass,
            Dictionary<TypeTippAssetClassCategory, SwapAccountPricing.Price> tippVolatility,
            IReadOnlyList<RowUpdate> rowUpdates,
            CashFeeInstruction cashFee,
            Dictionary<string, List<PriceTip>> arTips)
        {
            AccountId = accountId;
            MvSum = mvSum;
            MvRaw = mvRaw;
            MvCollateralSum = mvCollateralSum;
            MvClosedSum = mvClosedSum;
            WeightedBidAskSpreadSum = weightedBidAskSpreadSum;

            MvCash = mvCash;
            MvFutureCash = mvFutureCash;
            MvFuture = mvFuture;
            MvBond = mvBond;
            MvStock = mvStock;
            MvFund = mvFund;
            MvETF = mvETF;
            MvCertificate = mvCertificate;

            IsIndexComplete = isIndexComplete;
            IsIndexCloseComplete = isIndexCloseComplete;
            LeverageFactor = leverageFactor;
            Sum_5_10_40 = sum_5_10_40;

            TippHighRiskExposureByCategory = tippHighRiskExposureByCategory;
            TippHighRiskExposureByAssetClass = tippHighRiskExposureByAssetClass;
            TippVolatility = tippVolatility;

            RowUpdates = rowUpdates ?? (IReadOnlyList<RowUpdate>)Array.Empty<RowUpdate>();
            CashFee = cashFee;

            ArTips = arTips;
        }
    }

    public sealed class RowUpdate
    {
        public int PortfolioRowId { get; private set; }
        public int InstrumentTypeId { get; private set; }
        public double ClosePrice { get; private set; }
        public double ClosePriceCcy { get; private set; }
        public double LastPrice { get; private set; }
        public double LastPriceCcy { get; private set; }
        public double BidPrice { get; private set; }
        public double AskPrice { get; private set; }
        public double PriceOffset { get; private set; }
        public double PriceOffsetClosed { get; private set; }
        public double LastFx { get; private set; }
        public double CloseFx { get; private set; }
        public double Delta { get; private set; }
        public double Gamma { get; private set; }

        // calculated values
        public double MarketValue { get; private set; }           // (last + offset) * nominal
        public double MarketValueClosed { get; private set; }     // (close + offsetClosed) * nominal
        public double InvestedValue { get; private set; }         // averagePrice * nominal
        public double MarketValueCollateral { get; private set; }


        public Dictionary<string, List<PriceTip>> Tips { get; private set; }

        // Final normalized weights to pass to SetGridProperties2
        public double InvestedWeight { get; private set; }
        public double MarketWeight { get; private set; }
        public double MarketWeightCollateral { get; private set; }

        public void SetAdditionalProperties(double investedWeight, double marketWeight, double marketWeightCollateral)
        {
            InvestedWeight = investedWeight;
            MarketWeight = marketWeight;
            MarketWeightCollateral = marketWeightCollateral;
        }

        public double WeightedBidAskSpread { get; private set; }

        public RowUpdate(
            int portfolioRowId,
            int instrumentTypeId,
            double closePrice,
            double closePriceCcy,
            double lastPrice,
            double lastPriceCcy,
            double bidPrice,
            double askPrice,
            double priceOffset,
            double priceOffsetClosed,
            double lastFx,
            double closeFx,
            double delta,
            double gamma,
            double marketValue,
            double marketValueClosed,
            double investedValue,
            double marketValueCollateral,
            Dictionary<string, List<PriceTip>> tips)
        {
            PortfolioRowId = portfolioRowId;
            InstrumentTypeId = instrumentTypeId;
            ClosePrice = closePrice;
            ClosePriceCcy = closePriceCcy;
            LastPrice = lastPrice;
            LastPriceCcy = lastPriceCcy;
            BidPrice = bidPrice;
            AskPrice = askPrice;
            PriceOffset = priceOffset;
            PriceOffsetClosed = priceOffsetClosed;
            LastFx = lastFx;
            CloseFx = closeFx;
            Delta = delta;
            Gamma = gamma;

            MarketValue = marketValue;
            MarketValueClosed = marketValueClosed;
            InvestedValue = investedValue;
            MarketValueCollateral = marketValueCollateral;

            Tips = tips;
        }

    }


    public sealed class CashFeeInstruction
    {
        public bool HasInstruction { get; private set; }
        public int? PortfolioRowId { get; private set; }
        public double NewNominal { get; set; }
        public Dictionary<string, List<PriceTip>> prCashFeeTips { get; set; }


        private CashFeeInstruction(bool hasInstruction, int? portfolioRowId, double newNominal)
        {
            HasInstruction = hasInstruction;
            PortfolioRowId = portfolioRowId;
            NewNominal = newNominal;
        }

        public static CashFeeInstruction None()
            => new CashFeeInstruction(false, null, 0.0);

        public static CashFeeInstruction UpdateExisting(int portfolioRowId, double newNominal)
            => new CashFeeInstruction(true, portfolioRowId, newNominal);

        public static CashFeeInstruction CreateNew(double newNominal)
            => new CashFeeInstruction(true, null, newNominal);


    }


}

