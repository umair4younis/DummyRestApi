using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Globalization;
using System.ComponentModel;
using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;

namespace Puma.MDE.Data
{
    public enum BenchmarkVolatilitySourceType
    {
        Sophis = 0,
        MDE =1
    }
    
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("294CE1FA-3AD0-4cae-BDD9-ED57858A5C7A")]
    public class Underlyings : IEnumerable
    {
        IList<Underlying> collection;
        public Underlyings(IList<Underlying> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(Underlying item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public Underlying this[int index]
        {
            get
            {
                return collection[index];
            }
            set
            {
                collection[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return collection.Count;
            }
        }

        [ComVisible(false)]
        public IList<Underlying> Collection
        {
            get
            {
                return collection;
            }
        }
    }


    public enum WeeklyOptionsMode
    {
        [Description("Not Allowed")]
        NotAllowed,

        [Description("Allowed")]
        Allowed,

        [Description("Strictly Not Allowed")]
        StrictlyNotAllowed
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("05F40F79-ADD7-4b06-93D4-05C31F60AE5E")]
    [ComVisible(true)]
    [Serializable]
    public class Underlying : Entity
    {
        public enum VolatilityORCStickyModeType
        {
            StickyStrike = 0,
            StickySkew,
            StickyMoneyness
        };

        public enum BenchmarkSpreadCalculationMethodType
        {
            HistoricalRealizedVolatility = 0,
            ImpliedVolatiliityOnLastMaturity
        };

        public Underlying()
        {
            EarningDates = new List<EarningDate>();
            Destinations = new List<PublishDestination>();
            RepoDestinations = new List<RepoPublishDestination>();
            DivDestinations = new List<DivPublishDestination>();
            Compositions = new List<Composition>();
            QuixCompositions = new List<QuixComposition>();
            BasketFixingConfigurations = new List<BasketFixingConfiguration>();
            IndexCompositionDestinations = new List<IndexCompositionPublishDestination>();
            EuwaxIssuerItems = new List<EuwaxIssuerItem>();         // Trac: #35731

            MidVolatilityRelativeSpread = 0;
            ORCDividendTrafo = true;
            MinimalOrderSize = 0;
            UseFxOptions = false;
            UseEurexQuotes = false;
            WeightsByQuality = false;

            MaxListedOptionMaturity = 3;

            InitialMinStrikeListedOptions = 0.9;
            InitialMaxStrikeListedOptions = 1.2;
            FinalMinStrikeListedOptions = 0.7;
            FinalMaxStrikeListedOptions = 1.7;

            MaxExpiryStrikeInterpolation = 2;

            InitialMinStrikeListedOptionsPut = 0.8;
            InitialMaxStrikeListedOptionsPut = 1.1;
            FinalMinStrikeListedOptionsPut = 0.65;
            FinalMaxStrikeListedOptionsPut = 1.2;

            RepoPublishingWarningThreshold = 0.3;

            QuixIndexPrecision = 8;
            QuixSpiPrecision = 8;
            QuixIndexPrecisionReweight = false;
            UseIndexCompositionSource = false;
            QuixExcludeFeeFromPortal = false;
            QuixRebalancingFeeEnabled = false;
            IndexDividendEnabled = false;
            PnLImpact = true;

            UseContractNameFromLiqDB = false;
        }


        public String Reference { get; set; }
        private String _Type;
        public String Type
        {
            get => _Type;
            set
            {
                _Type = value;
                NotifyPropertyChanged(nameof(Type));
            }
        }
        public String RIC { get; set; }
        public String ISIN { get; set; }
        public String Bloomberg { get; set; }
        public int SophisId { get; set; }
        public String ORCPumaUnderlying { get; set; }
        public String ORCRepoShadowUnderlying { get; set; }
        public String ORCPumaDivContract { get; set; }
        public double ORCDividendHorizon { get; set; }
        public bool ORCDividendTrafo { get; set; }
        public bool UseFutureReference { get; set; }
        public double MidVolatilityRelativeSpread { get; set; }
        public bool UseContractNameFromLiqDB { get; set; }
        public bool StrikeInPercent { get; set; }
        public BenchmarkVolatilitySourceType BenchmarkVolatilitySource { get; set; }

        public string LinearDeskBasisRIC { get; set; }
        public double BasisThreshold { get; set; }
        public bool UseFxOptions { get; set; }

        public double MinimalOrderSize { get; set; }

        public bool UseEurexQuotes { get; set; }
        public bool WeightsByQuality { get; set; }
        public double FuturePriceMultiplier { get; set; }
        public bool SaveCurveFamilies { get; set; }

        bool useDedicatedShortMaturitySpreads;
        public bool UseDedicatedShortMaturitySpreads
        {
            get
            {
                return useDedicatedShortMaturitySpreads;
            }
            set
            {
                useDedicatedShortMaturitySpreads = value;
                NotifyPropertyChanged("UseDedicatedShortMaturitySpreads");
            }
        }

        bool _ORCPublishOnlyCommodityFutures;
        public bool ORCPublishOnlyCommodityFutures
        {
            get
            {
                return _ORCPublishOnlyCommodityFutures;
            }
            set
            {
                _ORCPublishOnlyCommodityFutures = value;
                if (_ORCPublishOnlyCommodityFutures == false)
                {
                    ORCPublishReplacedSubstring = "";
                    ORCPublishReplacingSubstring = "";
                }
                NotifyPropertyChanged("ORCPublishOnlyCommodityFutures");
            }
        }

        string _ORCPublishReplacedSubstring;
        public string ORCPublishReplacedSubstring
        {
            get
            {
                return _ORCPublishReplacedSubstring;
            }
            set
            {
                _ORCPublishReplacedSubstring = value;
                NotifyPropertyChanged("ORCPublishReplacedSubstring");
            }
        }

        public string _ORCPublishReplacingSubstring;
        public string ORCPublishReplacingSubstring
        {
            get
            {
                return _ORCPublishReplacingSubstring;
            }
            set
            {
                _ORCPublishReplacingSubstring = value;
                NotifyPropertyChanged("ORCPublishReplacingSubstring");
            }
        }

        public DateTime ShortMaturityFirstQuotesSlice { get; set; }
        public double ShortMaturitySpreadATM { get; set; }
        public double ShortMaturitySpreadSkew { get; set; }
        public double ShortMaturitySpreadCallCnv { get; set; }
        public double ShortMaturitySpreadPutCnv { get; set; }


        public DateTime IndicativeStartTime { get; set; }
        public DateTime IndicativeEndTime { get; set; }

        public string MaxMaturity { get; set; }

        public double UsedSpotThreshold { get; set; }

        public WeeklyOptionsMode AllowWeeklyOptions { get; set; }
        public bool AreWeeklyOptionsAllowed
        {
            get
            {
                return AllowWeeklyOptions == WeeklyOptionsMode.Allowed;
            }
        }
        public bool AllowQuixComposition { get; set; }
        public int  QuixIndexPrecision { get; set; }
        public int  QuixSpiPrecision { get; set; }
        public bool QuixIndexPrecisionReweight { get; set; }
        public bool QuixFeeForToday { get; set; }
        public bool QuixActivelyUsed { get; set; }
        public bool QuixUseComponentCalendar { get; set; }
        public bool QuixUseInstrumentCalendar { get; set; }
        public bool QuixExcludeFeeFromPortal { get; set; }
        public bool QuixRebalancingFeeEnabled { get; set; }
        public bool IndexDividendEnabled { get; set; }
        public bool IncludeInVestr { get; set; }
        public string VestrProductsISINs { get; set; }

        public string SophisPublishingExcludeStart { get; set; }
        public string SophisPublishingExcludeEnd { get; set; }

        public bool IsSophisPublishingWindowEnabled
        {
            get
            {
                if (String.IsNullOrEmpty(SophisPublishingExcludeStart) ||
                    String.IsNullOrEmpty(SophisPublishingExcludeEnd))
                {
                    return true;
                }

                try
                {
                    var start = DateTime.Today + DateTime.ParseExact(SophisPublishingExcludeStart, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
                    var end = DateTime.Today + DateTime.ParseExact(SophisPublishingExcludeEnd, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;

                    return !(DateTime.Now >= start && DateTime.Now <= end);
                }
                catch
                {
                    return true;
                }
            }
        }

        [ComVisible(false)]
        public double? QuixFeeAmount { get; set; }

        [ComVisible(false)]
        public int? QuixFeeDayCount { get; set; }

		public int IndexCompositionSource { get; set; }
        public bool UseIndexCompositionSource { get; set; }

        public string BasketFixingAlgorithm { get; set; }

        public double RepoPublishingWarningThreshold { get; set; }
        public double DivPublishingWarningThreshold { get; set; }

        public bool ForDivRepo { get; set; }

        // Possible DiscountingSetting values:
        // 0 - Default 
        // 1 - OIS
        // 2 - None
        public int DiscountingSetting { get; set; }
        
        public bool SendOisCurveToOrc { get; set; }
        public bool SendFxFwdCurveToOrc { get; set; }

        public string CCSYiedCurveFamily { get; set; }

        public bool CheckVolMonitors { get; set; }

        [ComVisible(false)]
        public TimeSpan IndicativeStartOffset
        {
            get
            {
                return IndicativeStartTime - IndicativeStartTime.Date;
            }
            set
            {
                IndicativeStartTime = DateTime.Today + value;
            }
        }
        [ComVisible(false)]
        public TimeSpan IndicativeEndOffset
        {
            get
            {
                return IndicativeEndTime - IndicativeEndTime.Date;
            }
            set
            {
                IndicativeEndTime = DateTime.Today + value;
            }
        }
        public DateTime TradableStartTime { get; set; }
        public DateTime TradableEndTime { get; set; }
        [ComVisible(false)]
        public TimeSpan TradableStartOffset
        {
            get
            {
                return TradableStartTime - TradableStartTime.Date;
            }
            set
            {
                TradableStartTime = DateTime.Today + value;
            }
        }
        [ComVisible(false)]
        public TimeSpan TradableEndOffset
        {
            get
            {
                return TradableEndTime - TradableEndTime.Date;
            }
            set
            {
                TradableEndTime = DateTime.Today + value;
            }
        }
        
        public DateTime MarketStartTime { get; set; }
        public DateTime MarketEndTime { get; set; }
        [ComVisible(false)]
        public TimeSpan MarketStartOffset
        {
            get
            {
                return MarketStartTime - MarketStartTime.Date;
            }
            set
            {
                MarketStartTime = DateTime.Today + value;
            }
        }
        
        [ComVisible(false)]
        public TimeSpan MarketEndOffset
        {
            get
            {
                return MarketEndTime - MarketEndTime.Date;
            }
            set
            {
                MarketEndTime = DateTime.Today + value;
            }
        }

        public bool UseDefaultIndexMaturitySchedule { get; set; }

        public bool CalculateHistoricBasketValue { get; set; }
        public string BasketFixingOutputColumn { get; set; }
        public int BasketFixingPrecision {get; set;}

        public bool TransferIndexCompositionToSophis { get; set; }

        public int MaxListedOptionMaturity { get; set; } // to be given in years

        public double ExcludeMonthlyOptionAfter { get; set; } // to be given in years

        public double StrikeStep { get; set; }

        public double InitialMinStrikeListedOptions { get; set; }
        public double InitialMaxStrikeListedOptions { get; set; }
        public double FinalMinStrikeListedOptions { get; set; }
        public double FinalMaxStrikeListedOptions { get; set; }
        public int MaxExpiryStrikeInterpolation { get; set; } //to be given in years
        public double InitialMinStrikeListedOptionsPut { get; set; }
        public double InitialMaxStrikeListedOptionsPut { get; set; }
        public double FinalMinStrikeListedOptionsPut { get; set; }
        public double FinalMaxStrikeListedOptionsPut { get; set; }

        public bool PnLImpact { get; set; }

        public Classifications Classifications
        {
            get
            {
                Classifications retval = new Classifications(new List<Classification>());

                return retval;
            }
        }

        public MaturitySchedule GetMaturitySchedule()
        {
            MaturitySchedule retval = null;

            foreach (Classification c in Classifications)
            {
                retval = c.GetMaturitySchedule();
                if (retval != null)
                    break;
            }

            if (retval == null)
                throw new PumaMDEException("no maturity schedule found for " + Reference );

            return retval;
        }

        public object GetMaturityScheduleProvider()
        {
            MaturitySchedule ms = GetMaturitySchedule();
            return null;
        }

        public DateTime GetScheduledMaturities()
        {
            return new DateTime();
        }

        Underlying _benchmark = null;
        
        [ComVisible(false)]
        public IList<PublishDestination> Destinations { get; set; }

        [ComVisible(false)]
        public IList<RepoPublishDestination> RepoDestinations { get; set; }

        #region Index Composition destinations
        [ComVisible(false)]
        public IList<IndexCompositionPublishDestination> IndexCompositionDestinations { get; set; }

        [ComVisible(false)]
        public IList<DivPublishDestination> DivDestinations { get; set; }

        public void AddDivDestination(DivPublishDestination item)
        {
            item.Underlying = this;
            DivDestinations.Add(item);
        }

        public DivPublishDestinations DivDestinationsCollection
        {
            get
            {
                return new DivPublishDestinations(DivDestinations);
            }
        }

        public bool HasFutureSpots()
        {
            return true;
        }
        
        public bool IsCurrency
        {
            get
            {
                return Type == "E";
            }
        }
        public bool IsEquity
        {
            get
            {
                return Type=="I" || Type=="A";
            }
        }
        public bool IsBasket
        {
            get
            {
                return Type == "B";
            }
        }

        public bool IsCommodity
        {
            get
            {
                return Type == "Q";
            }
        }

        public bool IsIndexOrBasket
        {
            get
            {
                return Type == "I" || Type == "B";
            }
        }

        public bool IsNotDWSBasket
        {
            get
            {
                return !"DWS".Equals(BasketFixingAlgorithm);
            }
        }

        [ComVisible(false)]
        public bool ShowCommodityFuturesPublishingSettings
        {
            get
            {
                return this.IsCommodity;
            }
        }


        [ComVisible(false)]
        public IList<BasketFixingConfiguration> BasketFixingConfigurations { get; set; }

        public BasketFixingConfigurations BasketFixingConfigurationCollection
        {
            get
            {
                return new BasketFixingConfigurations(BasketFixingConfigurations);
            }
        }

        public void AddBasketFixingConfiguration(BasketFixingConfiguration item)
        {
            item.Underlying = this;
            BasketFixingConfigurations.Add(item);
        }

        #region QuixCompositions
        [ComVisible(false)]
        public IList<QuixComposition> QuixCompositions { get; set; }

        public string QuixIcsInstrument { get; set; }
        public string QuixPortalInstrument { get; set; }

        public QuixReweight QuixReweight { get; set; }

        private string _lastQuixChange;
        public string LastQuixChange
        {
            get { return _lastQuixChange; }
        }

        public bool HasQuixCompositions
        {
            get
            {
                if (QuixCompositions == null)
                    return false;

                return QuixCompositions.Count > 0;
            }
        }

        public bool _hasQuixError;
        public bool HasQuixError
        {
            get { { return (_hasQuixError || _quixPublishingError); } }
        }

        public string QuixReweightName
        {
            get
            {
                if(QuixReweight != null &&
                   QuixReweight.QuixReweightModel != null)
                {
                    // Return anything between ( )
                    //if (QuixReweight.QuixReweightModel.Name.Contains("("))
                    //{
                    //    return Regex.Match(QuixReweight.QuixReweightModel.Name, @"\(([^)]*)\)").Groups[1].Value;
                    //}
                    return QuixReweight.QuixReweightModel.Name;
                }

                return null;
            }
        }

        public QuixComposition LastAvailableQuixComposition
        {
            get
            {
                if (QuixCompositions != null && QuixCompositions.Count > 0)
                {
                    var todayAtMidnight = DateTime.Now.Date.AddDays(1);

                    var listOfCompositionFromToday = QuixCompositions
                        .Where(c => c.ValidFrom < todayAtMidnight)
                        .OrderByDescending(c => c.ValidFrom.Date)
                        .ThenByDescending( c => c.ReweightStart.Date)           // TRAC: #34799
                        .ThenByDescending( c => c.ReweightStart.TimeOfDay);

                    return listOfCompositionFromToday.FirstOrDefault();
                }
                return null;
            }
        }

        private bool _quixPublishingError;
        public bool QuixPublishingError
        {
            get { return _quixPublishingError; }
        }

        public void LoadQuixLastUpdateAndPublishingStatus()
        {
            _lastQuixChange = "Reweight"; 
        }

        public void LoadQuixReweight()
        {
            QuixReweight = null;
        }

        public Type GetQuixReweightModelProcessType()
        {
            return null;
        }
        public bool AddQuixComposition(QuixComposition quixComposition)
        {
            if (QuixCompositions == null || QuixCompositions.Contains(quixComposition))
                return false;
            
            if (QuixCompositions != null)
            {
                return false;
            }

            quixComposition.Underlying = this;
            QuixCompositions.Add(quixComposition);

            SetToDirty(() => quixComposition);

            return true;
        }

        public bool UpdateQuixComposition(QuixComposition quixComposition)
        {
            if (QuixCompositions == null)
                return false;

            if (QuixCompositions != null)
            {
               // If it exist remove it and add new one
                var anyExistWithSameValidFrom = QuixCompositions.SingleOrDefault(c => c.ValidFrom.Date.Equals(quixComposition.ValidFrom.Date));
                if (anyExistWithSameValidFrom != null)
                {
                    QuixCompositions.Remove(anyExistWithSameValidFrom);

                    // update if any price is set as manual
                    //var anyWithPriceManual = anyExistWithSameValidFrom.QuixComponents.Where(components => components.PriceManual == true || components.FxRateManual == true);
                    //foreach(var component in anyWithPriceManual)
                    //{
                    //    var currentComponent = component;
                    //    var found = quixComposition.QuixComponents.SingleOrDefault(comp => comp.Reference == currentComponent.Reference);
                    //    if(found != null)
                    //    {
                    //        found.IsNotificationEnabled = false;

                    //        if (currentComponent.PriceManual)
                    //        {
                    //            found.Price = currentComponent.Price;
                    //        }

                    //        if (currentComponent.FxRateManual)
                    //        {
                    //            found.FxRate = currentComponent.FxRate;
                    //        }

                    //        found.IsNotificationEnabled = true;
                    //    }

                    //}
                }
            }

            quixComposition.Underlying = this;
            QuixCompositions.Add(quixComposition);

            SetToDirty(() => quixComposition);

            return true;
        }

        public void RemoveQuixComposition(QuixComposition quixComposition)
        {
            if (quixComposition.IsPersisted)
            {
                quixComposition.IsSetToDelete = true;
                SetToDirty();
            }
            else
            {
                if (QuixCompositions.Contains(quixComposition))
                {
                    QuixCompositions.Remove(quixComposition);
                }
            }
        }

        public void ResetToNotDirty()
        {
            if (QuixCompositions == null)
                return;

            foreach (var quixComposition in QuixCompositions)
            {
                var currQuixComposition = quixComposition;

                currQuixComposition.ResetToNonDirty();

                foreach (var comp in currQuixComposition.QuixComponents)
                {
                    var currComp = comp;
                }
            }

            ResetToNonDirty();
        }
        #endregion

        [ComVisible(false)]
        public IList<Composition> Compositions { get; set; }

        public void AddComposition(Composition item)
        {
            item.Underlying = this;
            Compositions.Add(item);
        }

        public Compositions CompositionsCollection
        {
            get
            {
                return new Compositions(Compositions);
            }
        }

        virtual public double GetVolatility(DateTime dtExpiry, double strike)
        {
            return 1;
        }

        virtual public double GetTheoreticalValue()
        {
            return 1;
        }

        virtual public double GetForward(DateTime dtExpiry)
        {
            return 1;
        }

        virtual public Volsurface GetVolsurfaceAt(DateTime time)
        {
            return Engine.Instance.Factory.GetVolsurfaceAt(this, time);
        }

        virtual public double GetHistoricVolatility(int days)
        {
            return 1;
        }

        virtual public double GetStandardFutureQuotity()
        {
            return 1;
        }

        Dictionary<Int32, Int32> offsetscache = new Dictionary<Int32, Int32>();
        public Int32 GetStandardListedMarketOptionPaymentOffset()
        {
            Int32 retval = 0;
            if (offsetscache.ContainsKey(Id))
                return offsetscache[Id];

            lock (Engine.Instance.Factory.thisLock)
            {
                try
                {
                    string sql = "";

                    sql += " select ";
                    sql += "       m.decalagereglement";
                    sql += " from ";
                    sql += "        mo_support s, ";
                    sql += "        mo_terme t,";
                    sql += "        titres o,";
                    sql += "        marche m";
                    sql += " where ";
                    sql += "       s.sicovam=:code";
                    sql += " and   t.ident=s.marche";
                    sql += " and   t.terme=s.terme";
                    sql += " and   o.sicovam=t.code_call";
                    sql += " and   o.marche=m.mnemomarche";
                    sql += " union all ";
                    sql += " select m.decalagereglement from marche m, titres t where t.sicovam =:code and m.mnemomarche = t.marche";


                    retval = (Int32)Engine.Instance.
                        Session.CreateSQLQuery(sql)
                               .SetInt32("code", SophisId)
                                        .List<Decimal>()[0];

                }
                catch
                {
                }
            }
            offsetscache[Id] = retval;
            return retval;
        }

        Dictionary<Int32, Int32> undoffsetscache = new Dictionary<Int32, Int32>();
        public Int32 GetMarketOptionPaymentOffset()
        {
            Int32 retval = 0;
            if (undoffsetscache.ContainsKey(Id))
                return undoffsetscache[Id];

            lock (Engine.Instance.Factory.thisLock)
            {
                try
                {
                    string sql = "";

                    sql += "select m.decalagereglement from marche m, titres t where t.sicovam =:code and m.mnemomarche = t.marche and m.codedevise = str_to_devise(:ccy)";


                    retval = (Int32)Engine.Instance.
                        Session.CreateSQLQuery(sql)
                               .SetInt32("code", SophisId).SetString("ccy",GetCurrency())
                                        .List<Decimal>()[0];

                }
                catch
                {
                }
            }
            undoffsetscache[Id] = retval;
            return retval;
        }
        
        public string GetCurrency()
        {
            return string.Empty;
        }

        public bool IsBasketCompo()
        {
            return false;
        }

        [ComVisible(false)]
        public IList<EarningDate> EarningDates { get; set; }

        public void AddEarningDate(EarningDate item)
        {
            item.Underlying = this;
            EarningDates.Add(item);
        }

        [ComVisible(false)]
        public IList<EuwaxIssuerItem> EuwaxIssuerItems { get; set; }

        Dictionary<Int32, string> undMarketNameCache = new Dictionary<Int32, string>();
    }

    [ComVisible(false)]
    [Serializable]
    public class Basket : Underlying
    {
        override public double GetVolatility(DateTime dtExpiry, double strike)
        {
            // using theoretical + stocks smile formula
            
            double retval = 0;
            double relstrike = strike/GetTheoreticalValue() ;

            double fwd  = GetForward(dtExpiry) ;
            double time = 1;

            foreach (Composition com1 in Compositions)
            {
                foreach (Composition com2 in Compositions)
                {
                    double corr = 1;
                    
                    double strike1 = relstrike * com1.Component.GetTheoreticalValue();
                    double vol1 = com1.Component.GetVolatility(dtExpiry, strike1);

                    double strike2 = relstrike * com2.Component.GetTheoreticalValue();
                    double vol2 = com2.Component.GetVolatility(dtExpiry, strike2);

                    double fwd1 = com1.Component.GetForward(dtExpiry) ;
                    double fwd2 = com2.Component.GetForward(dtExpiry) ;

                    retval += fwd1 * fwd2 * com1.Weight * com2.Weight * Math.Exp(corr * vol1 * vol2 * time);

                }
            }

            retval = Math.Log(retval / (fwd * fwd))/time;
            retval = Math.Sqrt(retval);

            return retval;
        }

        override public double GetTheoreticalValue()
        {
            double retval = 0;
            
            foreach (Composition com in Compositions)
            {
                retval += com.Weight * com.Component.GetTheoreticalValue();
            }

            return retval;
        }

        override public double GetForward(DateTime dtExpiry)
        {
            double retval = 0;

            foreach (Composition com in Compositions)
            {
                retval += com.Weight * com.Component.GetForward(dtExpiry);
            }

            return retval;
        }

        override public  Volsurface GetVolsurfaceAt(DateTime time)
        {
            Volsurface retval = Volsurface.CreateInstance(null, this) ;

            retval.Timestamp = DateTime.MinValue;
            foreach (Composition com1 in Compositions)
            {
                Volsurface s1 = com1.Component.GetVolsurfaceAt(time);

                if (s1.Timestamp > retval.Timestamp)
                    retval.Timestamp = s1.Timestamp;

            }

            return retval;
        }

        override public double GetHistoricVolatility(int days)
        {
            double retval = 0;

            double spot = GetTheoreticalValue();

            foreach (Composition com1 in Compositions)
            {
                foreach (Composition com2 in Compositions)
                {
                    double corr = 1;

                    double vol1 = 1;

                    double vol2 = 1;

                    double spot1 = com1.Component.GetTheoreticalValue();
                    double spot2 = com2.Component.GetTheoreticalValue();

                    retval += spot1 * spot2 * com1.Weight * com2.Weight * corr * vol1 * vol2 ;

                }
            }

            retval = retval / (spot * spot) ;
            retval = Math.Sqrt(retval);
            
            return retval;
        }
    }
    [ComVisible(false)]
    [Serializable]
    public class Share : Underlying
    {
        override public double GetStandardFutureQuotity()
        {
            return 1;
        }
    }
    [ComVisible(false)]
    [Serializable]
    public class Index : Underlying
    {
    }
    [ComVisible(false)]
    [Serializable]
    public class Currency : Underlying
    {
    }
    [ComVisible(false)]
    [Serializable]
    public class Commodity : Underlying
    {
    }


    public class UnderlyingAudit : Audit, IEquatable<UnderlyingAudit>
    {       
        public String Reference { get; set; }
        public String Type { get; set; }
        public String RIC { get; set; }

        [IgnoreCompare]
        public int SophisId { get; set; }

        public bool UseFutureReference { get; set; }
        public double MidVolatilityRelativeSpread { get; set; }

        public bool IsVolatilityCenteredAtSpot { get; set; }

        [IgnoreCompare]
        public string LinearDeskBasisRIC { get; set; }

        public bool UseFxOptions { get; set; }

        [IgnoreCompare]
        public double BasisThreshold { get; set; }


        public double MinimalOrderSize { get; set; }

        public bool UseEurexQuotes { get; set; }
        public bool WeightsByQuality { get; set; }

        public bool UseDedicatedShortMaturitySpreads { get; set; }
        public DateTime ShortMaturityFirstQuotesSlice { get; set; }
        public double ShortMaturitySpreadATM { get; set; }
        public double ShortMaturitySpreadSkew { get; set; }
        public double ShortMaturitySpreadCallCnv { get; set; }
        public double ShortMaturitySpreadPutCnv { get; set; }

        public string MaxMaturity { get; set; }

        public bool UseDefaultIndexMaturitySchedule { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Underlying"; }
        }

        //public override void Compare(object instance, Action<PropertyValueChange> propertyValueChange)
        //{
        //    var specific = (UnderlyingAudit)instance;
        //    CompareUtil.Compare(this, specific, propertyValueChange);
        //}

        public bool Equals(UnderlyingAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Reference, Reference) && Equals(other.Type, Type) && Equals(other.RIC, RIC) && other.SophisId == SophisId && other.UseFutureReference.Equals(UseFutureReference) && other.MidVolatilityRelativeSpread.Equals(MidVolatilityRelativeSpread) && other.IsVolatilityCenteredAtSpot.Equals(IsVolatilityCenteredAtSpot) && Equals(other.LinearDeskBasisRIC, LinearDeskBasisRIC) && other.UseFxOptions.Equals(UseFxOptions) && other.BasisThreshold.Equals(BasisThreshold) && other.MinimalOrderSize.Equals(MinimalOrderSize) && other.UseEurexQuotes.Equals(UseEurexQuotes) && other.WeightsByQuality.Equals(WeightsByQuality) && other.UseDedicatedShortMaturitySpreads.Equals(UseDedicatedShortMaturitySpreads) && other.ShortMaturityFirstQuotesSlice.Equals(ShortMaturityFirstQuotesSlice) && other.ShortMaturitySpreadATM.Equals(ShortMaturitySpreadATM) && other.ShortMaturitySpreadSkew.Equals(ShortMaturitySpreadSkew) && other.ShortMaturitySpreadCallCnv.Equals(ShortMaturitySpreadCallCnv) && other.ShortMaturitySpreadPutCnv.Equals(ShortMaturitySpreadPutCnv) && Equals(other.MaxMaturity, MaxMaturity) && other.UseDefaultIndexMaturitySchedule.Equals(UseDefaultIndexMaturitySchedule);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (UnderlyingAudit)) return false;
            return Equals((UnderlyingAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (Reference != null ? Reference.GetHashCode() : 0);
                result = (result*397) ^ (Type != null ? Type.GetHashCode() : 0);
                result = (result*397) ^ (RIC != null ? RIC.GetHashCode() : 0);
                result = (result*397) ^ Id;
                result = (result*397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result*397) ^ JournalTimeStamp.GetHashCode();
                result = (result*397) ^ SophisId;
                result = (result*397) ^ UseFutureReference.GetHashCode();
                result = (result*397) ^ MidVolatilityRelativeSpread.GetHashCode();
                result = (result*397) ^ IsVolatilityCenteredAtSpot.GetHashCode();
                result = (result*397) ^ (LinearDeskBasisRIC != null ? LinearDeskBasisRIC.GetHashCode() : 0);
                result = (result*397) ^ UseFxOptions.GetHashCode();
                result = (result*397) ^ BasisThreshold.GetHashCode();
                result = (result*397) ^ MinimalOrderSize.GetHashCode();
                result = (result*397) ^ UseEurexQuotes.GetHashCode();
                result = (result*397) ^ WeightsByQuality.GetHashCode();
                result = (result*397) ^ UseDedicatedShortMaturitySpreads.GetHashCode();
                result = (result*397) ^ ShortMaturityFirstQuotesSlice.GetHashCode();
                result = (result*397) ^ ShortMaturitySpreadATM.GetHashCode();
                result = (result*397) ^ ShortMaturitySpreadSkew.GetHashCode();
                result = (result*397) ^ ShortMaturitySpreadCallCnv.GetHashCode();
                result = (result*397) ^ ShortMaturitySpreadPutCnv.GetHashCode();
                result = (result*397) ^ (MaxMaturity != null ? MaxMaturity.GetHashCode() : 0);
                result = (result*397) ^ UseDefaultIndexMaturitySchedule.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(UnderlyingAudit left, UnderlyingAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UnderlyingAudit left, UnderlyingAudit right)
        {
            return !Equals(left, right);
        }
    }

    public class UnderlyingChangeDomain : PropertyValueChange
    {
        public UnderlyingChangeDomain()
        { }

        public UnderlyingChangeDomain(PropertyValueChange c)
            : base(c)
        {
        }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; NotifyPropertyChanged(() => Selected); }
        }

        private string _classification;
        public string Classification
        {
            get { return _classification; }
            set { _classification = value; NotifyPropertyChanged(() => Classification); }
        }
    }

   
}
#endregion
