using Puma.MDE.SwapAccountPricing.Provider;
using Puma.MDE.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Puma.MDE.SwapAccountPricing
{
    /// <summary>
    /// This class prices the accounts
    /// </summary>
    public static class PortfolioPricer
    {
        public static Dictionary<string, IPriceProvider> s_pricer = null;
        public static string s_defaultProvider = "Kx-Sophis";
        private static Dictionary<int, Price> s_ir = new Dictionary<int, Price>();
        public static int countonce = 0;

        /// <summary>
        /// Initializes the <see cref="PortfolioPricer"/> class.
        /// </summary>
        static PortfolioPricer()
        {
        }

        /// <summary>
        /// Pre-registers all FX rates and interest rates instruments (avoids the return of double.Nan at the first price retrieval).
        /// </summary>
        /// <param name="idt">The idt.</param>
        public static void PreRegisterRates()
        {
            // P106919: Commented lines as no work is done

            // try to fetch (register) all prices
            //foreach (SwapAccountInstrument ir in idt.Rows)
            //{
            //if (!ir.IsRICNull() && (ir.InstrumentType.IsFXRate || ir.InstrumentType.IsRate))
            //{
            //}
            //}
        }
        /// <summary>
        /// Initializes the price provider.
        /// </summary>
        public static void InitializePriceProvider()
        {
            s_pricer = new Dictionary<string, IPriceProvider>();

            // try to get the data providers from the app.config file
            foreach (string s in Engine.Configuration.Get("Data.PriceProviders").Split(';'))
            {
                string para = s.Substring(s.IndexOf('(') + 1);
                para = para.Substring(0, para.Length - 1);
                string key = s.Substring(0, s.IndexOf('='));
                string type = s.Substring(key.Length + 1);
                type = typeof(IPriceProvider).Namespace + "." + type.Substring(0, type.IndexOf('('));
                object instance = Activator.CreateInstance(Type.GetType(type), para);
                s_pricer.Add(key, instance as IPriceProvider);
                if (s_defaultProvider == null)
                    s_defaultProvider = key;
            }
        }

        /// <summary>
        /// Gets the available providers.
        /// </summary>
        /// <value>The available providers.</value>
		public static string[] AvailableProviders
        {
            get
            {
                string[] s = new string[s_pricer.Count];
                s_pricer.Keys.CopyTo(s, 0);
                Array.Sort(s);
                return s;
            }
        }

        /// <summary>
        /// Gets the default name of the price provider.
        /// </summary>
        /// <returns></returns>
        public static string GetDefaultPriceProviderName()
        {
            if (s_pricer != null && s_defaultProvider != string.Empty)
                return s_pricer[s_defaultProvider].ProviderName();
            else
                return "Default price provider not found.";
        }

        private static Dictionary<string, List<PriceTip>> newPortfolioRowTips()
        {
            Dictionary<string, List<PriceTip>> tips = new Dictionary<string, List<PriceTip>>();
            tips["xClosePriceCcy"] = new List<PriceTip>();
            tips["xCloseFxRate"] = new List<PriceTip>();
            tips["xClosePrice"] = new List<PriceTip>();
            tips["xLastPriceCcy"] = new List<PriceTip>();
            tips["xFXRate"] = new List<PriceTip>();
            tips["xLastPrice"] = new List<PriceTip>();
            return tips;
        }

        private static Dictionary<string, List<PriceTip>> newAccountRowTips()
        {
            Dictionary<string, List<PriceTip>> tips = new Dictionary<string, List<PriceTip>>();
            tips["xTippVolEquity"] = new List<PriceTip>();
            tips["xTippVolBond"] = new List<PriceTip>();
            return tips;
        }

        static public DateTime getWeekday(DateTime date)
        // get weekday (Mon-Fri) that is equal to or immediately precedes the specified date
        {
            DateTime d = date;
            if (d.DayOfWeek == DayOfWeek.Saturday)
                d = d.AddDays(-1);
            else if (d.DayOfWeek == DayOfWeek.Sunday)
                d = d.AddDays(-2);
            return d;
        }

        private static void getClosePriceTips(PortfolioRowSnapshot pr, Price closePriceCcy, bool isClosePriceCcyMissing, Price closeFxRate, bool isCloseFxRateMissing, Dictionary<string, List<PriceTip>> tips)
        {
            DateTime yesterday = getWeekday(DateTime.Now.Date.AddDays(-1));

            if (isClosePriceCcyMissing)
                tips["xClosePriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eClosePriceCcyIsMissing, mDate = null, mSeverity = 4 });
            else if (pr.UseCloseValue.HasValue && pr.UseCloseValue.Value)
                tips["xClosePriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eClosePriceCcyIsManual, mDate = null, mSeverity = 1 });
            else if (closePriceCcy.Updated < yesterday)
                tips["xClosePriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eClosePriceCcyIsOld, mDate = closePriceCcy.Updated, mSeverity = 3 });

            tips["xClosePrice"].AddRange(tips["xClosePriceCcy"]); // copy tips from "Close Price Ccy" to "Close Price"

            if (pr.AccountCurrency != pr.Currency)
            {
                if (isCloseFxRateMissing)
                    tips["xCloseFxRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eCloseFXRateIsMissing, mDate = null, mSeverity = 4 });
                else if (closeFxRate.Updated < yesterday)
                    tips["xCloseFxRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eCloseFXRateIsOld, mDate = closeFxRate.Updated, mSeverity = 3 });

                tips["xClosePrice"].AddRange(tips["xCloseFxRate"]); // append tips from "Close Fx Rate" to "Close Price"
            }
        }

        private static void getTippVolatilityTips(AccountSnapshot ar, string key, string volatilityInstrument, bool isManualVolatilityNull, Price tippVolatility, Dictionary<string, List<PriceTip>> tips)
        {
            DateTime yesterday = getWeekday(DateTime.Now.Date.AddDays(-1));

            if (!isManualVolatilityNull)
                tips[key].Add(new PriceTip() { mType = TypeManastPriceTip.eVolatilityIsManual, mDate = null, mSeverity = 1, mName = volatilityInstrument });
            else if ((tippVolatility == null) || (true))
                tips[key].Add(new PriceTip() { mType = TypeManastPriceTip.eVolatilityIsMissing, mDate = null, mSeverity = 4, mName = volatilityInstrument });
            else if (tippVolatility.Updated < yesterday)
                tips[key].Add(new PriceTip() { mType = TypeManastPriceTip.eVolatilityIsOld, mDate = tippVolatility.Updated, mSeverity = 3, mName = volatilityInstrument });
        }

        private static void getLastPriceTips(AccountSnapshot ar, PortfolioRowSnapshot pr, Price lastPriceCcy, bool isLastPriceCcyMissing, Price lastFxRate, bool isLastFxRateMissing, Price lastPrice, bool isLastPriceMissing, Dictionary<string, List<PriceTip>> tips)
        {
            DateTime today = DateTime.Now.Date;
            DateTime yesterday = getWeekday(today.AddDays(-1)); // weekday (Mon-Fri) that is <= today - 1

            if (isLastPriceCcyMissing)
            {
                tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsClosePriceCcy, mDate = null, mSeverity = 4 });
                tips["xLastPriceCcy"].AddRange(tips["xClosePriceCcy"]); // copy tips of close ccy to last ccy because last ccy is set equal to close ccy
            }
            else
            {
                if (pr.InstrumentByLastPriceInstrumentLink != null)
                {
                    tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsManualFromInstrument, mDate = null, mSeverity = 2, mName = pr.InstrumentByLastPriceInstrumentLink.InstrumentName });
                }
                else
                {
                    if (!pr.IsManualValueLastPriceNull())
                    {
                        tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsManual, mDate = null, mSeverity = 1 });
                    }
                    else
                    {
                        SPriceRequest requested = lastPriceCcy.RequestType;

                        if ((requested != null) && (ar.UseCloseAsLast)) // when the account is priced using close prices, check if they are of the kind that was requested and not too old
                        {
                            if (lastPriceCcy.ReturnField != requested.mField) // eg. warn when we requested "Fixing1 on T" but obtained Close instead
                            {
                                tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsClose, mDate = lastPriceCcy.Updated, mSeverity = 4, mName = requested.ToString() });
                            }
                            else
                            {
                                if (lastPriceCcy.ReturnField == TypeManastPriceField.eClose) // check if Close prices are not too old
                                {
                                    switch (requested.mRefDate)
                                    {
                                        case TypeManastCloseRefDate.eT:
                                            if (lastPriceCcy.Updated < today)
                                                tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsOld, mDate = lastPriceCcy.Updated, mSeverity = 3 });
                                            break;
                                        case TypeManastCloseRefDate.eTMinus1:
                                            if (lastPriceCcy.Updated < yesterday)
                                                tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsOld, mDate = lastPriceCcy.Updated, mSeverity = 3 });
                                            break;
                                        case TypeManastCloseRefDate.eSpecificDate:
                                            if ((requested.mCloseDate.HasValue) && (lastPriceCcy.Updated < requested.mCloseDate.Value))
                                                tips["xLastPriceCcy"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceCcyIsOld, mDate = lastPriceCcy.Updated, mSeverity = 3 });
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            tips["xLastPrice"].AddRange(tips["xLastPriceCcy"]);

            if (pr.AccountCurrency != pr.Currency)
            {
                if (isLastFxRateMissing)
                {
                    tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsMissing, mDate = null, mSeverity = 4 });
                }
                else
                {
                    if (!pr.IsFXInstrumentNull())
                    {
                        tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsManualFromInstrument, mDate = null, mSeverity = 2, mName = pr.InstrumentByFXInstrumentLink.InstrumentName });
                    }
                    else
                    {
                        if (!pr.IsManualValueLastFXNull())
                        {
                            tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsManual, mDate = null, mSeverity = 1 });
                        }
                        else
                        {
                            if ((ar.UseManualFX.HasValue && ar.UseManualFX.Value) && (ar.ManualFXRows.Count > 0))
                            {
                                foreach (ManualFXRowSnapshot row in ar.ManualFXRows)
                                {
                                    if (row.Currency != pr.Currency)
                                        continue;
                                    if (row.Value != 0.0) // a manual rate?
                                    {
                                        tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsManual2, mDate = null, mSeverity = 1 });
                                        break;
                                    }
                                    if (row.Instrument != null) // a fixing instrument?
                                    {
                                        tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsManualFromInstrument2, mDate = null, mSeverity = 2, mName = row.Instrument.InstrumentName });
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (ar.UseWMForValuation)
                                {
                                    tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsWMFixing, mDate = null, mSeverity = 2, mName = string.Format("WMFIXING {0}/{1}", ar.Currency, pr.Currency) });
                                }
                            }

                            SPriceRequest requested = lastFxRate.RequestType;

                            if ((requested != null) && (ar.UseCloseAsLast))
                            {
                                if (lastFxRate.ReturnField != requested.mField)
                                {
                                    tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsClose, mDate = lastFxRate.Updated, mSeverity = 4, mName = requested.ToString() });
                                }
                                else
                                {
                                    if (lastFxRate.ReturnField == TypeManastPriceField.eClose)
                                    {
                                        switch (requested.mRefDate)
                                        {
                                            case TypeManastCloseRefDate.eT:
                                                if (lastFxRate.Updated < today)
                                                    tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsOld, mDate = lastFxRate.Updated, mSeverity = 3 });
                                                break;
                                            case TypeManastCloseRefDate.eTMinus1:
                                                if (lastFxRate.Updated < yesterday)
                                                    tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsOld, mDate = lastFxRate.Updated, mSeverity = 3 });
                                                break;
                                            case TypeManastCloseRefDate.eSpecificDate:
                                                if ((requested.mCloseDate.HasValue) && (lastFxRate.Updated < requested.mCloseDate.Value))
                                                    tips["xFXRate"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastFXRateIsOld, mDate = lastFxRate.Updated, mSeverity = 3 });
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                tips["xLastPrice"].AddRange(tips["xFXRate"]);
            }

            if (isLastPriceMissing)
            {
                tips["xLastPrice"].Add(new PriceTip() { mType = TypeManastPriceTip.eLastPriceIsClosePrice, mDate = null, mSeverity = 4 });
                tips["xLastPrice"].AddRange(tips["xClosePrice"]); // append tips from close to last because last will have been set equal to close 
            }
        }

        public static PricingResult Price(AccountSnapshot account)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            // market asset class weights
            double mvCash = 0.0;
            double mvCashFee = 0.0;
            double mvFuture = 0.0;
            double mvFutureCash = 0.0;
            double mvBond = 0.0;
            double mvStock = 0.0;
            double mvFund = 0.0;
            double mvETF = 0.0;
            double mvCertificate = 0.0;

            double mvSum = 0.0;
            double mvRaw = 0.0;
            double mvCollateralSum = 0.0;
            double ciSum = 0.0;
            double mvClosedSum = 0.0;
            double weightedBidAskSpreadSum = 0.0;

            Dictionary<TypeTippAssetClassCategory, double> tippHighRiskExposureByCategory = new Dictionary<TypeTippAssetClassCategory, double>(); // {(asset class category, SUM market value)} eg. {("Bond", ..),("Equity", ..)}
            Dictionary<int, double> tippHighRiskExposureByAssetClass = new Dictionary<int, double>(); // {(asset class id, SUM market value)} eg. {("Equities EM", ..),("MSCI World", ..)}
            Dictionary<TypeTippAssetClassCategory, Price> tippVolatility = new Dictionary<TypeTippAssetClassCategory, Price>(); // {(asset class category, volatility)} eg. {("Bond", ..),("Equity", ..)}

            bool isIndexComplete = true;
            bool isIndexCloseComplete = true;

            CashFeeInstruction prCashFee = null;
            List<RowUpdate> portfolioRowUpdates = new List<RowUpdate>();

            try
            {
                DateTime startTime = DateTime.Now;

                IReadOnlyList<PortfolioRowSnapshot> prs = account.PortfolioRows;
                foreach (PortfolioRowSnapshot pr in prs)
                {
                    // no nominal, no pricing (do not price inventory instruments, but price always cash instruments)
                    if ((!pr.InstrumentSnapshot.IsRealCash && pr.Nominal == 0.0) || pr.InstrumentSnapshot.IsInventory) // || pr.Instrument.InstrumentType.IsFXSpot)
                        continue;

                    if (pr.InstrumentSnapshot.IsCashFee)
                    {
                        prCashFee = CashFeeInstruction.UpdateExisting(pr.PortfolioRowId, pr.Nominal);
                        continue;
                    }

                    InstrumentRowSnapshot ir = pr.InstrumentSnapshot;

                    Dictionary<string, List<PriceTip>> prTips = newPortfolioRowTips();

                    // close price (from HISTORIQUE on T-1)

                    Price closePriceCcy = GetClosePrice(pr, false);
                    bool isClosePriceCcyMissing = false;

                    Price closePrice = new Price(closePriceCcy);

                    Price closeFxRate = null;
                    bool isCloseFxRateMissing = false;
                    if (pr.AccountCurrency != pr.Currency)
                    {
                        closeFxRate = GetCloseFXRate(pr);
                        isCloseFxRateMissing = false;
                        closePrice.Value /= closeFxRate.Value;
                    }

                    getClosePriceTips(pr, closePriceCcy, isClosePriceCcyMissing, closeFxRate, isCloseFxRateMissing, prTips);

                    // last price : real-time or close, depending on account settings

                    Price lastPriceCcy = GetLastPrice(pr, account.GetSnapshotForLastPrice(), false);
                    bool isLastPriceCcyMissing = false;
                    if (isLastPriceCcyMissing)
                        lastPriceCcy = closePriceCcy;

                    Price lastPrice = new Price(lastPriceCcy);

                    Price lastFxRate = null;
                    bool isLastFxRateMissing = false;
                    if (pr.AccountCurrency != pr.Currency)
                    {
                        lastFxRate = GetFXRate(account, pr, false);
                        isLastFxRateMissing = false;
                        lastPrice.Value /= lastFxRate.Value;
                    }

                    bool isLastPriceMissing = false;
                    if (isLastPriceMissing)
                        lastPrice = closePrice;

                    getLastPriceTips(account, pr, lastPriceCcy, isLastPriceCcyMissing, lastFxRate, isLastFxRateMissing, lastPrice, isLastPriceMissing, prTips);

                    // ask price

                    Price askPrice = new Price(GetAskPrice(ir)); // copy price as otherwise the below /= modifies the price inside the cache
                    if (pr.AccountCurrency != pr.Currency)
                    {
                        askPrice.Value /= lastFxRate.Value;
                    }
                    bool isAskPriceMissing = false;
                    if (isAskPriceMissing)
                        askPrice = closePrice;

                    // bid price

                    Price bidPrice = new Price(GetBidPrice(ir));
                    if (pr.AccountCurrency != pr.Currency)
                    {
                        bidPrice.Value /= lastFxRate.Value;
                    }
                    bool isBidPriceMissing = false;
                    if (isBidPriceMissing)
                        bidPrice = closePrice;

                    // bond offset

                    double priceOffset = 0.0;
                    double priceOffsetClosed = 0.0;

                    bool calculateAccrued = (pr.InstrumentSnapshot.IsBond) && (account.TippMultiplierDefinition != TypeTippMultiplierDefinition.Allianz);
                    if ((calculateAccrued) && (!pr.InstrumentSnapshot.IsBondMaturityNull()) && (pr.InstrumentSnapshot.BondMaturity.Value.Date > DateTime.Now.Date))
                    {
                        try
                        {
                            priceOffset = pr.InstrumentSnapshot.ContractSize * s_pricer[s_defaultProvider].GetAccrued(DateTime.Now, pr.InstrumentSnapshot.SICOVAM);
                            priceOffsetClosed = pr.InstrumentSnapshot.ContractSize * s_pricer[s_defaultProvider].GetAccrued(DateTime.Now.AddDays(-1), pr.InstrumentSnapshot.SICOVAM);
                            if (pr.AccountCurrency != pr.Currency)
                            {
                                priceOffset /= lastFxRate.Value;
                                priceOffsetClosed /= lastFxRate.Value;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    // check if index and close index is complete
                    isIndexComplete &= !double.IsNaN(lastPrice.Value) && (lastPrice.Value > 0.0);
                    isIndexCloseComplete &= !double.IsNaN(closePrice.Value) && (closePrice.Value > 0.0);

                    double delta = GetDelta(pr.InstrumentSnapshot);
                    double gamma = GetGamma(pr.InstrumentSnapshot);


                    // update AccountDataset with the last price values (close/last/bid/ask, isLastPriceMissing and offset)
                    //pr.SetGridProperties(closePrice.Value, closePriceCcy.Value, lastPrice.Value, lastPriceCcy.Value, bidPrice.Value, askPrice.Value, priceOffset, priceOffsetClosed, (lastFxRate != null) ? lastFxRate.Value : 1.0, (closeFxRate != null) ? closeFxRate.Value : 1.0, delta, gamma, prTips);

                    double marketValue = (lastPrice.Value + priceOffset) * pr.Nominal * pr.InstrumentSnapshot.ContractSize / ((pr.InstrumentSnapshot.IsBond) ? 100.0 : 1.0);
                    double marketValueCollateral = (lastPrice.Value + priceOffset) * pr.Collateral;
                    double marketValueClosed = (closePrice.Value + priceOffsetClosed) * pr.Nominal * pr.InstrumentSnapshot.ContractSize / ((pr.InstrumentSnapshot.IsBond) ? 100.0 : 1.0);
                    double bidPriceForCalculation = (bidPrice.Value == 0.0) ? (closePrice.Value == 0.0) ? lastPrice.Value : closePrice.Value : bidPrice.Value;
                    double askPriceForCalculation = (askPrice.Value == 0.0) ? (closePrice.Value == 0.0) ? lastPrice.Value : closePrice.Value : askPrice.Value;
                    double xBidAskSpreadPercent = (pr.InstrumentSnapshot.IsCash || pr.InstrumentSnapshot.IsBond || pr.Nominal == 0) ? 0.0 : (askPriceForCalculation - bidPriceForCalculation) / bidPriceForCalculation;
                    double weightedBidAskSpread = (pr.InstrumentSnapshot.IsCash || pr.InstrumentSnapshot.IsBond || pr.Nominal == 0) ? 0.0 : xBidAskSpreadPercent * pr.MarketWeight;

                    // calculate market weights
                    // TODO: cash type as enumeration
                    switch (pr.InstrumentSnapshot.InstrumentTypeName)
                    {
                        case "Cash":
                            mvCash += marketValue;
                            break;
                        case "CashFee":
                            mvCashFee += marketValue;
                            break;
                        case "FutureCash":
                            mvFutureCash += marketValue;
                            break;
                        case "Future":
                            mvFuture += marketValue;
                            break;
                        case "Bond":
                            mvBond += marketValue;
                            break;
                        case "Stock":
                            mvStock += marketValue;
                            break;
                        case "Fund":
                            mvFund += marketValue;
                            break;
                        case "ETF":
                            mvETF += marketValue;
                            break;
                        case "Certificate":
                            mvCertificate += marketValue;
                            break;
                        default:
                            break;
                    }


                    // caculate the sum of the account value (lastprice + offset) * nominal
                    mvSum += marketValue;
                    mvCollateralSum += marketValueCollateral;
                    // caculate the sum of the account closed value (closeprice + offset) * nominal
                    mvClosedSum += marketValueClosed;

                    // caculate the sum of the account value with average prices (averageprice * nominal)
                    ciSum += pr.InvestedValue;

                    // ignore the bid/ask spread if it is greather than 20 % of the ask price
                    if (Math.Abs(askPrice.Value / bidPrice.Value) <= 1.2)
                    {
                        weightedBidAskSpreadSum += weightedBidAskSpread;
                    }

                    if ((account.IsTipp) && (!pr.IsTippAssetRiskProfileIDNull()) && (pr.TippAssetRiskProfileId == (int)TypeTippAssetRiskProfile.High))
                    {
                        if (!pr.IsTippAssetClassCategoryIDNull())
                        {
                            TypeTippAssetClassCategory cat = (TypeTippAssetClassCategory)pr.TippAssetClassCategoryId.Value;
                            if (!tippHighRiskExposureByCategory.ContainsKey(cat))
                                tippHighRiskExposureByCategory[cat] = marketValue;
                            else
                                tippHighRiskExposureByCategory[cat] += marketValue;
                        }
                        if (!pr.IsTippAssetClassIDNull())
                        {
                            int assetClassId = pr.TippAssetClassId.Value;
                            if (!tippHighRiskExposureByAssetClass.ContainsKey(assetClassId))
                                tippHighRiskExposureByAssetClass[assetClassId] = marketValue;
                            else
                                tippHighRiskExposureByAssetClass[assetClassId] += marketValue;
                        }
                    }
                    RowUpdate portfolioUpdate = new RowUpdate(
                        portfolioRowId: pr.PortfolioRowId,
                        instrumentTypeId: pr.InstrumentSnapshot.InstrumentTypeId,
                        closePrice: closePrice.Value,
                        closePriceCcy: closePriceCcy.Value,
                        lastPrice: lastPrice.Value,
                        lastPriceCcy: lastPriceCcy.Value,
                        bidPrice: bidPrice.Value,
                        askPrice: askPrice.Value,
                        priceOffset: priceOffset,
                        priceOffsetClosed: priceOffsetClosed,
                        lastFx: lastFxRate != null ? lastFxRate.Value : 1.0,
                        closeFx: closeFxRate != null ? closeFxRate.Value : 1.0,
                        delta: delta,
                        gamma: gamma,
                        marketValue: marketValue,
                        marketValueClosed: marketValueClosed,
                        investedValue: pr.InvestedValue,
                        marketValueCollateral: marketValueCollateral,
                        tips: prTips
                        );
                    portfolioRowUpdates.Add(portfolioUpdate);
                }

                mvRaw = mvSum - mvCashFee;

                {
                    // eg. calculate the Nominal of the "CASH Fee EUR" position, make sure to add it if not already exists

                    Dictionary<string, List<PriceTip>> prCashFeeTips = new Dictionary<string, List<PriceTip>>();
                    prCashFeeTips["Nominal"] = new List<PriceTip>();

                    if (prCashFee != null)
                        prCashFee.NewNominal = 0.0; // reset

                    if ((!account.IsApplyMgmFeeNull()) && (account.ApplyMgmFee.Value && (!account.IsMgmFeeStartDateNull()) && (account.MgmFeeCalcType == (int)TypeManagementFeeCalculation.Periodic)))
                    {
                        string error = "";
                        //if ((prCashFee == null) && ((prCashFee = SwapUtils.Tools.AddCashFeeRow(account, out error)) == null))
                        //{
                        //    Engine.Instance.Log.Error(methodName + " : could not add CASH Fee to " + account.AccountName + " for reason: " + error);
                        //}
                        if ((prCashFee == null) && ((prCashFee = CashFeeInstruction.CreateNew(0)) == null))
                        {
                            Engine.Instance.Log.Error(methodName + " : could not add CASH Fee to " + account.AccountName + " for reason: " + error);
                        }
                        else
                        {
                            DateTime pricingDate = account.PricingDate; // eg. if account uses T-1 prices then pricing date is business day that immediately precedes today
                            double feePct = 0.0;
                            if (account.ManagementFees1.Count > 0)
                                feePct = account.ManagementFees1[0].FeePct;
                            DateTime lastRebalancingDate = account.MgmFeeStartDate.Value.Date; // we as yet do not keep a history or rebalancing dates but make usage of the startdate to hold the date of the latest rebalancing
                            double dayCountBasis = 1;
                            double dayCount = Math.Max((pricingDate - lastRebalancingDate).Days, 0.0);
                            mvCashFee = -1 * feePct * 0.01 * dayCount / dayCountBasis * mvRaw;

                            string formula = string.Format("= -1 * {0:P2} (Management Fee 1 %) * {1:N0} (= {2} - {3}) / {4:N2} * {5:N2} (Market Value Raw)", feePct * 0.01, dayCount, pricingDate.ToString("dd/MM/yyyy"), lastRebalancingDate.ToString("dd/MM/yyyy"), dayCountBasis, mvRaw);
                            prCashFeeTips["Nominal"].Add(new PriceTip() { mType = TypeManastPriceTip.eMiscellaneous, mDate = null, mSeverity = 0, mName = formula });

                            prCashFee.NewNominal = mvCashFee;
                            mvSum = mvRaw + mvCashFee;
                        }
                    }

                    if (prCashFee != null)
                        prCashFee.prCashFeeTips = prCashFeeTips;
                }

                Dictionary<string, List<PriceTip>> arTips = newAccountRowTips();

                List<int> crossAssetAlerts = null; // instrument types
                double crossAssetAlertMin = 0.0;
                double sum_5_10_40 = 0.0;

                if (account.CrossWeights.Count > 0)
                {
                    CrossWeightSnapshot first = account.CrossWeights[0]; // just take first because is the same for all rows
                    string error = string.Empty;
                    crossAssetAlerts = first.GetInstrumentTypeIds(out error);
                    if (error != string.Empty) Engine.Instance.Log.Error(methodName + " : error '" + error + "' for account '" + account.AccountName + "'");
                    crossAssetAlertMin = first.MinWeight.Value;
                }

                // set the invested weights and the market weights
                foreach (RowUpdate ru in portfolioRowUpdates)
                {
                    double marketWeight, marketWeightCollateral;

                    if ((!account.IsTNANull()) && (account.TNA != 0.0))
                        marketWeight = ru.MarketValue / account.TNA.Value;
                    else
                        marketWeight = ru.MarketValue / mvSum;
                    marketWeightCollateral = (mvCollateralSum != 0.0) ? ru.MarketValueCollateral / mvCollateralSum : 0.0;
                    ru.SetAdditionalProperties(ru.InvestedValue / ciSum, marketWeight, marketWeightCollateral);
                    if ((crossAssetAlerts != null) && (crossAssetAlerts.Contains(ru.InstrumentTypeId)) && (marketWeight > crossAssetAlertMin))
                        sum_5_10_40 += marketWeight;
                }

                // set the invested weights and the market weights
                //foreach (PortfolioRowSnapshot pr in prs)
                //{
                //    double marketWeight, marketWeightCollateral;

                //    if ((!account.IsTNANull()) && (account.TNA != 0.0))
                //        marketWeight = pr.xMarketValue / account.TNA.Value;
                //    else
                //        marketWeight = pr.xMarketValue / mvSum;
                //    marketWeightCollateral = (mvCollateralSum != 0.0) ? pr.xMarketValueCollateral / mvCollateralSum : 0.0;
                //    pr.SetGridProperties2(pr.xInvestedValue / ciSum, marketWeight, marketWeightCollateral);
                //    if ((crossAssetAlerts != null) && (crossAssetAlerts.Contains(pr.InstrumentSnapshot.InstrumentTypeId)) && (marketWeight > crossAssetAlertMin))
                //        sum_5_10_40 += marketWeight;
                //}

                double leverageFactor = (mvBond + mvStock + mvFund + mvETF + mvCertificate + mvFuture) /
                    (mvBond + mvStock + mvFund + mvETF + mvCertificate + mvFuture + mvCash + mvFutureCash);

                if (account.IsTipp)
                {
                    tippVolatility[TypeTippAssetClassCategory.Equity] = null;
                    tippVolatility[TypeTippAssetClassCategory.Bond] = null;

                    if (!account.IsTippManualVolatilityNull())
                    {
                        tippVolatility[TypeTippAssetClassCategory.Equity] = new Price(account.TippManualVolatility.Value, DateTime.Now, null, TypeManastPriceField.eClose);
                    }
                    else
                    {
                        int sicovam = 1;
                        if (sicovam != 0)
                        {
                            IPriceProvider pp = PortfolioPricer.s_pricer[PortfolioPricer.s_defaultProvider];
                            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eTMinus1, null);
                            tippVolatility[TypeTippAssetClassCategory.Equity] = null;
                        }
                    }

                    if (!account.IsTippManualVolatility2Null())
                    {
                        tippVolatility[TypeTippAssetClassCategory.Bond] = new Price(account.TippManualVolatility2.Value, DateTime.Now, null, TypeManastPriceField.eClose);
                    }
                    else
                    {
                        int sicovam = 1;
                        if (sicovam != 0)
                        {
                            IPriceProvider pp = PortfolioPricer.s_pricer[PortfolioPricer.s_defaultProvider];
                            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eTMinus1, null);
                            tippVolatility[TypeTippAssetClassCategory.Bond] = null;
                        }
                    }

                    getTippVolatilityTips(account, "xTippVolEquity", account.TippVolatilityInstrument, account.IsTippManualVolatilityNull(), tippVolatility[TypeTippAssetClassCategory.Equity], arTips);
                    getTippVolatilityTips(account, "xTippVolBond", account.TippVolatilityInstrument2, account.IsTippManualVolatility2Null(), tippVolatility[TypeTippAssetClassCategory.Bond], arTips);
                }

                PricingResult pricingRes = new PricingResult(
                    accountId: account.AccountId,
                    mvSum: mvSum,
                    mvRaw: mvRaw,
                    mvCollateralSum: mvCollateralSum,
                    mvClosedSum: mvClosedSum,
                    weightedBidAskSpreadSum: weightedBidAskSpreadSum,
                    mvCash: mvCash,
                    mvFutureCash: mvFutureCash,
                    mvFuture: mvFuture,
                    mvBond: mvBond,
                    mvStock: mvStock,
                    mvFund: mvFund,
                    mvETF: mvETF,
                    mvCertificate: mvCertificate,
                    isIndexComplete: isIndexComplete,
                    isIndexCloseComplete: isIndexCloseComplete,
                    leverageFactor: leverageFactor,
                    sum_5_10_40: sum_5_10_40,
                    tippHighRiskExposureByCategory: tippHighRiskExposureByCategory,
                    tippHighRiskExposureByAssetClass: tippHighRiskExposureByAssetClass,
                    tippVolatility: tippVolatility,
                    rowUpdates: portfolioRowUpdates,
                    cashFee: prCashFee,
                    arTips: arTips
                    );

                // set the account values
                //account.SetAccountProperties(mvSum, mvRaw, mvCollateralSum, mvSum * account.IndexFactorDouble, mvClosedSum * account.IndexFactorDouble,
                //    weightedBidAskSpreadSum, mvSum, mvCash / mvSum, mvFutureCash / mvSum, mvFuture / mvSum, mvBond / mvSum, mvStock / mvSum,
                //    mvFund / mvSum, mvETF / mvSum, mvCertificate / mvSum, isIndexComplete,
                //    isIndexCloseComplete, leverageFactor, sum_5_10_40, tippVolatility, tippHighRiskExposureByCategory, tippHighRiskExposureByAssetClass, arTips);

                DateTime endTime = DateTime.Now;
                TimeSpan elapsTime = endTime - startTime;
                if (elapsTime.TotalSeconds > 1) // log only when calculation takes more than 1 second 
                    Engine.Instance.Log.Info(methodName + " : calculated account '" + account.AccountName + "' in " + elapsTime.ToString() + " (hh:mi:ss.ms) time");

                //return !double.IsNaN(mvSum);
                return pricingRes;
            }
            catch (Exception e)
            {
                Engine.Instance.Log.Error(methodName + " : exception caught '" + e.Message + "' for account '" + account.AccountName + "'");
                Engine.Instance.Log.Info(e);
            }
            return null;
        }

        public static double addPerformanceFee(SwapAccount account, DateTime now, SwapAccountPortfolio realCash, double feePct, double closeIndexValue, double closeIndexFactor)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Engine.Instance.Log.Info(methodName + " : calculating performance fee for '" + account.AccountName + "' on '" + now.ToString("dd/MM/yyyy") + "' ..");

            if (account.HighWaterMark == 0.0)
                account.HighWaterMark = account.InitialIndexValueDouble;

            Engine.Instance.Log.Info(methodName + " : current high water mark (index value) equals " + account.HighWaterMark.ToString("R"));

            if (closeIndexValue <= account.HighWaterMark)
                return 0.0;

            if (closeIndexFactor == 0.0)
                return 0.0;

            double closeNbrOfCertificates = 1 / closeIndexFactor;
            double cash = (closeIndexValue - account.HighWaterMark) * account.PerformanceFee * closeNbrOfCertificates;
            //realCash.RealizedPL += -cash;
            string feeDetails = "Perf fee on '" + now.ToString("dd.MM.yyyy") + "' equals " + cash.ToString("N") + " = (" + closeIndexValue.ToString("N") + " - " + account.HighWaterMark.ToString("N") + ") * " + account.PerformanceFee + " * " + closeNbrOfCertificates.ToString("N");

            Engine.Instance.Log.Info(methodName + " : added performance fee " + (-1 * cash).ToString("N") + " " + realCash.Currency + " to '" + realCash.xInstrumentName + "' for '" + account.AccountName + "' to execute on '" + now.Date.ToString("dd/MM/yyyy") + "'");
            Engine.Instance.Log.Info(methodName + " : " + feeDetails);

            double nominalBefore = closeIndexValue * closeNbrOfCertificates; // nominal before performance fee
            double indexValueAfter = (nominalBefore - cash) / closeNbrOfCertificates; // index value after performance fee

            Engine.Instance.Log.Info(methodName + " : new high water mark (index value) for account '" + account.AccountName + "' becomes " + indexValueAfter.ToString("R"));
            account.HighWaterMark = indexValueAfter;

            return cash;
        }

        public static double addManagementFee(SwapAccount account, DateTime now, SwapAccountPortfolio realCash, List<SMgmtFeeRow> feeRows, double closeIndexValue, double closeIndexFactor, int which)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            // calculate the mangement fee and add it to the cash instrument

            Engine.Instance.Log.Info(methodName + " : calculating management fee " + which + " for '" + account.AccountName + "' on '" + now.ToString("dd/MM/yyyy") + "' ..");

            if (closeIndexFactor == 0.0)
                return 0.0;

            double closeNbrOfCertificates = 1 / closeIndexFactor;
            double accountValue = closeIndexValue * closeNbrOfCertificates;
            double cash = 0.0;
            double dayCountBasis = 1;

            // eg. { (0,1%), (5,2%), (20,3%), (50,4%) } and assume value is 22 then the fee becomes (5*1%) + (15*2%) + (2*3%) + (0*4%)

            double remainder = accountValue;
            for (int k = feeRows.Count - 1; k >= 0; k--)
            {
                SMgmtFeeRow row = feeRows[k];
                if (remainder > row.mValue)
                {
                    cash += (remainder - row.mValue) * row.mFeePct * 0.01 / dayCountBasis;
                    remainder = row.mValue;
                }
            }

            // subtract the management fee from the realized P/L of the cash
            //realCash.RealizedPL += -cash;

            // create a pay managementFee event and set it on executed = now, because the management fee is calculated now on the portfolio
            string feeDetails = string.Empty;
            if (feeRows.Count == 1)
                feeDetails = "Management fee " + which + " on '" + now.ToString("dd.MM.yyyy") + "' equals " + cash.ToString("N") + " = " + closeIndexValue.ToString("N") + " * " + closeNbrOfCertificates.ToString("N") + " * " + feeRows[0].mFeePct + "% / " + dayCountBasis.ToString("N");
            else
                feeDetails = "Management fee " + which + " on '" + now.ToString("dd.MM.yyyy") + "' equals " + cash.ToString("N") + ", calculated on " + accountValue.ToString("N") + " = " + closeIndexValue.ToString("N") + " * " + closeNbrOfCertificates.ToString("N");

            TypeManastEvent feeType = TypeManastEvent.ManagementFee1;
            if (which == 1)
                feeType = TypeManastEvent.ManagementFee1;
            else if (which == 2)
                feeType = TypeManastEvent.ManagementFee2;
            else if (which == 3)
                feeType = TypeManastEvent.ManagementFee3;

            Engine.Instance.Log.Info(methodName + " : added management fee " + which + " of " + (-1 * cash).ToString("N") + " " + realCash.Currency + " to '" + realCash.xInstrumentName + "' for '" + account.AccountName + "' to execute on '" + now.Date.ToString("dd/MM/yyyy") + "'");
            Engine.Instance.Log.Info(methodName + " : " + feeDetails);

            return cash;
        }

        public static double addManagementFee(SwapAccount account, DateTime now, SwapAccountPortfolio realCash, double feePct, double closeIndexValue, double closeIndexFactor, int which)
        {
            return 0.0;
        }

        public static double getEventAmount(SwapAccountPortfolio pr, TypeManastEvent eventType, DateTime d)
        {
            double sum = 0.0;
            foreach (SwapEvent er in pr.EventRowsByPortfolioSource)
            {
                if (((TypeManastEvent)er.EventType == eventType) && (er.ExecutionDate.Date == d.Date))
                    sum = sum + er.Nominal;
            }
            return sum;
        }

        public struct SMgmtFeeRow
        {
            public SMgmtFeeRow(double value, double feePct) { mValue = value; mFeePct = feePct; }
            public double mValue;
            public double mFeePct;
        }

        public static List<SMgmtFeeRow> getFeeRows(SwapAccount account, int which)
        {
            List<SMgmtFeeRow> feeRows = new List<SMgmtFeeRow>();
            if (which == 1)
            {
                foreach (SwapManagementFee1 fee in account.GetManagementFee1Rows())
                    feeRows.Add(new SMgmtFeeRow(fee.Value, fee.FeePct));
            }
            if (which == 2)
            {
                foreach (SwapManagementFee2 fee in account.GetManagementFee2Rows())
                    feeRows.Add(new SMgmtFeeRow(fee.Value, fee.FeePct));
            }
            if (which == 3)
            {
                foreach (SwapManagementFee3 fee in account.GetManagementFee3Rows())
                    feeRows.Add(new SMgmtFeeRow(fee.Value, fee.FeePct));
            }

            feeRows.Sort((r1, r2) => r1.mValue.CompareTo(r2.mValue)); // sort on "value" ASC
            return feeRows;
        }

        public static void ApplyFees(SwapAccount account, DateTime now)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                bool bookManagementFees = ((account.GetManagementFee1Rows().Count > 0) && (account.MgmFeeCalcType == (int)TypeManagementFeeCalculation.Accrued));
                bool bookPerformanceFee = (account.PerformanceFee != 0.0);

                // return if no fee has to be calculated
                //if ((account.GetManagementFee1Rows().Length == 0) && (account.PerformanceFee == 0.0))
                if ((!bookManagementFees) && (!bookPerformanceFee))
                    return;

                // return if too early to book fees because index snapshot has not yet happened
                if ((now.Date == DateTime.Now.Date) && (System.DateTime.Compare(account.IndexSnapShotSaved.Date, now.Date) < 0))
                    return; // too early to book today's fees

                // get cash instrument
                SwapAccountPortfolio realCash = null;
                foreach (SwapAccountPortfolio pr in account.AccountPortfolioRows)
                {
                    if (pr.Instrument.InstrumentType.IsCash && !pr.Instrument.IsTemporalCash && pr.Currency == account.Currency)
                    {
                        realCash = pr;
                        break;
                    }
                }
                if (realCash == null)
                    return;

                // look up index value in account history on most recent business day that is equal to - or precedes "now"

                DateTime lastBusinessDay = account.GetMatchingBusinessDay(now, true, true);
                DateTime foundDate = DateTime.MinValue;
                double closeIndexValue = 0.0;
                double closeIndexFactor = 0.0;
                if (true)
                {
                    Engine.Instance.Log.Info(methodName + " : take the index value " + closeIndexValue.ToString("R") + " (found on " + foundDate.Date.ToString("dd/MM/yyyy") + ") from the account history to calculate the fees on " + now.Date.ToString("dd/MM/yyyy"));
                }
                else
                {
                    Engine.Instance.Log.Info(methodName + " : taking the initial index value " + account.InitialIndexValue.ToString("R") + " to calculate the fees on " + now.Date.ToString("dd/MM/yyyy") + " because did not find index value in history");
                    closeIndexValue = account.InitialIndexValueDouble;
                }

                // calculate the performance fee and add it to the cash instrument

                if (bookPerformanceFee)
                {
                    if ((lastBusinessDay.Date == now.Date) && (foundDate.Date == now.Date)) // calculate performance fee only on business days and provided the index value is available in the history
                    {
                        addPerformanceFee(account, now, realCash, account.PerformanceFee, closeIndexValue, closeIndexFactor);
                        //account.xHasHadEventExecutedWithLastRun = true;
                    }
                    else
                    {
                        // Say close on Fri is 110 and HighWaterMark is 100. During the night, a Perf Fee is calculated on (110 - 100) and the new HWM becomes eg 108 (= 110 - Perf Fee). Fine.
                        // However, on Sat a new Perf Fee will be calculated on (110 - 108) since the close on Sat (normally 108) is in fact taken from Fri (110), because MANAST is
                        // not running on Sat. The same for Sun. We avoid this by calculating a Perf Fee for a given date D only if there is a close on D.
                        Engine.Instance.Log.Info(methodName + " : will not calculate perf fee since did not find index value on " + now.Date.ToString("dd/MM/yyyy") + ") in the account history");
                    }
                }

                // calculate the mangement fees and add them to the cash instrument

                if (!bookManagementFees)
                    return;

                double nominalBeforePerformanceFee = closeIndexValue / closeIndexFactor; // nominal before performance fee
                double performanceFee = -1 * getEventAmount(realCash, TypeManastEvent.PerformanceFee, foundDate); // performance fee on the date of the close
                double indexValueAfterPerformanceFee = (nominalBeforePerformanceFee - performanceFee) * closeIndexFactor;

                if (performanceFee != 0.0)
                    Engine.Instance.Log.Info(methodName + " : index value goes from " + closeIndexValue.ToString("R") + " to " + indexValueAfterPerformanceFee.ToString("R") + " following performance fee");

                if (foundDate.Date != lastBusinessDay.Date)
                { // show error in trace window to warn trader about missing index snapshot
                    string errorString = string.Format("Index history missing on date '{0:d}' for {1}", lastBusinessDay.ToString("dd.MM.yyyy"), account.AccountName);
                    Engine.Instance.Log.Warn(methodName + " : " + errorString);
                    // TODO aner check traces
                    //Logger.TraceMessage(Logger.ElogLevel.ERROR, errorString);
                }

                Engine.Instance.Log.Info(methodName + " : will use index value " + indexValueAfterPerformanceFee.ToString("R") + " from " + foundDate.Date.ToString("dd/MM/yyyy") + " to calculate the management fees on " + now.Date.ToString("dd/MM/yyyy"));

                double managementFeeCash = 0.0;
                if (account.GetManagementFee1Rows().Count > 0)
                {
                    List<SMgmtFeeRow> feeRows = getFeeRows(account, 1);
                    managementFeeCash = managementFeeCash + addManagementFee(account, now, realCash, feeRows, indexValueAfterPerformanceFee, closeIndexFactor, 1);
                }

                if (account.GetManagementFee2Rows().Count > 0)
                {
                    List<SMgmtFeeRow> feeRows = getFeeRows(account, 2);
                    managementFeeCash = managementFeeCash + addManagementFee(account, now, realCash, feeRows, indexValueAfterPerformanceFee, closeIndexFactor, 2);
                }

                if (account.GetManagementFee3Rows().Count > 0)
                {
                    List<SMgmtFeeRow> feeRows = getFeeRows(account, 3);
                    managementFeeCash = managementFeeCash + addManagementFee(account, now, realCash, feeRows, indexValueAfterPerformanceFee, closeIndexFactor, 3);
                }
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error(methodName + " : exception caught '" + ex.Message + "'");
            }
            finally
            {
                Engine.Instance.Log.Info(methodName + " : calculated fees for '" + account.AccountName + "' on '" + now.ToString("dd/MM/yyyy") + "'");
            }
        }

        /// <summary>
        /// Applies the interest rate on the cash of an account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="now">The current date.</param>
        /// <param name="db">The AccountDataset database.</param>
        /// <returns></returns>
        /*! \callergraph */
        public static bool ApplyIR(SwapAccount account, DateTime now)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                IEnumerable<SwapAccountPortfolio> prs = account.AccountPortfolioRows;

                foreach (SwapAccountPortfolio pr in prs)
                {
                    if (!pr.Instrument.IsRealCash)
                        continue;
                    SwapAccountPortfolio cashTarget = pr.PortfolioRowParent;

                    bool alreadyBooked = false; // interests already booked ?
                    foreach (SwapEvent ev in pr.Account.GetSortedEventRows(true))
                    {
                        if (((TypeManastEvent)ev.EventType == TypeManastEvent.Interest && now.Date == ev.ExecutionDate.Date && pr == ev.PortfolioRowByPortfolioSource && cashTarget == ev.PortfolioRowByPortfolioTarget) || now.Date == DateTime.Now.Date)
                        {
                            alreadyBooked = true;
                            break;
                        }
                    }
                    if (alreadyBooked)
                        continue;

                    DateTime calculationDate = pr.Account.GetMatchingBusinessDay(now, true, true);

                    Price reutersRate = new Price();
                    Price rate = new Price();
                    double interestRate = 0.0;
                    bool floorAtZero = false;

                    if (true)
                    {
                        SwapAccountInstrument ir = pr.InstrumentByPositiveRateLink;
                        if (pr.IspRateSourceIDNull())
                            continue;

                        if (pr.hasManualPositiveRate())
                        {
                            reutersRate.Value = pr.xPRateManual * 100.0;
                            reutersRate.Updated = DateTime.Now;
                        }
                        else
                        {
                            if (now.Date != System.DateTime.Now.Date)
                            {
                                reutersRate = GetHistoricPrice(new InstrumentRowSnapshot(pr.InstrumentByPositiveRateLink), now);
                                if (Double.IsNaN(reutersRate.Value))
                                {
                                    Engine.Instance.Log.Error(methodName + " : failed to calculate interests for '" + pr.Instrument.InstrumentName + "' for '" + account.AccountName + "' on date '" + now.Date.ToString("dd/MM/yyyy") + "' because rate is missing");
                                    continue;
                                }
                            }
                            else
                            {
                                if (!FetchPrice(new InstrumentRowSnapshot(ir), string.Empty, out reutersRate, account))
                                    continue;
                            }
                        }
                        interestRate = reutersRate.Value / 100.0; // Reuters reports 4.97 for 4.97%, therefore divide by 100
                        interestRate *= pr.PRateSourceFactor.Value;
                        interestRate += pr.PRateSourceOffset.Value;
                        rate = new Price(interestRate, reutersRate.Updated);
                        floorAtZero = pr.IspRateFloorAtZeroNull() || pr.pRateFloorAtZero.Value;
                    }
                    else
                    {
                        SwapAccountInstrument ir = pr.InstrumentByNegativeRateLink;
                        if (pr.IsnRateSourceIDNull())
                            continue;
                        if (pr.hasManualNegativeRate())
                        {
                            reutersRate.Value = (pr.NRateManual.HasValue ? pr.NRateManual.Value : 0) * 100.0;
                            reutersRate.Updated = DateTime.Now;
                        }
                        else
                        {
                            if (now.Date != System.DateTime.Now.Date)
                            {
                                reutersRate = GetHistoricPrice(new InstrumentRowSnapshot(pr.InstrumentByNegativeRateLink), now);
                                if (Double.IsNaN(reutersRate.Value))
                                {
                                    Engine.Instance.Log.Error(methodName + " : failed to calculate interests for '" + pr.Instrument.InstrumentName + "' for '" + account.AccountName + "' on date '" + now.Date.ToString("dd/MM/yyyy") + "' because rate is missing");
                                    continue;
                                }
                            }
                            else
                            {
                                if (!FetchPrice(new InstrumentRowSnapshot(ir), string.Empty, out reutersRate, account))
                                    continue;
                            }
                        }
                        interestRate = reutersRate.Value / 100.0;
                        interestRate *= pr.NRateSourceFactor.Value;
                        interestRate += pr.NRateSourceOffset.Value;
                        rate = new Price(interestRate, reutersRate.Updated);
                        floorAtZero = pr.IsnRateFloorAtZeroNull() || pr.nRateFloorAtZero.Value;
                    }

                    if (floorAtZero)
                        interestRate = Math.Max(0.0, interestRate);

                    if (interestRate == 0.0)
                    {
                        Price noRate = new Price(0.0, DateTime.Now);
                        continue;
                    }

                    if (!pr.CheckCashTargetAviability("Interest not paid."))
                        continue;

                    double interestAmount = 1 * interestRate / 360.0;
                }

                return true;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error(methodName + " : exception caught '" + ex.Message + "' for '" + account.AccountName + "' on date '" + now.Date.ToString("dd/MM/yyyy") + "'");
                Engine.Instance.Log.Info(ex);
            }
            return false;
        }

        public static bool AddInterestsForFutures(SwapAccount account, DateTime now)
        // TO DO : rewrite this function like function "ApplyIR"
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                Price noRate = new Price(0.0, DateTime.Now);

                Price d;
                IEnumerable<SwapAccountPortfolio> prs = account.AccountPortfolioRows;

                s_ir.Clear();
                foreach (SwapAccountPortfolio pr in prs)
                {
                    if (!pr.Instrument.InstrumentType.IsFuture)
                        continue;

                    double marketValueAtDate = pr.xMarketValue;
                    if (marketValueAtDate == 0.0)
                        continue;

                    d = new Price();
                    double tmpInterestRate;
                    SwapAccountInstrument ir;

                    if (marketValueAtDate > 0.0)
                    {
                        if (pr.IspRateSourceIDNull())
                            continue; // rate source is missing or "none"
                        if (s_ir.ContainsKey(pr.pRateSourceID.Value))
                            continue; // already fetched ?
                        ir = pr.InstrumentByPositiveRateLink;
                        if (now.Date != System.DateTime.Now.Date)
                        {
                            d = GetHistoricPrice(new InstrumentRowSnapshot(ir), now);
                        }
                        else
                        {
                            FetchPrice(new InstrumentRowSnapshot(ir), string.Empty, out d, account);
                        }
                        tmpInterestRate = d.Value / 100.0;
                        tmpInterestRate *= pr.PRateSourceFactor.Value;
                        tmpInterestRate += pr.PRateSourceOffset.Value;
                        d = new Price(tmpInterestRate, d.Updated);
                        s_ir.Add(pr.pRateSourceID.Value, d);
                    }
                    else
                    {
                        if (pr.IsnRateSourceIDNull())
                            continue;
                        if (s_ir.ContainsKey(pr.nRateSourceID.Value))
                            continue;
                        ir = pr.InstrumentByNegativeRateLink;
                        if (now.Date != System.DateTime.Now.Date)
                        {
                            d = GetHistoricPrice(new InstrumentRowSnapshot(ir), now);
                        }
                        else
                        {
                            FetchPrice(new InstrumentRowSnapshot(ir), string.Empty, out d, account);
                        }
                        tmpInterestRate = d.Value / 100.0;
                        tmpInterestRate *= pr.NRateSourceFactor.Value;
                        tmpInterestRate += pr.NRateSourceOffset.Value;
                        d = new Price(tmpInterestRate, d.Updated);
                        s_ir.Add(pr.nRateSourceID.Value, d);
                    }
                }

                foreach (SwapAccountPortfolio pr in prs)
                {
                    if (!pr.Instrument.InstrumentType.IsFuture)
                        continue;

                    double marketValueAtDate = pr.xMarketValue;
                    if (marketValueAtDate == 0.0)
                        continue;

                    int sourceId = 0;
                    SwapAccountInstrument ir;

                    if (marketValueAtDate > 0)
                    {
                        if (pr.IspRateSourceIDNull())
                            continue; // rate source is missing or "none"
                        ir = pr.InstrumentByPositiveRateLink;
                        sourceId = pr.pRateSourceID.Value;
                    }
                    else
                    {
                        if (pr.IsnRateSourceIDNull())
                            continue;
                        ir = pr.InstrumentByNegativeRateLink;
                        sourceId = pr.nRateSourceID.Value;
                    }

                    double interestRate = s_ir[sourceId].Value;
                    if (interestRate == 0.0)
                    {
                        string errorString = "failed to get interest rate of '" + ir.InstrumentName + "' for '" + pr.Instrument.InstrumentName + "' part of '" + account.AccountName + "' on '" + now.Date.ToString("dd/MM/yyyy") + "'";
                        Engine.Instance.Log.Error(methodName + " : " + errorString);
                        continue;
                    }

                    if (!pr.CheckCashTargetAviability("Interest not paid."))
                        continue;

                    SwapAccountPortfolio cashTarget = pr.PortfolioRowParent;
                    double interest = marketValueAtDate * interestRate / 360.0;
                }

                return true;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Info(ex);
                return false;
            }
        }

        public static void calculateNominal(DateTime today, bool doCash, bool doOtherThanCash)
        {
        }

        public static void calculateNominal(SwapAccount ar, DateTime today, bool doCash, bool doOtherThanCash)
        {
            if (!ar.xBookEvents)
                return; // for some accounts we do not book events, so we do not want the cash nominal to be calculated from those events either, as this would give zero
            Parallel.ForEach(ar.AccountPortfolioRows,
                pr =>
                {
                    bool isCash = pr.Instrument.InstrumentType.IsCash;
                    if (((isCash) && (doCash)) || ((!isCash) && (doOtherThanCash)))
                    {
                        calculateNominal(ar, pr, today);
                    }
                }
                );
        }

        public static void calculateNominal(SwapAccount ar, SwapAccountPortfolio pr, DateTime today)
        {
            const double ZERO = 1E-6;

            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            double oldNominal = pr.Nominal;

            DateTime onDate = getNominalCalculationDate(ar, today);
            if (pr.Instrument.InstrumentType.IsCash)
            {
            }
            else
            {
                if (pr.Instrument.InstrumentType.IsCashFee)
                {
                    return; // Nominal is calculated from neither orders nor events
                }
                else
                {
                    double nominal, averagePrice;
                    getNominalOfStock(ar, pr, onDate, out nominal, out averagePrice);
                    pr.Nominal = nominal;
                    pr.AveragePrice = averagePrice;
                }
            }

            double newNominal = pr.Nominal;

            if (Math.Abs(oldNominal - newNominal) > ZERO)
                Engine.Instance.Log.Info(methodName + " : change (old: " + oldNominal + ", new: " + newNominal + ") of '" + pr.Instrument.InstrumentName + "' for '" + pr.Account.AccountName + "'");
        }

        public static DateTime getNominalCalculationDate(SwapAccount ar, DateTime today)
        // return "today" or the "valuation date", depending on the user preference
        {
            DateTime date = new DateTime();
            date = today;
            if (true)
                date = ar.getPricingDate(today); // eg. if account uses T-1 prices then pricing date is business day that immediately precedes today
            return date;
        }

        public static void calculateCertificates(DateTime today)
        {
        }

        public static void calculateCertificates(SwapAccount ar, DateTime today)
        {
            ar.IndexFactor = (decimal)getIndexFactorWrapper(ar, today);
        }

        public static double getIndexFactorWrapper(SwapAccount ar, DateTime today)
        {
            double factor = 0.0;
            if (true)
                factor = getIndexFactor(ar, today);
            else
                factor = ar.IndexFactorDouble; // trader manually mantains the number of certificates
            return factor;
        }

        public static double getIndexFactor(SwapAccount ar, DateTime today)
        // calculate index factor (= 1 / number of certificates) from the "CreationOrRedemption" orders
        {
            const int APOBANK_RENTEN_PRIVAT = 11351414; // HVB_MANAST_ACCOUNT.id
            const int APOBANK_AKTIEN_PRIVAT = 11351415;
            const int APOBANK_DEFENSIV_PRIVAT = 11351416;

            //DateTime today = DateTime.Now;
            DateTime onDate = getNominalCalculationDate(ar, today);

            double factor = 0.0;
            DateTime startDate = DateTime.MinValue;

            // - for the 3 ApoBank accounts of the MA database, the number of certificates cannot simply be calculated from the orders of type "CreationOrRedemption"
            // - instead should we also consider events of type ManagementFee1 with action type "OnlyDeltaAdjustment"
            // - because this would complicate things and because this action type has been abandoned since 2015, we prefer to hardcode the start levels
            switch (ar.Id)
            {
                case APOBANK_RENTEN_PRIVAT:
                    factor = 0.0000029559479149092; // on Jan 1, 2015 the number of certificates equals 338,300.9541, taken from the index history
                    startDate = new DateTime(2015, 01, 01);
                    break;
                case APOBANK_AKTIEN_PRIVAT:
                    factor = 0.00000443986034672782;
                    startDate = new DateTime(2015, 01, 01);
                    break;
                case APOBANK_DEFENSIV_PRIVAT:
                    factor = 0.000001884292884676;
                    startDate = new DateTime(2015, 01, 01);
                    break;
                default:
                    factor = 0.0;
                    break;
            }

            foreach (SwapOrder or in ar.GetSortedOrderRows(true, true))
            {
                if (!or.DateExecuted.HasValue)
                    continue;
                if (or.OrderType != TypeManastOrder.CreationOrRedemption)
                    continue;
                if (or.TradeDate.Date < startDate.Date)
                    continue;
                if (or.TradeDate.Date > onDate.Date)
                    break;

                double oldNbrOfCertificates = (factor != 0.0) ? (1.0 / factor) : (0.0);
                double newNbrOfCertificates = oldNbrOfCertificates + or.CertificatesOrZero;
                factor = (newNbrOfCertificates != 0.0) ? (1.0 / newNbrOfCertificates) : (0.0);
            }
            return factor;
        }

        public static double getIndexFactorOld(SwapAccount ar)
        // calculate index factor (= 1 / number of certificates) from the "CreationOrRedemption" orders
        {
            const int APOBANK_RENTEN_PRIVAT = 11351414; // HVB_MANAST_ACCOUNT.id
            const int APOBANK_AKTIEN_PRIVAT = 11351415;
            const int APOBANK_DEFENSIV_PRIVAT = 11351416;

            DateTime today = DateTime.Now;
            DateTime onDate = getNominalCalculationDate(ar, today);

            double factor = 0.0;
            DateTime startDate = DateTime.MinValue;

            // - for the 3 ApoBank accounts of the MA database, the number of certificates cannot simply be calculated from the orders of type "CreationOrRedemption"
            // - instead should we also consider events of type ManagementFee1 with action type "OnlyDeltaAdjustment"
            // - because this would complicate things and because this action type has been abandoned since 2015, we prefer to hardcode the start levels
            switch (ar.Id)
            {
                case APOBANK_RENTEN_PRIVAT:
                    factor = 0.0000029559479149092; // on Jan 1, 2015 the number of certificates equals 338,300.9541, taken from the index history
                    startDate = new DateTime(2015, 01, 01);
                    break;
                case APOBANK_AKTIEN_PRIVAT:
                    factor = 0.00000443986034672782;
                    startDate = new DateTime(2015, 01, 01);
                    break;
                case APOBANK_DEFENSIV_PRIVAT:
                    factor = 0.000001884292884676;
                    startDate = new DateTime(2015, 01, 01);
                    break;
                default:
                    factor = 0.0;
                    break;
            }

            bool first = true;

            foreach (SwapOrder or in ar.GetSortedOrderRows(true, true))
            {
                if (!or.DateExecuted.HasValue)
                    continue;
                if (or.OrderType != TypeManastOrder.CreationOrRedemption)
                    continue;
                if (or.TradeDate.Date < startDate.Date)
                {
                    first = false;
                    continue;
                }
                if (or.TradeDate.Date > onDate.Date)
                    break;

                double orderSum = 1;

                if (first)
                {
                    first = false;
                    factor = ar.InitialIndexValueDouble / orderSum;
                }
                else
                {
                    // for example, say current market value is 100 EUR (oldSum) and index is 50 (order.IndexValue) and so index factor is 0.5 (idxFactorBeforeOrder) and 10 EUR fresh money comes in (orderSum)
                    // then the new index factor = 50/110 = (50/100)x(100/110) = current index factor x 100/(100+10)

                    double oldSum = Convert.ToDouble(or.IndexValue) / factor;
                    factor *= oldSum / (oldSum + orderSum);
                }
            }
            return factor;
        }

        public static double getNominalOfStock(SwapAccount ar, SwapAccountPortfolio pr, DateTime today, bool adjustDate)
        {
            DateTime onDate = today;
            if (adjustDate)
                onDate = getNominalCalculationDate(ar, today);
            double nominal, dummy;
            getNominalOfStock(ar, pr, onDate, out nominal, out dummy);
            return nominal;
        }


        // Helper: avoid allocating new DateTime via .Date repeatedly.
        // This returns the day number since 0001-01-01 (no allocations).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int DayNumber(DateTime dt) => (int)(dt.Ticks / TimeSpan.TicksPerDay);


        private static void getNominalOfStock(SwapAccount ar, SwapAccountPortfolio pr, DateTime onDate, out double nominal, out double averagePrice)
        // get nominal of position other than cash, eg. stock, bond, .. For cash positions, one has to call "GetNominalOfCash"
        {
            const double ZERO = 1E-6;

            nominal = 0.0;
            averagePrice = 0.0;

            List<TypeManastEvent> correctionEventTypes = new List<TypeManastEvent>();
            correctionEventTypes.Add(TypeManastEvent.StockSplit);
            correctionEventTypes.Add(TypeManastEvent.DividendReinvest);

            List<SwapEvent> stockSplits = pr.getSortedEventRowsOfType(correctionEventTypes); // sorted on execution date
            List<OrderSnapshotForNominalOfStock> orders = ar.GetOrderSnapshotRowsForNominalOfStock(true, true); // sorted on trade date

            int i = 0;
            int j = 0;

            int onDateDayNumber = DayNumber(onDate);

            while ((i < stockSplits.Count) && (DayNumber(stockSplits[i].ExecutionDate) <= onDateDayNumber) && (j < orders.Count) && (DayNumber(orders[j].TradeDate) <= onDateDayNumber))
            {
                if (!stockSplits[i].Executed.HasValue)
                {
                    ++i;
                    continue; // skip stock split if not executed
                }
                if (!orders[j].DateExecuted.HasValue)
                {
                    ++j;
                    continue; // skip order if not executed
                }
                if (DayNumber(stockSplits[i].ExecutionDate) <= DayNumber(orders[j].TradeDate))
                {
                    SplitAveragePrice(stockSplits[i].Nominal, ref nominal, ref averagePrice);
                    ++i;
                }
                else
                {
                    var trades = orders[j].TradeSnapshotRows;
                    int tradeRowsCount = trades.Count;
                    int prInstrumentId = pr.Instrument.Id;

                    for (int t = 0; t < tradeRowsCount; ++t)
                    {
                        var tr = trades[t];
                        if (tr.InstrumentId != prInstrumentId)
                            continue;
                        UpdateAveragePrice(pr.Instrument.InstrumentType.IsBond, tr, nominal, ref averagePrice);
                        nominal += tr.Nominal;
                    }
                    ++j;
                }
            }
            while ((i < stockSplits.Count) && (DayNumber(stockSplits[i].ExecutionDate) <= onDateDayNumber))
            {
                if (!stockSplits[i].Executed.HasValue)
                {
                    ++i;
                    continue;
                }
                SplitAveragePrice(stockSplits[i].Nominal, ref nominal, ref averagePrice);
                ++i;
            }
            while ((j < orders.Count) && (DayNumber(orders[j].TradeDate) <= onDateDayNumber))
            {
                if (!orders[j].DateExecuted.HasValue)
                {
                    ++j;
                    continue;
                }
                var trades = orders[j].TradeSnapshotRows;
                int tradeRowsCount = trades.Count;
                int prInstrumentId = pr.Instrument.Id;
                for (int t = 0; t < tradeRowsCount; ++t)
                {
                    var tr = trades[t];
                    if (tr.InstrumentId != prInstrumentId)
                        continue;
                    UpdateAveragePrice(pr.Instrument.InstrumentType.IsBond, tr, nominal, ref averagePrice);
                    nominal += tr.Nominal;
                }
                ++j;
            }

            // sometimes the calculated Nominal is 0.000000001 or so, so different from 0.0, causing these positions (mistakenly) to be taken into consideration when MANAST calculates the account
            if (Math.Abs(nominal) < ZERO)
                nominal = 0.0;
            if (Math.Abs(averagePrice) < ZERO)
                averagePrice = 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SplitAveragePrice(double splitFactor, ref double nominal, ref double averagePrice)
        {
            // say we had 100 stocks at average price 5$ and the split is 2 for 1 (= splitFactor 0.5), then we end up with 200 stocks at 2.5$
            averagePrice *= splitFactor;
            nominal = nominal / splitFactor;
        }

        private static void UpdateAveragePrice(bool isBond, TradeSnapshotRow tr, double nominal, ref double averagePrice)
        // return the Sophis so-called "weighted average price (WAP)"
        {
            double currentNominal = nominal; // for stocks, "Nominal" holds the number of stocks
            double currentAvgPrice = averagePrice;

            double f = isBond ? 100.0 : 1.0;
            double tradeNominal = tr.Nominal;

            if (currentNominal * tradeNominal > 0)
            {
                double s = tradeNominal > 0 ? 1.0 :
                           tradeNominal < 0 ? -1.0 : 0.0;
                // note : the accrued is in the same currency as the price, wherease the fee is already in the currency of the account
                double tradeAmount = (tr.Price / f * tradeNominal + tr.Accrued * s) / tr.FXRate + tr.Fee;
                // say we are long 100 at 1$ and purchase an additional 100 at 1.5$, then avg price is (100*1$+100*1.5$)/(100+100) = 1.25$
                averagePrice = ((currentNominal * currentAvgPrice) + (tradeAmount * f)) / (currentNominal + tradeNominal);
            }
            else
            {

                double absCurrent = currentNominal >= 0 ? currentNominal : -currentNominal;
                double absTrade = tradeNominal >= 0 ? tradeNominal : -tradeNominal;

                if (absCurrent >= absTrade)
                {
                    // say we are long 100 at 1$ and sell -70 at 1.5$, then we realise 70 at 1.5$ and remain with 30 at 1$, hence avg price = 1$
                    averagePrice = currentAvgPrice;
                }
                else
                {
                    double tradePrice = tr.Price / f / tr.FXRate; // convert trade price (eg. in USD) into currency of account (eg. EUR)
                    // say we are long 100 at 1$ and sell -300 at 1.5$, then we realise 100 at 1.5 and sell an additional 200 at 1.5$, hence avg price = 1.5$
                    averagePrice = tradePrice;
                }
            }
        }

        public static void calculateFeeColumns(SwapAccount ar)
        {
            foreach (SwapAccountPortfolio pr in ar.AccountPortfolioRows)
            {
                if (!pr.Instrument.IsRealCash)
                    continue;
                PortfolioPricer.calculateFeeColumns(pr);
            }
        }

        public static void calculateFeeColumns(SwapAccountPortfolio pr)
        {
            if (!pr.Instrument.IsRealCash)
                return;

            DateTime pricingDate = pr.Account.getPricingDate(DateTime.Now.Date);

            pr.xSumMgmtFee1 = pr.xSumMgmtFee2 = pr.xSumMgmtFee3 = pr.xSumPerfFee = 0.0;
            pr.xSumMgmtFeePerUnit1 = pr.xSumMgmtFeePerUnit2 = pr.xSumMgmtFeePerUnit3 = pr.xSumPerfFeePerUnit = 0.0;

            foreach (SwapEvent er in pr.EventRowsByPortfolioSource)
            {
                if (er.ExecutionDate.Date <= pricingDate.Date)
                {
                    double fee = er.Nominal;
                    double feePerUnit = (er.Basis != 0.0) ? (er.Nominal / er.Basis) : (0.0); // field Basis holds the number of certificates that was used to calculate the Nominal

                    switch (er.EventType)
                    {
                        case (int)TypeManastEvent.ManagementFee1:
                            pr.xSumMgmtFee1 += fee;
                            pr.xSumMgmtFeePerUnit1 += feePerUnit;
                            break;
                        case (int)TypeManastEvent.ManagementFee2:
                            pr.xSumMgmtFee2 += fee;
                            pr.xSumMgmtFeePerUnit2 += feePerUnit;
                            break;
                        case (int)TypeManastEvent.ManagementFee3:
                            pr.xSumMgmtFee3 += fee;
                            pr.xSumMgmtFeePerUnit3 += feePerUnit;
                            break;
                        case (int)TypeManastEvent.PerformanceFee:
                            pr.xSumPerfFee += fee;
                            pr.xSumPerfFeePerUnit += feePerUnit;
                            break;
                        default:
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Gets the cash invested at the specified date.
        /// </summary>
        /// <param name="ar">The account.</param>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static double GetCashInvestedAtDate(SwapAccount ar, DateTime date)
        {
            double price = 0.0;
            double cashInvested = 0.0;

            foreach (SwapOrder or in ar.OrderRows)
            {
                if (!or.DateExecuted.HasValue || or.DateExecuted.Value.Date > date.Date || (TypeManastOrder)or.Type == TypeManastOrder.Transaction)
                    continue;

                double tmp = cashInvested;
                foreach (SwapTrade tr in or.TradeRows)
                {
                    // do not add trades on inventory instruments
                    if ((tr.Instrument.InstrumentType.IsInventory) || (tr.Instrument.InstrumentType.IsFXSpot))
                        continue;

                    price = (tr.Price / ((tr.Instrument.InstrumentType.IsBond) ? 100.0 : 1.0) * tr.Nominal + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate + tr.Fee;

                    cashInvested += price * tr.Instrument.ContractSize;
                }
                System.Console.WriteLine("Order '{0}'\t{1}\t{2}", or.Description, cashInvested - tmp, cashInvested);
            }
            return cashInvested;
        }


        #region Helper functions

        /// <summary>
        /// Fetches the price.
        /// </summary>
        /// <param name="ir">The instrument.</param>
        /// <param name="stockExchange">The stock exchange.</param>
        /// <param name="price">The price.</param>
        /// <returns></returns>
        private static bool FetchPrice(InstrumentRowSnapshot ir, string stockExchange, out Price price,
            SwapAccount account)
        {
            Price lastPrice = GetLastPrice(ir, stockExchange, null, true /* wait */);
            bool isLastPriceMissing = ((lastPrice.Value == 0) || (double.IsNaN(lastPrice.Value)));
            if (isLastPriceMissing)
            {
                SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eTMinus1, null);
                Price closePrice = GetPrice(ir, priceRequest, true /* wait */);
                lastPrice = closePrice;
            }
            price = lastPrice;

            // return true if the last price is a valid double
            return !double.IsNaN(price.Value);
        }

        public static bool isDeveloper()
        {
            return ((System.Environment.UserName == "et00460") || (System.Environment.UserName == "ex00460") || (System.Environment.UserName == "p856303") || System.Environment.UserName == "p879567" || System.Environment.UserName == "p876371" || System.Environment.UserName == "p106919");
        }

        /// <summary>
        /// Gets the price provider for the instrument.
        /// </summary>
        /// <param name="ir">The instrument.</param>
        /// <param name="stockExchange">The stock exchange.</param>
        /// <param name="ticker">The ticker.</param>
        /// <returns></returns>
		private static IPriceProvider GetProvider(InstrumentRowSnapshot ir)
        {
            string provider = "";
            if (ir.PriceProvider == null || ir.PriceProvider == string.Empty)
                provider = PortfolioPricer.s_defaultProvider;
            else
                provider = ir.PriceProvider;
            return s_pricer[provider];
        }

        public static void SuspendProviders()
        {
            foreach (KeyValuePair<string, IPriceProvider> pp in s_pricer)
            {
                pp.Value.Suspend();
            }
        }

        public static void ResumeProviders()
        {
            foreach (KeyValuePair<string, IPriceProvider> pp in s_pricer)
            {
                pp.Value.Resume();
            }
        }

        public static void StartProviders()
        {
            foreach (KeyValuePair<string, IPriceProvider> pp in s_pricer)
            {
                pp.Value.Start();
            }
        }

        public static void StopProviders()
        {
            foreach (KeyValuePair<string, IPriceProvider> pp in s_pricer)
            {
                pp.Value.Stop();
            }
        }

        public static void ReloadClosings()
        {
            foreach (KeyValuePair<string, IPriceProvider> pp in s_pricer)
            {
                pp.Value.ReloadClosings();
            }
        }

        public static Price GetPrice(InstrumentRowSnapshot ir, TypeManastPriceField type, DateTime? closeDate, bool wait, TypeManastCloseRefDate refDate)
        {
            SPriceRequest priceRequest = new SPriceRequest(type, refDate, closeDate);
            return GetPrice(ir, priceRequest, wait);
        }

        public static Price GetPrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, bool wait)
        {
            if (ir.IsCash)
                return new Price(1.0, DateTime.Now, priceRequest, priceRequest.mField);

            IPriceProvider pp = GetProvider(ir);
            Price z = pp.GetPrice(ir, priceRequest, wait);

            if ((ir.IsFXOpt) || (ir.IsFXSpread))
                z.Value = Math.Max(0.000001, z.Value);

            return z;
        }

        private static double GetDelta(InstrumentRowSnapshot ir)
        {
            if ((ir.IsETF) || (ir.IsFund) || (ir.IsBond) || (ir.IsCash))
                return 1.0;
            double delta = 0.0;
            if (ir.IsFXDerivative)
            {
                long sophisBaseCurrency = 1; // eg. 54875474 for "EUR"
                IPriceProvider pp = GetProvider(ir);
                delta = pp.GetDelta(ir.SICOVAM, sophisBaseCurrency.ToString());
            }
            return delta;
        }

        private static double GetGamma(InstrumentRowSnapshot ir)
        {
            if ((ir.IsETF) || (ir.IsFund) || (ir.IsBond) || (ir.IsCash))
                return 1.0;
            double gamma = 0.0;
            if (ir.IsFXDerivative)
            {
                long sophisBaseCurrency = 1; // eg. 54875474 for "EUR"
                IPriceProvider pp = GetProvider(ir);
                gamma = pp.GetGamma(ir.SICOVAM, sophisBaseCurrency.ToString());
            }
            return gamma;
        }

        /// <summary>
        /// Gets the historic price.
        /// </summary>
        /// <param name="ir">The ir.</param>
        /// <param name="stockExchange">The stock exchange.</param>
        /// <param name="field">The field.</param>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static Price GetHistoricPrice(InstrumentRowSnapshot ir, DateTime date)
        {
            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eSpecificDate, date.Date);
            return GetPrice(ir, priceRequest, true /* wait */);
        }

        //public static double firstTraded(SwapAccount account, SwapAccountInstrument ir, DateTime date)
        //{
        //    TimeSpan span;
        //    TimeSpan spanBefore = date - DateTime.MinValue;
        //    double price = -1.0;
        //    foreach (SwapOrder or in account.GetSortedOrderRows(true, true))
        //    {
        //        span = date - or.TradeDate;
        //        if (or.TradeDate.Date <= date.Date && span <= spanBefore)
        //        {
        //            foreach (SwapTrade tr in or.GetTradeRows())
        //            {
        //                if ((tr.Instrument.Id == ir.Id) && tr.Nominal != 0.0)
        //                {
        //                    spanBefore = span;
        //                    price = tr.Price;
        //                    //System.Diagnostics.Trace.TraceError("first trade price taken!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! price"+price.ToString()+" tradedate " + or.TradeDate+ " ric " +tr.Instrument.RIC + " ir.RIC "+ir.RIC);
        //                }
        //            }
        //        }
        //    }
        //    return price;
        //}

        public static Price GetClosePrice(PortfolioRowSnapshot pr, bool wait)
        {
            if (pr.UseCloseValue.HasValue && pr.UseCloseValue.Value)
            {
                return new Price(pr.CloseValue.Value, DateTime.Now, null, TypeManastPriceField.eClose);
            }
            else
            {
                SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eTMinus1, null);
                return GetPrice(pr.InstrumentSnapshot, priceRequest, wait);
            }
        }

        public static Price GetLastPrice(PortfolioRowSnapshot pr, AccountSnapshotForLastPrice account, bool wait)
        {
            Price p = new Price();
            if (pr.InstrumentByLastPriceInstrumentLink != null)
            {
                p = GetLastPrice(pr.InstrumentByLastPriceInstrumentLink, string.Empty, account, wait);
            }
            else if (!pr.IsManualValueLastPriceNull())
            {
                p = new Price(pr.ManualValueLastPrice.Value, DateTime.Now);
            }
            else
            {
                p = GetLastPrice(pr.InstrumentSnapshot, pr.StockExchange, account, wait);
            }
            return p;
        }

        public static void getPriceType(AccountSnapshotForLastPrice ar, InstrumentRowSnapshot ir, ref SPriceRequest priceRequest)
        {
            getPriceType(ar, ir, ref priceRequest.mField, out priceRequest.mRefDate, out priceRequest.mCloseDate);
        }

        private static void getPriceType(AccountSnapshotForLastPrice ar, InstrumentRowSnapshot ir, ref TypeManastPriceField priceField, out TypeManastCloseRefDate refDate, out DateTime? closeDate)
        {
            refDate = TypeManastCloseRefDate.eNoRefDate;
            closeDate = null;

            if (ar.UseCloseAsLast == true)
            {
                TypeManastCloseAsLast closeType = (TypeManastCloseAsLast)(ar.CloseType);
                switch (closeType)
                {
                    case TypeManastCloseAsLast.TMINUS1:
                        priceField = TypeManastPriceField.eClose;
                        refDate = TypeManastCloseRefDate.eTMinus1;
                        break;
                    case TypeManastCloseAsLast.MOSTRECENT:
                        priceField = TypeManastPriceField.eClose;
                        refDate = TypeManastCloseRefDate.eT;
                        break;
                    case TypeManastCloseAsLast.DATE:
                        priceField = TypeManastPriceField.eClose;
                        refDate = TypeManastCloseRefDate.eSpecificDate;
                        closeDate = ar.CloseDate;
                        break;
                    case TypeManastCloseAsLast.CUSTOM:
                        foreach (PriceRuleSnapshot r in ar.PriceRuleRows) // order by priority ascending
                        {
                            bool matchInstrumentType = ((!r.InstrumentTypeId.HasValue) || ((ir != null) && (ir.InstrumentTypeId == r.InstrumentTypeId)));
                            if (!matchInstrumentType)
                                continue;
                            bool matchRegion = ((!r.RegionId.HasValue) || ((ir != null) && (ir.CountryRegionId != null) && (ir.CountryRegionId == r.RegionId.Value)));
                            if (!matchRegion)
                                continue;
                            priceField = (TypeManastPriceField)r.PriceFieldId;
                            refDate = (TypeManastCloseRefDate)r.RefDateId;
                            closeDate = r.SpecificDate; // filled only when "refDate" = "Specific"
                            break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public static Price GetLastPrice(InstrumentRowSnapshot ir, string stockExchange, AccountSnapshotForLastPrice account, bool wait)
        {
            IPriceProvider pp = GetProvider(ir);

            Price p = null;

            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eRealTimeLast);
            getPriceType(account, ir, ref priceRequest);

            if (ir.IsFXSpot) // eg. "XAU/EUR"
            {
                string ccy1 = ir.InstrumentName.Substring(0, 3);
                string ccy2 = ir.InstrumentName.Substring(4, 3);
                p = pp.GetFXRate(ccy2, ccy1, false, priceRequest, wait);
            }
            else
            {
                p = pp.GetPrice(ir, priceRequest, wait);
            }

            if ((ir.IsFXOpt) || (ir.IsFXSpread))
                p.Value = Math.Max(0.000001, p.Value);

            return p;
        }

        /// <summary>
        /// Gets the ask price.
        /// </summary>
        /// <param name="ir">The instrument.</param>
        /// <param name="stockExchange">The stock exchange.</param>
        /// <returns></returns>
		private static Price GetAskPrice(InstrumentRowSnapshot ir)
        {
            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eRealTimeAsk, TypeManastCloseRefDate.eNoRefDate, null);
            return GetPrice(ir, priceRequest, false);
        }

        /// <summary>
        /// Gets the bid price.
        /// </summary>
        /// <param name="ir">The instrument.</param>
        /// <param name="stockExchange">The stock exchange.</param>
        /// <returns></returns>
		private static Price GetBidPrice(InstrumentRowSnapshot ir)
        {
            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eRealTimeBid, TypeManastCloseRefDate.eNoRefDate, null);
            return GetPrice(ir, priceRequest, false);
        }

        public static Price GetFXRate(AccountSnapshot ar, PortfolioRowSnapshot pr, bool wait)
        {
            Price fxRate = null;

            if (pr.InstrumentByFXInstrumentLink != null) // is there fixing instrument in portfolio properties?
            {
                fxRate = GetLastPrice(pr.InstrumentByFXInstrumentLink, string.Empty, ar.GetSnapshotForLastPrice(), wait);
            }
            else
            {
                if (!pr.IsManualValueLastFXNull()) // is there fixed (manual) rate in portfolio properties?
                {
                    fxRate = new Price(pr.ManualValueLastFX.Value, DateTime.Now);
                }
                else
                {
                    fxRate = GetFXRate(pr.Currency, ar, wait);
                }
            }

            return fxRate;
        }

        public static Price GetFXRate(string ccy, AccountSnapshot ar, bool wait)
        // return ar.Currency / ccy
        {
            if (ccy == ar.Currency)
                return new Price(1.0, DateTime.Now, null, TypeManastPriceField.eRealTimeBid);
            else if ((ccy == "GBX") && (ar.Currency == "GBP"))
                return new Price(100, DateTime.Now, null, TypeManastPriceField.eRealTimeBid); // GBP/GBX
            else if ((ccy == "GBP") && (ar.Currency == "GBX"))
                return new Price(0.01, DateTime.Now, null, TypeManastPriceField.eRealTimeBid); // GBX/GBP

            Price fxRate = null;

            if ((ar.UseManualFX.HasValue && ar.UseManualFX.Value) && (ar.ManualFXRows.Count > 0))
            {
                double factor = 1.0;

                foreach (ManualFXRowSnapshot row in ar.ManualFXRows)
                {
                    if (row.Currency != ccy)
                        continue;
                    if (row.Value != 0.0) // a manual rate?
                    {
                        fxRate = new Price(row.Value, DateTime.Now);
                        break;
                    }
                    if (row.Instrument != null) // a fixing instrument?
                    {
                        fxRate = GetLastPrice(row.Instrument, string.Empty, ar.GetSnapshotForLastPrice(), wait);
                        factor = row.Instrument.ContractSize;
                        break;
                    }
                }

                if (fxRate != null) // ccy found in the table?
                {
                    if (factor != 1.0)
                    {
                        fxRate = new Price(fxRate); // copy the price as otherwise the below *= modifies the price inside the cache, eg. EUR/GBX = EUR/GBP * 100
                        fxRate.Value *= factor;
                    }
                    return fxRate;
                }
            }

            SwapAccountInstrument instr = new SwapAccountInstrument(1);
            instr.InstrumentType = null;
            InstrumentRowSnapshot ir = new InstrumentRowSnapshot(instr);

            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eRealTimeBid);
            getPriceType(ar.GetSnapshotForLastPrice(), ir, ref priceRequest);

            fxRate = GetFXRate(ccy, ar.Currency, ar.UseWMForValuation, priceRequest, wait);

            return fxRate;
        }

        public static Price GetCloseFXRate(PortfolioRowSnapshot pr)
        {
            SPriceRequest priceRequest = new SPriceRequest(TypeManastPriceField.eClose, TypeManastCloseRefDate.eTMinus1, null);
            Price fxRate = GetFXRate(pr.Currency, pr.AccountCurrency, pr.UseWMForValuation, priceRequest, false);
            return fxRate;
        }

        public static Price GetFXRate(string sourceCurrency, string destCurrency, bool useWMFixing, SPriceRequest priceRequest, bool wait)
        {
            //if (sourceCurrency == destCurrency)
            //    return new Price(1.0, DateTime.Now, type);
            Price fxRate = s_pricer[s_defaultProvider].GetFXRate(sourceCurrency, destCurrency, useWMFixing, priceRequest, wait);
            return fxRate;
        }

        /// <summary>
        /// Gets the coupon value.
        /// </summary>
        /// <param name="pr">The portfolio instrument.</param>
        ///         /// <param name="now">The current DateTime.</param>
        /// <returns></returns>
        public static double getCouponRate(SwapAccountPortfolio pr, DateTime now)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            double rate = 0.0;
            try
            {
                InstrumentRowSnapshot ir = new InstrumentRowSnapshot(pr.Instrument);
                IPriceProvider pp = GetProvider(ir);
                rate = pp.GetBdCpnValue(now.Date, ir.SICOVAM);
                Engine.Instance.Log.Info(methodName + " : coupon rate for '" + ir.InstrumentName + "' on '" + now.Date.ToString("dd/MM/yyyy") + "' is " + rate);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error(methodName + " : exception caught for '" + pr.Instrument.InstrumentName + "' of '" + pr.Account.AccountName + "' : " + ex.Message);
            }
            return rate;
        }

        /// <summary>
        /// Gets the next coupon date.
        /// </summary>
        /// <param name="pr">The portfolio instrument.</param>
        /// <param name="now">The current DateTime.</param>
        /// <returns></returns>
        public static bool GetCouponNextDate(SwapAccountPortfolio pr, DateTime now, out DateTime exDate, out DateTime payDate)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            exDate = payDate = DateTime.MinValue;
            try
            {
                InstrumentRowSnapshot ir = new InstrumentRowSnapshot(pr.Instrument);
                IPriceProvider pp = GetProvider(ir);
                return pp.GetCpnNext(now, pr.Instrument.SICOVAM, out exDate, out payDate);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error(methodName + " : exception caught for '" + pr.Instrument.InstrumentName + "' of '" + pr.Account.AccountName + "' : " + ex.Message);
            }
            return false;
        }

        #endregion
    }
}
