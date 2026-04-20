using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Runtime.InteropServices;
using System.ComponentModel;
using NLog;


//using sophis.market_data;
//using sophis.instrument;
//using sophis.commodity;
//using sophisTools;

namespace Puma.MDE.Data
{
    public enum eCommodityVolaClass     
    { 
        Default         = 0 , 
        Underlying      = 1,
        HashFuture      = 2, 
        Classification  = 3 
    }
    public enum mdeMVolatilityModelType 
    {
        M_vmSameAsBasis                 = 1,
        M_vmAbsolute                    = 2,
        M_vmBasisSpread                 = 3,
        M_vmPutSpread                   = 4,
        M_vmSameAsCall                  = 5,
        M_vmSameAsCallPlusPutSpread     = 6,
        M_vmSameAsCallPlusCallSpread    = 7,
    }
    public enum mdeMVolatilityCurveType 
    {
        [Description("Call Result")] 
        M_vcCallResult = 0,

        [Description("Call Market")] 
        M_vcCallMarket      = 1,

        [Description("Call Management")]
        M_vcCallManagement  = 2,

        [Description("Call Bid")]
        M_vcCallBid         = 3,

        [Description("Call Ask")]
        M_vcCallAsk         = 4,

        [Description("Put Result")]
        M_vcPutResult       = 5,

        [Description("Put Market")]
        M_vcPutMarket       = 6,

        [Description("Put Management")]
        M_vcPutManagement   = 7,

        [Description("Put Bid")]
        M_vcPutBid          = 8,

        [Description("Put Ask")]
        M_vcPutAsk          = 9,

       // [Description("EndOfList")]
       // M_vcEndOfList       = 10,
    }
    public enum mdeMVolatilityPointType 
    {
        M_vpAbsoluteDate            = 1,
        M_vpDay                     = 2,
        M_vpMonth                   = 3,
        M_vpYear                    = 4,
        M_vpMonthEndOfMonth         = 5,
        M_vpQuarterEndOfQuarter     = 6,
        M_vpYearEndOfYear           = 7,
        M_vpAbsoluteMonth           = 8,
    }
    public enum mdeMSmileScaleType      
    {
        M_ssDeltaCash           = -1,
        M_ssPercentageStrike    = 0,
        M_ssAbsoluteStrike      = 1,
        M_ssDeltaStrike         = 2,
    }
    public enum eDisplayType            
    {
        [Description("Volatility")]
        Volatility = 0,
        [Description("Smile")]
        Smile = 1
    }



    [ComVisible(false)]
    public class D2Linterp 
    {
        private SortedDictionary<DateTime, double[]> m_VolCurveSlices = new SortedDictionary<DateTime, double[]>();
        private SortedDictionary<DateTime, double> m_ATMVolCurve= new SortedDictionary<DateTime, double>();
        private List<double> m_StrikeGrid = new List<double>();
        private List<DateTime> m_matKeys = new List<DateTime>();
        private double m_reversedGridValue = 0;
        private bool m_strikeGridReversed = false; 

        public D2Linterp()
        {
            //m_VolCurveSlices = new SortedDictionary<DateTime, double[]>();
            //m_StrikeGrid = new List<double>();
            //m_matKeys = new List<DateTime>();
        }

              public D2Linterp(List<DateTime> matgrid, List<double> strikeGrid)
              {
                  Initialize(matgrid, strikeGrid); 
              }

              public void clear()
              {
                  m_VolCurveSlices.Clear();
                  m_StrikeGrid.Clear();
                  m_matKeys.Clear();
                  m_ATMVolCurve.Clear();
              }

              public void  Initialize(List<DateTime> matgrid, List<double> strikeGrid)
              {
                      m_StrikeGrid = new List<double> (strikeGrid.ToArray());
                      m_VolCurveSlices = new SortedDictionary<DateTime, double[]>();
                      m_ATMVolCurve = new SortedDictionary<DateTime, double>(); 
                      m_matKeys = new List<DateTime>(matgrid.ToArray()); 
                      // strike grid
                     // m_atm_Idx = strikeGrid.IndexOf(ATMValue); 
                      // structure 
                      for (int i = 0; i < matgrid.Count(); ++i)
                      {
                            double[] vols = new double[m_StrikeGrid.Count];
                            m_VolCurveSlices.Add(matgrid[i], vols);
                            m_ATMVolCurve.Add(matgrid[i], 0.0); 
                      }
              }

              public void setReversedGridValue(double ToReverse)
              {
                  this.m_reversedGridValue = ToReverse;
                  for (int i = 0; i < m_StrikeGrid.Count(); ++i)
                  {
                      m_StrikeGrid[i] = m_reversedGridValue - m_StrikeGrid[i]; 
                  }
                  m_strikeGridReversed = true; 

              }

              public void addToGenericPointOfGrid(DateTime matGridTime, int strikeIdx, double volch)
              {
                  if (m_strikeGridReversed == false) (m_VolCurveSlices[matGridTime])[strikeIdx] += volch;
                  else
                  {
                      int indexrev = m_StrikeGrid.Count() - 1 - strikeIdx;
                      (m_VolCurveSlices[matGridTime])[indexrev] += volch;
                  }
              }

              public void SetToGenericPointOfGrid(DateTime matGridTime, int strikeIdx, double volch)
              {
                  if (m_strikeGridReversed == false) (m_VolCurveSlices[matGridTime])[strikeIdx] = volch;
                  else
                  {
                      int indexrev = m_StrikeGrid.Count() - 1 - strikeIdx;
                      (m_VolCurveSlices[matGridTime])[indexrev] = volch;
                  }
              }  

              /**
              public void addATMShift(DateTime mat, double volsh)
              {
                  // parallel shift of that maturity 
                  for (int i = 0; i < m_StrikeGrid.Count; ++i)
                  {
                      (m_VolCurveSlices[mat])[i] += volsh; 
                  }
              }
              */
  
              public void ChangeATMShift(DateTime mat, double atmVolsh)
              {
                  // parallel shift of that maturity 
                  for (int i = 0; i < m_StrikeGrid.Count; ++i)
                  {
                      (m_VolCurveSlices[mat])[i] = atmVolsh;
                  }
                  m_ATMVolCurve[mat] = atmVolsh;
              }  


             /**
              
              public void addNonBucketSmileShift(double volsh , double strikeLevel)
              {
                  if (m_strikeGridReversed) strikeLevel = this.m_reversedGridValue - strikeLevel; 

                  int  index = m_StrikeGrid.BinarySearch(strikeLevel);
                  if (index < 0) index = ~index;   // past after idx
                  if (index >= m_StrikeGrid.Count()) index = Math.Max(index - 1, 0);

                  for (int i = 0; i < m_VolCurveSlices.Count; ++i)
                  {
                      (m_VolCurveSlices.ElementAt(i)).Value[index] += volsh; 
                  }
              }
                
              */

              public void ChangeNonBucketSmileShift(double volsh, double strikeLevel)
              {
                  if (m_strikeGridReversed) strikeLevel = this.m_reversedGridValue - strikeLevel;

                  int index = m_StrikeGrid.BinarySearch(strikeLevel);
                  if (index < 0) index = ~index;   // past after idx
                  if (index >= m_StrikeGrid.Count()) index = Math.Max(index - 1, 0);

                  for (int i = 0; i < m_VolCurveSlices.Count; ++i)
                  {
                      DateTime mat = m_matKeys.ElementAt(i);
                      double atm;  
                      if(!m_ATMVolCurve.TryGetValue(mat, out atm)) {
                           string msg = "Missing maturity in Vol modification!";
                           throw new PumaMDEException(msg);
                      }
                      // add it 
                      (m_VolCurveSlices.ElementAt(i)).Value[index] = atm + volsh;
                     
                  }
              }         

              public double getVolModification(DateTime mat, double StriKeInPercent)
              {
                  int index_0, index_1;
                  double mod = 0;
                  if (m_strikeGridReversed) StriKeInPercent = this.m_reversedGridValue - StriKeInPercent; 

                  getMaturityBrackets(mat, out index_0, out index_1);
                  if (index_0 == index_1)
                  {
                      mod = 1;
                  }
                  else
                  {
                      double[] vols = new double[2];
                      DateTime[] DT = new DateTime[2];
                      DT[0] = m_matKeys[index_0]; DT[1] = m_matKeys[index_1]; 
                  }
                  return mod; 
              }

              public bool IsOn()
              {
                  return (!(m_StrikeGrid.Count() == 0 || m_VolCurveSlices.Count() == 0 || m_matKeys.Count() == 0));   
              }


              public D2Linterp clone()
              {
                  D2Linterp d2l = new D2Linterp();
                  d2l.m_StrikeGrid = this.m_StrikeGrid;
                  d2l.m_matKeys = this.m_matKeys;
                  d2l.m_VolCurveSlices = this.m_VolCurveSlices;
                  d2l.m_reversedGridValue = this.m_reversedGridValue;
                  d2l.m_strikeGridReversed = this.m_strikeGridReversed;
                  d2l.m_ATMVolCurve = this.m_ATMVolCurve;
                  return d2l; 
              }

              private void getMaturityBrackets(DateTime mat, out int index_0, out int index_1)
              {
                  index_1 = m_matKeys.BinarySearch(mat);
                  if (index_1 < 0) index_1 = ~index_1;   // past after idx
                  if (index_1 >= m_matKeys.Count()) index_1 = Math.Max(index_1 - 1, 0);
                  index_0 = index_1 > 0 ? index_1 - 1 : index_1; // preceeding idx
              }
    }


    [ComVisible(false)]
    public class VolatilityData : DataTable
    {
        #region Constructor

        public VolatilityData()
        {
            VolaClass   = eCommodityVolaClass.Default;
            _VolModDirect = new D2Linterp();
            _VolModFromFather = new D2Linterp();
            _VolModTotal = new D2Linterp();
            _VolModifForGui = new D2Linterp();
            IsVolByDelta = false;
            IsForwardToConvert = false;
            ConversionFactor = 1.0;
            IsVolPinnedToFOCurve = false;
            IsFOCommodityToBePinned = false; 
            DisplayType = eDisplayType.Volatility;
            VolatilityCurveType = mdeMVolatilityCurveType.M_vcCallResult;
            IsVolModifCumulative = false;

        }

        public VolatilityData(Underlying underlying)
            : base(underlying.Reference.Trim()) 
        {
            _Underlying = underlying;
            VolaClass   = eCommodityVolaClass.Underlying;
            IsVolPinnedToFOCurve = false;
            IsFOCommodityToBePinned = false; 
            IsVolByDelta = false;
            IsForwardToConvert = false;
            ConversionFactor = 1.0;
            _VolModDirect = new D2Linterp();
            _VolModFromFather = new D2Linterp();
            _VolModTotal = new D2Linterp();
            _VolModifForGui = new D2Linterp();
            DisplayType = eDisplayType.Volatility;
            VolatilityCurveType = mdeMVolatilityCurveType.M_vcCallResult;
            IsVolModifCumulative = false;
            DataColumn strikesColumn = new DataColumn(_Underlying.Reference.Trim(), typeof(string));
            this.Columns.Add(strikesColumn); 
        }

        public VolatilityData(Classification classification)
            : base(classification.Name.Trim())
        {
            IsVolByDelta = false;
            IsVolPinnedToFOCurve = false;
            IsFOCommodityToBePinned = false; 
            IsForwardToConvert = false;
            ConversionFactor = 1.0;
            _VolModDirect = new D2Linterp();
            _VolModFromFather = new D2Linterp();
            _VolModTotal = new D2Linterp();
            _VolModifForGui = new D2Linterp();
            DisplayType = eDisplayType.Volatility;
            VolatilityCurveType = mdeMVolatilityCurveType.M_vcCallResult;
            IsVolModifCumulative = false;
            DataColumn strikesColumn = new DataColumn(_Classification.Name.Trim(), typeof(string));
            this.Columns.Add(strikesColumn);
        }

        public VolatilityData(HashFutures hashFuture, Underlying parentCommodity)
            : base(hashFuture.SecurityName.Trim())
        {
            _HashFuture = hashFuture;
            _Underlying = parentCommodity;
            VolaClass   = eCommodityVolaClass.HashFuture;
            IsVolByDelta = false;
            IsForwardToConvert = false;
            IsVolPinnedToFOCurve = false;
            IsFOCommodityToBePinned = false; 
            ConversionFactor = 1.0;
            _VolModDirect = new D2Linterp();
            _VolModFromFather = new D2Linterp();
            _VolModTotal = new D2Linterp();
            _VolModifForGui = new D2Linterp();
            DisplayType = eDisplayType.Volatility;
            VolatilityCurveType = mdeMVolatilityCurveType.M_vcCallResult;
            IsVolModifCumulative = false; 
            DataColumn strikesColumn = new DataColumn(hashFuture.SecurityName.Trim(), typeof(string));
            this.Columns.Add(strikesColumn);
        }

        #endregion Constructor

        #region Properties
        public int SophisID
        {
            get
            {
                switch (VolaClass)
                {
                    case eCommodityVolaClass.Underlying:
                        return _Underlying.SophisId;
                    case eCommodityVolaClass.HashFuture:
                        return _HashFuture.Sicovam;
                    default: /* Optional */
                        return 0;
                }
            }
        }
        public int ID 
        {
            get
            {
                switch (VolaClass)
                {
                    case eCommodityVolaClass.Underlying:
                        return _Underlying.Id;
                    case eCommodityVolaClass.Classification:
                        return _Classification.Id;
                    case eCommodityVolaClass.HashFuture:
                        return _HashFuture.Sicovam;
                    default: /* Optional */
                        return 0;
                }
            }
        }
        
       
        internal HashFutures              _HashFuture            { get; set; }
        internal Underlying               _Underlying            { get; set; }
        internal Classification           _Classification        { get; set; }
        private  D2Linterp                _VolModTotal           { get; set; }
        private D2Linterp                 _VolModFromFather      { get; set; }
        private D2Linterp                 _VolModDirect          { get; set; }
        private D2Linterp                 _VolModifForGui        { get; set; } 
        public mdeMVolatilityModelType    VolatilityModelType    { get; set; }
        public mdeMVolatilityCurveType    VolatilityCurveType    { get; set; }
        public mdeMVolatilityPointType    VolatilityPointType    { get; set; }
        public bool                       AbsoluteVolatilityFlag { get; set; }
        public mdeMSmileScaleType         SmileScaleType         { get; set; }
        public eCommodityVolaClass        VolaClass              { get; set; }
        public bool IsVolByDelta                                 { get; set; }
        public bool IsForwardToConvert                           { get; set; }
        public double ConversionFactor                           { get; set; }
        public bool IsVolPinnedToFOCurve                         { get; set; }
        public bool IsFOCommodityToBePinned                      { get; set; }
        public bool  IsManualModificationsAllow                  { get; set; }
        public bool  IsVolModifCumulative                        { get; set; }
        
        private eDisplayType _DisplayType = eDisplayType.Volatility;
        public eDisplayType DisplayType  
        {
            get { return _DisplayType; }
            set { _DisplayType = value;
                  SwitchView();        }
        }

        public IEnumerable<double>       StrikesList
        {
            get
            {
                return this.Rows.Cast<DataRow>().Take(Rows.Count - 1).Select(row => Convert.ToDouble(row[GetReferenceName()]));
            }
        }
        public IEnumerable<DateTime>     MaturitiesList
        {
            get
            {
                return GetMaturityColumns().Select(column => Convert.ToDateTime(column.ColumnName));
            }
        }

        public IEnumerable<string> MaturitiesStrList
        {
            get
            {
                return GetMaturityColumns().Select(column => column.ColumnName);
            }
        }

        internal Dictionary<string/*MaturityDateTimeStr*/, double/*Spread*/> MaturitySpreads = new Dictionary<string, double>();
        //internal Dictionary<string/*MaturityDateTimeStr*/, double/*Spread*/> MaturityCumulativeSpreads = new Dictionary<string, double>();
        internal Dictionary<double/*Strike*/, double/*Shift*/> StrikeShifts = new Dictionary<double, double>();
        //internal Dictionary<double/*Strike*/, double/*Shift*/> StrikeShiftsCumulative = new Dictionary<double, double>();
       
        //private string LogEntries                               { get; set; }
        private string _LogEntries = "";
        public string LogEntries
        {
            get { return _LogEntries; }
            set
            {
                _LogEntries = value;
            }
        }

        #endregion Properties


        #region Methods

        public bool IsForUnderlying()
        { 
            return this.VolaClass.Equals(eCommodityVolaClass.Underlying); 
        }
        public bool IsForHashFuture()
        {
            return this.VolaClass.Equals(eCommodityVolaClass.HashFuture);
        }
        public bool IsForClassification()
        {
            return this.VolaClass.Equals(eCommodityVolaClass.Classification);
        }

        public void CreateGrid() //currently used only for Underlying
        {
            if (_Underlying == null) { return; }

            Engine.Instance.Log.Debug("Commodity:Data starting CreateGrid.");

            Classifications rootClassifications = _Underlying.Classifications;
            Classification rootClassification   = rootClassifications[0];
            List<double> strikesList            = rootClassification.GetCommodityStrikesGrid(true);
            List<double> relativeStrikesGrid    = strikesList.ToList().Select(strike => strike * 100).ToList();

            var maturitySchedule                = _Underlying.GetMaturityScheduleProvider();
            IList<DateTime> maturitiesList      = new List<DateTime>();

            if (strikesList.Count == 0 || maturitiesList.Count == 0)
                throw new PumaMDEException("CreateGrid - zero length strikesList,maturitiesList ");

            this.AddStrikesGrid(relativeStrikesGrid);
            this.AddMaturities(maturitiesList);

            this.VolatilityCurveType            = mdeMVolatilityCurveType.M_vcCallResult;
            this.VolatilityModelType            = mdeMVolatilityModelType.M_vmAbsolute;
            this.SmileScaleType                 = mdeMSmileScaleType.M_ssPercentageStrike;
            this.VolatilityPointType            = mdeMVolatilityPointType.M_vpAbsoluteDate;
            this.VolaClass                      = eCommodityVolaClass.Underlying;
            this.AbsoluteVolatilityFlag         = true;

            Engine.Instance.Log.Debug("Commodity:Data finished CreateGrid.");
        }

        
        public void SetStrikeGridToPercentageFwd()
        {
          
            List<double> strikesList            = _Classification.GetCommodityStrikesGridInPercentage();
            List<double> relativeStrikesGrid    = strikesList.ToList().Select(strike => strike * 100).ToList();
            if (strikesList.Count == 0)
                throw new PumaMDEException("Reset Strike Grid in Percentage - zero length strikesList");

            this.AddStrikesGrid(relativeStrikesGrid);
        }
        

        public IEnumerable<DataColumn> GetMaturityColumns()
        {
            return this.Columns.Cast<DataColumn>().Skip(1);
        }

        public void AddMaturity(string maturityStr, Type dataType)
        {
            DataColumn maturityColumn = new DataColumn(maturityStr, dataType);
            this.Columns.Add(maturityColumn);
        }

        public void AddMaturities(IList<DateTime> maturitiesList)
        {
            Engine.Instance.Log.Debug("Commodity:Data AddMaturities starting.");
         
            if (false)
                { return; }

            Engine.Instance.Log.Debug("Commodity:Data AddMaturities finished.");
        }

        public void ClearMaturities()
        {
            if (this.Columns.Count > 1)
            {
                for (int index = this.Columns.Count - 1; index > 0; index--)
                { this.Columns.RemoveAt(index); }
            }
        }

        public void ClearStrikes()
        {
            if (this.Rows.Count > 1)
            {
                for (int index = this.Rows.Count - 1; index > 0; index--)
                { this.Rows.RemoveAt(index); }
            }
        }

        public void AddStrikesGrid(IEnumerable<double> strikesList)
        {
            if (false)
            { return; }
            Engine.Instance.Log.Debug("Commodity:Data AddStrikesGrid starting.");

            this.Rows.Clear();
            string referenceStr = GetReferenceName();            
            DataRow newATMStrikeRow = this.NewRow();
            newATMStrikeRow[referenceStr] = "ATM";
            this.Rows.Add(newATMStrikeRow);

            Engine.Instance.Log.Debug("Commodity:Data AddStrikesGrid finished.");
        }

        public void AddDeltaStrikeGrid(List<double> PartialdeltaStrikeGrid, object comp)
        {
            if (comp == null) { return; }

            Engine.Instance.Log.Debug("Commodity:Data AddDeltaStrikeGrid starting.");

            List<double> totalStrikeGrid = new List<double>();
            for (int i = 0; i < PartialdeltaStrikeGrid.Count(); ++i)
            {
                totalStrikeGrid.Add(100 - PartialdeltaStrikeGrid[i]); 
            }
            totalStrikeGrid.Add(1);
            for (int i = PartialdeltaStrikeGrid.Count()-1; i >=0; --i)
            {
                totalStrikeGrid.Add(PartialdeltaStrikeGrid[i]); 
            }

            this.ClearStrikes();
            string referenceStr = GetReferenceName();
            for (int i = 0; i < totalStrikeGrid.Count(); ++i)
            {
                DataRow newStrikeRow = this.NewRow();
                newStrikeRow[referenceStr] = totalStrikeGrid.ElementAt(i);
                this.Rows.Add(newStrikeRow);
            }
            DataRow newATMStrikeRow = this.NewRow();
            newATMStrikeRow[referenceStr] = "ATM";
            this.Rows.Add(newATMStrikeRow);
            this.IsVolByDelta = true;
            Engine.Instance.Log.Debug("Commodity:Data AddDeltaStrikeGrid finished.");
        }


        public void AddDeltaStrikeGrid(object comp)
        {
            if (comp ==null)  { return; }
            Engine.Instance.Log.Debug("Commodity:Data AddDeltaStrikeGrid starting.");
            //this.Rows.Clear();
            this.ClearStrikes();
            string referenceStr = GetReferenceName();
            int nstrike = 1;
            for (int i = 0; i < nstrike; ++i)
            {
                DataRow newStrikeRow = this.NewRow();
                newStrikeRow[referenceStr] = 1;
                this.Rows.Add(newStrikeRow); 
            }
            DataRow newATMStrikeRow = this.NewRow();
            newATMStrikeRow[referenceStr] = "ATM";
            this.Rows.Add(newATMStrikeRow);
            this.IsVolByDelta = true;
            Engine.Instance.Log.Debug("Commodity:Data AddDeltaStrikeGrid finished.");
        }

        public double GetConversionAdj(DateTime mat, double fwdfather)
        {
            if (IsForwardToConvert == false) return 1.0;
            int fe = 1;
            double fwdchild = 1 / ConversionFactor;
            if( fwdfather >0) return fwdchild / fwdfather;
            return 1.0; 
        }

        public void PinVolToFOCurve()
        {
            if (IsVolByDelta) return;

           try
            {
                double[] strikegrid = StrikesList.ToArray();
                int[] timeGrid = new int[MaturitiesList.Count()];
                for (int i = 0; i < MaturitiesList.Count(); ++i) timeGrid[i] = 1;
                double[] surface = new double[strikegrid.Length * timeGrid.Length];
                int index = 0; double fwd, strike, vol;
                for (int i = 0; i < timeGrid.Length; ++i)
                {
                    DateTime dt = MaturitiesList.ElementAt(i);
                    for (int k = 0; k < strikegrid.Length; ++k)
                    {
                        strike = strikegrid[k] * 1 / 100.0;
                        vol = GetVolatility(dt, k);
                        surface[index] = vol;
                        index += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                IsVolPinnedToFOCurve = false; 
                Engine.Instance.ErrorException(String.Format("exception in Pinning the Commodity Volatility for referece ={0}", ""), ex);
            }
            IsVolPinnedToFOCurve = true; 
        }

        public void PinATMVolToFOCurve()
        {
            try
            {
                double[] strikegrid = new double [1];
                strikegrid[0] = 100;
                int[] timeGrid = new int[MaturitiesList.Count()];
                for (int i = 0; i < MaturitiesList.Count(); ++i) timeGrid[i] = 1;
                double[] surface = new double[strikegrid.Length * timeGrid.Length];
                int index = 0; double fwd, vol;
                for (int i = 0; i < timeGrid.Length; ++i)
                {
                    DateTime dt = MaturitiesList.ElementAt(i);
                     vol = GetATMVolatility(dt); 
                     surface[index] = vol;
                     index += 1;
                }
            }
            catch (Exception ex)
            {
                Engine.Instance.ErrorException(String.Format("exception in Pinning the Commodity Volatility for referece ={0}", ""), ex);
            }
        }

        public void PinVolFromFatherFOCurve(VolatilityData fatherVolData)
        {
            if (IsVolByDelta) return;

            try
            {
                double[] strikegrid = fatherVolData.StrikesList.ToArray();
                int[] timeGrid = new int[fatherVolData.MaturitiesList.Count()];
                for (int i = 0; i < fatherVolData.MaturitiesList.Count(); ++i) timeGrid[i] = 1;
                double[] surface = new double[strikegrid.Length * timeGrid.Length];
                int index = 0; double fwd, strike, vol;
                for (int i = 0; i < timeGrid.Length; ++i)
                {
                    DateTime dt = fatherVolData.MaturitiesList.ElementAt(i);
                    fwd = 1;
                    for (int k = 0; k < strikegrid.Length; ++k)
                    {
                        strike = strikegrid[k] * fwd / 100.0;
                        vol = fatherVolData.GetVolatility(dt, k);
                        surface[index] = vol;
                        index += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                IsVolPinnedToFOCurve = false;
                Engine.Instance.ErrorException(String.Format("exception in Pinning the Commodity Volatility for referece ={0}", ""), ex);
            }
            IsVolPinnedToFOCurve = false;
        }


        public void InitializeVolModificatorFromFather(List<DateTime> matgrid, List<double> strikeGrid)
        {
            if (_VolModFromFather != null) _VolModFromFather.Initialize(matgrid, strikeGrid);
        }

        public void InitializeDirectVolModificator(List<DateTime> matgrid, List<double> strikeGrid)
        {
            if (_VolModDirect != null) _VolModDirect.Initialize(matgrid, strikeGrid);
        }

        public void InitializeTotalVolModificator(List<DateTime> matgrid, List<double> strikeGrid)
        {
            if (_VolModTotal != null) _VolModTotal.Initialize(matgrid, strikeGrid);
        }

        

        public bool VolModificatorsDirectIsOn()
        {
            if (_VolModDirect == null) return false;
            return _VolModDirect.IsOn(); 
        }

        public bool VolModificatorsFromFatherIsOn()
        {
            if (_VolModFromFather == null) return false;
            return _VolModFromFather.IsOn();
        }

        public bool VolModificatorsTotalIsOn()
        {
            if (_VolModTotal == null) return false;
            return _VolModTotal.IsOn();
        }

        public bool VolModificatorsForDeltaIsOn()
        {
            return (VolModificatorsTotalIsOn() || VolModificatorsDirectIsOn());  
            
        }

        public bool VolModificatorsForGuiIsOn()
        {
            if (_VolModifForGui== null) return false;
            return _VolModifForGui.IsOn();
        }
     

        public void addGenericPointOfGridDirectMod(DateTime matGridTime, int strikeIdx, double volch)
        {
            if (_VolModDirect.IsOn()) _VolModDirect.addToGenericPointOfGrid(matGridTime, strikeIdx, volch);
        }

        public void SetGenericPointOfGridDirectMod(DateTime matGridTime, int strikeIdx, double volch)
        {
            if (_VolModDirect.IsOn()) _VolModDirect.SetToGenericPointOfGrid(matGridTime, strikeIdx, volch);
        }

        public void SetFatherGenericPointOfGrid(DateTime matGridTime, int strikeIdx, double volch)
        {
            if (_VolModFromFather.IsOn()) _VolModFromFather.SetToGenericPointOfGrid(matGridTime, strikeIdx, volch);
        }

        public void SetTotalGenericPointOfGrid(DateTime matGridTime, int strikeIdx, double volch)
        {
            if (_VolModTotal.IsOn()) _VolModTotal.SetToGenericPointOfGrid(matGridTime, strikeIdx, volch);
        }
        
        public double GetTotalVolSmileModification(DateTime mat, double strikeInPercentage)
        {
            if (IsVolByDelta) return 0.0;
            double mod = 0.0;
            // in case the total modifications are not yet initialized give the direct modification
            if (_VolModTotal.IsOn()) mod = _VolModTotal.getVolModification(mat, strikeInPercentage); 
            else if (_VolModDirect.IsOn()) mod = _VolModDirect.getVolModification(mat, strikeInPercentage);  
            return mod; 
        }

        public double GetFatherVolSmileModification(DateTime mat, double strikeInPercentage)
        {
            if (IsVolByDelta) return 0.0;
            double mod = 0.0;
            if (_VolModFromFather.IsOn()) mod = _VolModFromFather.getVolModification(mat, strikeInPercentage);
            return mod;
        }

        public double GetDirectVolSmileModification(DateTime mat, double strikeInPercentage)
        {
            if (IsVolByDelta) return 0.0;
            double mod = 0.0;
            if (_VolModDirect.IsOn()) mod = _VolModDirect.getVolModification(mat, strikeInPercentage);
            return mod;
        }

      
        public double GetVolSmileModificationGui(DateTime mat, double strikeInPercentage)
        {
            if (IsVolByDelta) return 0.0;
            double mod = 0.0;
            if (_VolModifForGui.IsOn()) mod = _VolModifForGui.getVolModification(mat, strikeInPercentage);
            /**
             if (_VolModFromFather.IsOn())
            {
                mod += _VolModFromFather.getVolModification(mat, strikeInPercentage);
            }
            */
            return mod;
        }

       
 
        public void resetVolModifications()
        {
            if (_VolModDirect.IsOn()) _VolModDirect.clear();
            if (_VolModFromFather.IsOn()) _VolModFromFather.clear();
            if (_VolModTotal.IsOn()) _VolModTotal.clear(); 
        }

       

        public void SetATMVolatility(DateTime maturity, double volatility)
        {
            SetVolatility( maturity, this.Rows.Count-1,  volatility);
        }

        public void SetVolatility(DateTime maturity, int strikeIndex, double volatility)
        {
            //this.Rows[strikeIndex][maturity.ToShortDateString()] = volatility; /// Math.Round(volatility, 3).ToString(); 

            this.Rows[strikeIndex].SetField(maturity.ToShortDateString(), volatility);     
        }

        private void ShiftSmileSection(double strikeLevel, double volshift)
        {
            if (StrikesList.Count() == 0) return; 
            int indexStrike = StrikesList.ToList().IndexOf(strikeLevel);
            if (indexStrike < 0) return; 
            
            double volat = 0;
            for (int i = 0; i < MaturitiesList.Count(); ++i)
            {
                string name = MaturitiesList.ElementAt(i).ToShortDateString();
                volat = Convert.ToDouble((this.Rows[indexStrike][name])); 
                this.Rows[indexStrike][name] = volat + volshift;
                this.Rows[indexStrike].SetField(name, (volat + volshift)); 
            }

        }


        private void ShiftVolCurveSection(DateTime maturity, double volshift)
        {
            string  name = maturity.ToShortDateString();
            for (int i = 0; i < this.Rows.Count - 1; ++i)
            {
                double val = Convert.ToDouble((this.Rows[i][name])); 
                val+=  volshift;
                this.Rows[i][name] = val; 
            }

        }

        public double GetATMVolatility(DataColumn maturityCol)
        {
            return  GetATMVolatility (maturityCol.ColumnName) ;
        }

        public double GetATMVolatility(DateTime maturity)
        {
           return GetATMVolatility( maturity.ToShortDateString() );
        }

        public double GetATMVolatility(string maturityStr)
        {
            object volaObj = this.Rows[this.Rows.Count - 1][maturityStr];

            return volaObj == null ? 0 : Convert.ToDouble(volaObj);
        }

        public double GetVolatility(DateTime maturity, int strikeIndex)
        {
            return GetVolatility(maturity.ToShortDateString(), strikeIndex);
        }

        public double GetVolatility(DataColumn maturityCol, int strikeIndex)
        {
            return GetVolatility (maturityCol.ColumnName , strikeIndex );
        }

        public double GetVolatility(string maturityStr, int strikeIndex)
        {
            object volaObj = this.Rows[strikeIndex][maturityStr];
            double volatilityValue = Convert.ToDouble(volaObj);

            if (DisplayType.Equals(eDisplayType.Smile))
            { volatilityValue += GetATMVolatility(maturityStr); }

            return volatilityValue;
        }

        public double GetStrike(int strikeIndex)
        {
            object strikeObj = this.Rows[strikeIndex][GetReferenceName()];
            return Convert.ToDouble(strikeObj);
        }

        public string GetReferenceName()
        {
            switch (VolaClass)
            {
                case eCommodityVolaClass.Underlying:
                    return _Underlying.Reference.Trim();
                case eCommodityVolaClass.Classification:
                    return _Classification.Name.Trim();
                case eCommodityVolaClass.HashFuture:
                    return _HashFuture.SecurityName.Trim();
                default: /* Optional */
                    return string.Empty;
            }
        }

        public double GetTheoreticalValue()
        {
            return _Underlying == null ? 0.0 : 1;
        }

        private void SwitchView()
        {
            foreach (DataColumn maturitySlice in this.Columns)
            {
                if (maturitySlice.Ordinal == 0) { continue; } //we are in Stikes DataColumn
                double atmVolatility = GetATMVolatility(maturitySlice);

                foreach (DataRow StrikeRow in Rows)
                {
                    int rowIndex = Rows.IndexOf(StrikeRow);
                    if (rowIndex == Rows.Count - 1 ) { continue; }
                    double volatility = Convert.ToDouble(StrikeRow[maturitySlice.ColumnName]);
                    if (DisplayType.Equals(eDisplayType.Smile)) //Volatility -> Smile
                    {
                        StrikeRow[maturitySlice.ColumnName] = volatility - atmVolatility;
                    }
                    else if (DisplayType.Equals(eDisplayType.Volatility)) //Smile -> Volatility
                    {
                        StrikeRow[maturitySlice.ColumnName] = volatility + atmVolatility;
                    }
                }
            }
        }

        public void SetDisplayTypeSilently(eDisplayType displayType)
        {
            this._DisplayType = displayType;
        }

        public VolatilityData CloneVolaData()
        {
            VolatilityData newData = new VolatilityData();

            newData._HashFuture             = this._HashFuture;
            newData._Underlying             = this._Underlying;
            newData._Classification         = this._Classification;
            newData.VolatilityModelType     = this.VolatilityModelType;
            newData.VolatilityCurveType     = this.VolatilityCurveType;
            newData.VolatilityPointType     = this.VolatilityPointType;
            newData.AbsoluteVolatilityFlag  = this.AbsoluteVolatilityFlag;
            newData.SmileScaleType          = this.SmileScaleType;
            newData.VolaClass               = this.VolaClass;
            newData.SetDisplayTypeSilently(this.DisplayType);
            newData.IsVolByDelta            = this.IsVolByDelta;
            newData.IsForwardToConvert      = this.IsForwardToConvert;
            newData.ConversionFactor        = this.ConversionFactor;
            newData._VolModDirect           = this._VolModDirect;
            newData._VolModFromFather       = this._VolModFromFather;
            newData._VolModTotal            = this._VolModTotal;
            newData._VolModifForGui         = this._VolModifForGui;
            newData.IsVolPinnedToFOCurve    = this.IsVolPinnedToFOCurve;
            newData.IsFOCommodityToBePinned = this.IsFOCommodityToBePinned;
            newData.IsVolModifCumulative = this.IsVolModifCumulative;
            DataTable data = this as DataTable;
            newData.Clear();
            newData.Merge(data);
            newData.AcceptChanges();

            return newData;
        }

        public void CopyAll(VolatilityData newData)
        {
            this._HashFuture            = newData._HashFuture;
            this._Underlying            = newData._Underlying;
            this._Classification        = newData._Classification;
            this.VolatilityModelType    = newData.VolatilityModelType;
            this.VolatilityCurveType    = newData.VolatilityCurveType;
            this.VolatilityPointType    = newData.VolatilityPointType;
            this.AbsoluteVolatilityFlag = newData.AbsoluteVolatilityFlag;
            this.SmileScaleType         = newData.SmileScaleType;
            this.VolaClass              = newData.VolaClass;
            this.SetDisplayTypeSilently(newData.DisplayType);
            this.IsVolByDelta = newData.IsVolByDelta;
            this.IsForwardToConvert = newData.IsForwardToConvert;
            this.ConversionFactor  = newData.ConversionFactor;
            this._VolModDirect = newData._VolModDirect;
            this._VolModFromFather = newData._VolModFromFather;
            this._VolModTotal = newData._VolModTotal;
            this._VolModifForGui   = newData._VolModifForGui;
            this.IsVolPinnedToFOCurve = newData.IsVolPinnedToFOCurve;
            this.IsFOCommodityToBePinned = newData.IsFOCommodityToBePinned;
            this.IsVolModifCumulative = newData.IsVolModifCumulative; 

            DataTable thisDataTable  = this as DataTable;
            thisDataTable.Clear();
            thisDataTable.Columns.Clear();
            thisDataTable.Merge(newData);

            thisDataTable.AcceptChanges();
        }

        public bool getModificationCopyForDelta(out D2Linterp modifForDelta)
        {
            modifForDelta = null; 
            if (_VolModTotal.IsOn()) {
                modifForDelta = _VolModTotal.clone();
                return true;
            }
            else if (_VolModDirect.IsOn())
            {
                modifForDelta = _VolModDirect.clone();
                return true; 
            }
            return false; 
        }

        public void CopyOnlyGrid(VolatilityData newData)
        {
            this.VolatilityModelType    = newData.VolatilityModelType;
            this.VolatilityCurveType    = newData.VolatilityCurveType;
            this.VolatilityPointType    = newData.VolatilityPointType;
            this.AbsoluteVolatilityFlag = newData.AbsoluteVolatilityFlag;
            this.SmileScaleType         = newData.SmileScaleType;

            this.SetDisplayTypeSilently(newData.DisplayType);

            DataTable thisDataTable = this as DataTable;
            thisDataTable.Clear();
            thisDataTable.Columns.Clear();
            thisDataTable.Merge(newData);
            if ( thisDataTable.Columns.Count > 0)
                { thisDataTable.Columns[0].ColumnName = this.GetReferenceName(); }

            thisDataTable.AcceptChanges();
        }

        public double GetATMSpread(string MaturityStr)
        {
            double spread ;
            if (MaturitySpreads.TryGetValue(MaturityStr, out spread))
                { return spread; }
            return 0.0;
        }

      /*  public double GetATMCumulativeSpread(string MaturityStr)
        {
            double Cspread;
            if (MaturityCumulativeSpreads.TryGetValue(MaturityStr, out Cspread))
            { return Cspread; }
            return 0.0;
        }

       */

        public double GetStrikeShift(double strike)
        {
            double shift;
            if (StrikeShifts.TryGetValue(strike, out shift))
                { return shift; }
            return 0.0;
        }

        /**
        public double GetStrikeShiftCumulative(double strike)
        {
            double shift;
            if (StrikeShiftsCumulative.TryGetValue(strike, out shift))
            { return shift; }
            return 0.0;
        }
        */
         
        public void SetATMSpread(IEnumerable<VolatilityDataAtmMode> VolatilityDataAtmCollection)
        {

            var spreadCollection = VolatilityDataAtmCollection;
            if (spreadCollection.Count() == 0) { return; }

            bool ToCascade = (this.VolaClass == eCommodityVolaClass.Classification);

            if (ToCascade && !_VolModDirect.IsOn())
            {
                _VolModDirect.Initialize(MaturitiesList.ToList(), StrikesList.ToList());
                if (IsVolByDelta) _VolModDirect.setReversedGridValue(100);
                if (!IsVolByDelta)
                {
                    _VolModifForGui.Initialize(MaturitiesList.ToList(), StrikesList.ToList());
                }
            }

            foreach (VolatilityDataAtmMode Slice in spreadCollection)
            {
                MaturitySpreads[Slice.MaturityDate.ToShortDateString() ] = Slice.Spread;
                DateTime mat = Convert.ToDateTime(Slice.Maturity);

                if (ToCascade) _VolModDirect.ChangeATMShift(mat, Slice.Spread); //n
                else ShiftVolCurveSection(mat, Slice.Spread);

                if (!IsVolByDelta && ToCascade) _VolModifForGui.ChangeATMShift(mat, Slice.Spread);
            }


            if (VolaClass == eCommodityVolaClass.Classification) return; // for classification changes are on computer 
            
            if (DisplayType.Equals(eDisplayType.Smile))
            {
                DataRow AtmStrikeRow = this.Rows.Cast<DataRow>().Where(r => r[0].ToString() == "ATM").Single();
                foreach (VolatilityDataAtmMode Slice in spreadCollection)
                {
                    AtmStrikeRow[Slice.Maturity] = Slice.Spread;
                }
                return;
            }

            foreach (VolatilityDataAtmMode Slice in spreadCollection )
            {
                foreach (DataRow strikeRow in Rows)
                {
                    double volatility = Convert.ToDouble(strikeRow[Slice.Maturity]);
                    strikeRow[Slice.Maturity] = volatility + Slice.Spread;
                }
            }

            if (VolaClass == eCommodityVolaClass.Underlying) this.AcceptChanges(); 

        }

        public void ShiftStrikes(IEnumerable<VolatilityDataSmileMode> VolatilityDataSmileShifts)
        {

            var smileshiftCollection = VolatilityDataSmileShifts;
            if (smileshiftCollection.Count() == 0) { return; }

            bool ToCascade = (this.VolaClass == eCommodityVolaClass.Classification);

            List<double> strikeList = StrikesList.ToList();
            if (ToCascade && !_VolModDirect.IsOn())
            {
                _VolModDirect.Initialize(MaturitiesList.ToList(), strikeList);
                if (IsVolByDelta) _VolModDirect.setReversedGridValue(100);
                if (!IsVolByDelta) _VolModifForGui.Initialize(MaturitiesList.ToList(), StrikesList.ToList());
            }

            foreach (VolatilityDataSmileMode smileshift in smileshiftCollection)
            {
                double newShift = smileshift.Shift;
                double oldShift = GetStrikeShift(smileshift.Smile);
                StrikeShifts[smileshift.Smile] = newShift;

                if (!IsVolByDelta && ToCascade) _VolModifForGui.ChangeNonBucketSmileShift(smileshift.Shift, smileshift.Smile);

                if (ToCascade) _VolModDirect.ChangeNonBucketSmileShift(smileshift.Shift, smileshift.Smile);
                else ShiftSmileSection(smileshift.Smile, newShift);
            }

            if (VolaClass == eCommodityVolaClass.Underlying) this.AcceptChanges(); 

             
        }

        public void AddInfoLog(string logEntry)  { this.AddLog(logEntry, LogLevel.Info);  }
        public void AddDebugLog(string logEntry) { this.AddLog(logEntry, LogLevel.Debug); }
        public void AddErrorLog(string logEntry) { this.AddLog(logEntry, LogLevel.Error); }
        public void AddErrorLog(string logEntry,Exception ex) { this.AddLog(logEntry, ex); }
        
        internal void AddLog(string logEntry, LogLevel logLevel)
        {
            string logStr  = logEntry + Environment.NewLine;
            
            if (logLevel.Equals(LogLevel.Error))
            {
                Engine.Instance.Log.Error(logStr);
                logStr = " ERROR: " + logStr;
            }
            else if (logLevel.Equals(LogLevel.Info))
            { 
                Engine.Instance.LogInfo(logStr);
            }
            else if (logLevel.Equals(LogLevel.Debug))
            {
                Engine.Instance.Log.Debug(logStr);
            }
            
            this.LogEntries += logStr;
        }

        internal bool HasLog()
        {
            
            return !string.IsNullOrEmpty(this.LogEntries) ;
        }


        internal void AddLog(string logEntry, Exception ex)
        {
            string logStr = logEntry + Environment.NewLine;

            Engine.Instance.ErrorException(logEntry, ex);

            this.LogEntries += " ERROR: " +  logStr;
        }


        internal string GetLogs()
        {
           return GetReferenceName() +":" + this.LogEntries.Trim() + Environment.NewLine;
        }

        #endregion Methods



    }
}
