using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Globalization;
using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("ED13C82A-6178-454c-AD54-31CC2C910861")]
    [ComVisible(true)]

    [Serializable]
    public class SettingsSetRule
    {
        public enum SystemVolatilitySourceTypeEnum
        {
            MDE = 0,
            Sophis = 1
        } ;

        public enum CutOffTypeEnum
        {
            Automatic,
            UserDefined,
            DefaultAdjusted,
            Detection,
            FromBenchmark
        } ;

        public enum SophisPublishingModeType
        {
            VU = 0,
            Direct
        };

        public SettingsSetRule Clone()
        {
            //var retval= DataFactory.Clone<SettingsSetRule>(this);
            var retval = (SettingsSetRule)(this.MemberwiseClone());

            retval.DefaultCutoffs = new List<DefaultCutoff>();

            foreach (DefaultCutoff co in DefaultCutoffs)
            {
                retval.AddCutoff(co.Clone());
            }

            retval.ForwardTolerances = new List<ForwardTolerance>();

            foreach (ForwardTolerance co in ForwardTolerances)
            {
                retval.AddTolerance(co.Clone());
            }

            retval.ExtrapolationSlices = new List<ExtrapolationSlice>();

            foreach (var es in ExtrapolationSlices)
            {
                retval.AddExtrapolationSlice(es.Clone());
            }

            retval.CubicSplineParams = new List<CubicSplineParam>();

            foreach (CubicSplineParam param in CubicSplineParams)
            {
                retval.AddCubicSplineParam(param.Clone());
            }

            return retval;
        }

        public SettingsSetRule()
        {
            DefaultCutoffs = new List<DefaultCutoff>();
            ExtrapolationSlices = new List<ExtrapolationSlice>();
            ForwardTolerances = new List<ForwardTolerance>();
            CubicSplineParams = new List<CubicSplineParam>();

            ATMVolSmoothing = 0.0;
            ATMChangeDecreasing = false;

            SkewSmoothing =0.0;
            SkewFlatning = false;
            SkewChangeDecreasing = false;

            PutConvSmoothLt = 0.0;
            PutConvSmoothSt = 0.0;
            CallConvSmoothLt = 0.0;
            CallConvSmoothSt = 0.0;
            MatBoundLtSt = 0.0;

            MaxConvDecreasePerYear = 2e+19;
            MaxConvIncreasePerYear = 0;

            ConvexityChangeDecreasing = false;
            TotalAbsDiffPcCc = 2e+19;

            VolCapFactor = 2e+19;
            VolCapSmoothRange = 0.03;

            PenaltySystemVolatilitySTLTMaturity = 0.5;
            PenaltySystemVolatilityST = 0.05;
            PenaltySystemVolatilityLT = 0.5;
            MinSpread = 0.005;
            MaxSpread = 0.05;

            LbVc = 0.0005;
            UbVc = 4;
            LbSc = -2e+19;
            UbSc = 0.02;
            LbPc = -0.05;
            UbPc = 50;
            LbCc = -0.05;
            UbCc = 50;

            LbVcRel = 0;
            UbVcRel = 2e19;

            OrcCurvFloor = -0.05;

            NoArbitrageConstraints = true;
            CheckArbitrage = false;

            StrikeMin = 0.1;
            StrikeMax = 2.0;
            StrikeStep = 0.04;
            NumberOfPublishingIterations = 1;
            SecondsBetweenPublishingIterations = 0;
            

            UseStrippedReposForImpliedVolatilityalculationsAndPublishing=false ;
            StrippedForwardRelativeTolerance = 0.05 ;
            
            ATMBoost =1 ;
            ATMBoostMaturity ="6m";

            ATMBoostStrikeMin =0.9;
            ATMBoostStrikeMax = 1.1;

            UseShortMaturityArtificialSliceExtrapolation = false;
            MissingShortMaturitiesExtrapolation = false;

            StartNonpositiveSkew = 1E19;

            SystemVolatilitySource = SystemVolatilitySourceTypeEnum.MDE;

            GridWingMin = 0.1;
            GridWingMax = 2;
            GridWingStep = 0.08;
            GridATMMin = 0.7;
            GridATMMax = 1.3;
            GridATMStep = 0.04;

            SophisPublishingMode = SophisPublishingModeType.VU;

            SystemCutoffTolerance = 0.01 ;
            ExtendedStrikeRange = 0;

        }

        public int Id { get; set; }
        public int TypeId { get; set; }
        public int ClassificationId { get; set; }

        public double ATMVolSmoothing { get; set; }
        
        public double MaxConvDecreasePerYear { get; set; }
        public double MaxConvIncreasePerYear { get; set; }

        public double SkewSmoothing { get; set; }
        public CutOffTypeEnum UserCutOffs { get; set; }
        public double OrcCurvFloor {get; set;}
        public bool SkewFlatning {get; set;}
        public bool NoArbitrageConstraints {get;set;}
        public double TotalAbsDiffPcCc { get; set; }
        public double LbVc { get; set; }
        public double UbVc { get; set; }
        public double LbSc { get; set; }
        public double UbSc { get; set; }
        public double LbPc { get; set; }
        public double UbPc { get; set; }
        public double LbCc { get; set; }
        public double UbCc { get; set; }

        public double LbVcRel { get; set; }
        public double UbVcRel { get; set; }
        
        public double VolCapFactor { get; set; }
        public double VolCapSmoothRange { get; set; }
        public double MatBoundLtSt { get; set; }
        public double PutConvSmoothLt { get; set; }
        public double PutConvSmoothSt { get; set; }
        public double CallConvSmoothLt { get; set; }
        public double CallConvSmoothSt { get; set; }

        public double StrikeMin      { get; set; }
        public double StrikeMax      { get; set; }
        public double StrikeStep     { get; set; }

        public double GridWingMin { get; set; }
        public double GridWingMax { get; set; }
        public double GridWingStep { get; set; }

        public double GridATMMin { get; set; }
        public double GridATMMax { get; set; }
        public double GridATMStep { get; set; }

        public bool   CheckArbitrage { get; set; }

        public double PenaltySystemVolatilityLT { get; set; }
        public double PenaltySystemVolatilityST { get; set; }

        public double PenaltySystemVolatilitySTLTMaturity { get; set; }

        public SystemVolatilitySourceTypeEnum SystemVolatilitySource { get; set; }

        public double MinSpread { get; set; }
        public double MaxSpread { get; set; }

        public bool ATMChangeDecreasing { get; set; }
        public bool SkewChangeDecreasing { get; set; }
        public bool ConvexityChangeDecreasing { get; set; }

        public double StartNonpositiveSkew { get; set; }

        public double CallPutParityCheckThreshold { get; set; }
        public int NumberOfPublishingIterations { get; set; }
        
        public int SecondsBetweenPublishingIterations {get;set;}

        public bool UseStrippedReposForImpliedVolatilityalculationsAndPublishing { get; set; }
        public double StrippedForwardRelativeTolerance { get; set; }

        public double TargetVolatility3MCutOffs { get; set; }

        public double ATMBoost { get; set; }
        public string ATMBoostMaturity { get; set; }
        public double ATMBoostStrikeMin { get; set; }
        public double ATMBoostStrikeMax { get; set; }

        public bool UseShortMaturityArtificialSliceExtrapolation { get; set; }
        public bool MissingShortMaturitiesExtrapolation { get; set; }

        public string ShortMaturityReferenceTimeStamp { get; set; }

        public SophisPublishingModeType SophisPublishingMode { get; set; }

        public double SystemCutoffTolerance { get; set; }
        public double ExtendedStrikeRange { get; set; }

        public double CheckPublishingMaturity { get; set; }
        public double CheckPublishingSpread { get; set; }

        public bool ExcludeCalendarNoArbitrageConstraints { get; set; }
        public double SplineGridMinimumDistance { get; set; }

        public DateTime ATMBoostMaturityDate
        {
            get
            {
                DateTime retval = DateTime.Today;
                if (!String.IsNullOrEmpty(ATMBoostMaturity))
                {
                    try
                    {
                        try
                        {
                            retval = DateTime.ParseExact(ATMBoostMaturity, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            retval = new DateTime();
                        }
                    }
                    catch
                    {
                    }
                }
                return retval;
            }
        }

        [ComVisible(false)]
        public IList<DefaultCutoff> DefaultCutoffs { get; set; }

        public DefaultCutoffs DefaultCutoffsCOllection
        {
            get
            {
                return new DefaultCutoffs(DefaultCutoffs);
            }
        }

        public void AddCutoff(DefaultCutoff co)
        {
            co.SettingsSetRule = this;
            DefaultCutoffs.Add(co);
        }

        DateTime[] cachedMats = null;
        double[] cachedDownValues = null;
        double[] cachedUpValues = null;
        
        public double GetDownCutoff(Underlying und, double volLevel3M, DateTime expiry)
        {
            if (UserCutOffs!=CutOffTypeEnum.DefaultAdjusted)
                throw new PumaMDEException("type of cutoff should be 'Preset cutoffs, moving with 3m ATM vol'");

            double retval = 0;

            DateTime[] mats = null;
            if (cachedMats != null)
                mats = cachedMats;
            else
                cachedMats = mats = DefaultCutoffs.OrderBy(x1 => x1.Maturity).Select(x2 => x2.Maturity).ToArray();

            double[] values = null;
            if (cachedDownValues != null)
                values = cachedDownValues;
            else
                cachedDownValues = values = DefaultCutoffs.OrderBy(x1 => x1.Maturity).Select(x2 => x2.DownCut).ToArray(); 

            double dcut = 1;

            int numberofdays = 1; 

            retval = (1 + (10 - Math.Log(numberofdays)) / 10 * 
                (volLevel3M - TargetVolatility3MCutOffs) / (TargetVolatility3MCutOffs)) * dcut;

            return retval ;
        }

        public double GetUpperCutoff(Underlying und, double volLevel3M, DateTime expiry)
        {
            if (UserCutOffs != CutOffTypeEnum.DefaultAdjusted)
                throw new PumaMDEException("type of cutoff should be 'Preset cutoffs, moving with 3m ATM vol'");

            double retval = 0;

            DateTime[] mats = null;
            if (cachedMats != null)
                mats = cachedMats;
            else
                cachedMats = mats = DefaultCutoffs.OrderBy(x1 => x1.Maturity).Select(x2 => x2.Maturity).ToArray();

            double[] values = null;
            if (cachedUpValues != null)
                values = cachedUpValues;
            else
                cachedUpValues = values = DefaultCutoffs.OrderBy(x1 => x1.Maturity).Select(x2 => x2.UpperCut).ToArray();
            
            double dcut = 1;

            int numberofdays = 1; 

            retval = (1 + (10 - Math.Log(numberofdays)) / 10 *
                (volLevel3M - TargetVolatility3MCutOffs) / (TargetVolatility3MCutOffs)) * dcut;

            return retval;
        }

        [ComVisible(false)]
        public IList<ExtrapolationSlice> ExtrapolationSlices { get; set; }

        [ComVisible(false)]
        public ExtrapolationSlices ExtrapolationSliceCollection
        {
            get
            {
                return new ExtrapolationSlices(ExtrapolationSlices);
            }
        }

        [ComVisible(false)]
        public void AddExtrapolationSlice(ExtrapolationSlice es)
        {
            es.Settings = this;
            ExtrapolationSlices.Add(es);
        }

        [ComVisible(false)]
        public IList<ForwardTolerance> ForwardTolerances { get; set; }

        public ForwardTolerances ForwardTolerancesCollection
        {
            get
            {
                return new ForwardTolerances(ForwardTolerances);
            }
        }

        public void AddTolerance(ForwardTolerance co)
        {
            co.SettingsSetRule = this;
            ForwardTolerances.Add(co);
        }

        [ComVisible(false)]
        public IList<CubicSplineParam> CubicSplineParams { get; set; }

        public CubicSplineParams CubicSplineParamsCollection
        {
            get
            {
                return new CubicSplineParams(CubicSplineParams);
            }
        }

        public void AddCubicSplineParam(CubicSplineParam param)
        {
            param.SettingsSetRule = this;
            CubicSplineParams.Add(param);
        }

        DateTime[] cubicSpline_cachedMats = null;
        double[] cachedPenalties = null;
        
        public double GetPenaltyNonLinearity(Underlying und, double volLevel3M, DateTime expiry)
        {
            DateTime[] mats = null;
            if (cubicSpline_cachedMats != null)
                mats = cubicSpline_cachedMats;
            else
                cubicSpline_cachedMats = mats = CubicSplineParams.OrderBy(x1 => x1.Maturity).Select(x2 => x2.Maturity).ToArray();

            double[] values = null;
            if (cachedPenalties != null)
                values = cachedPenalties;
            else
                cachedPenalties = values = CubicSplineParams.OrderBy(x1 => x1.Maturity).Select(x2 => x2.PenaltyNonLinearity).ToArray(); 

            return 1;
        }

        public string EnforceCutoffsStrikeRangeMaturity { get; set; }
        
        public DateTime EnforceCutoffsStrikeRangeMaturityAsDate
        {
            get
            {
                if (!String.IsNullOrEmpty(EnforceCutoffsStrikeRangeMaturity))
                {
                    try
                    {
                        try
                        {
                            return DateTime.ParseExact(EnforceCutoffsStrikeRangeMaturity, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            return new DateTime();
                        }
                    }
                    catch
                    {
                    }
                }
                return DateTime.MinValue;
            }
        }
    }

    public class FoSettingsSetRuleAudit : Audit, IEquatable<FoSettingsSetRuleAudit>
    {
        public enum CutOffTypeEnum
        {
            Automatic,
            UserDefined,
            DefaultAdjusted,
            Detection
        } ;

        [IgnoreCompare]
        public int TypeId { get; set; }

        [IgnoreCompare]
        public int ClassificationId { get; set; }

        public double ATMVolSmoothing { get; set; }
        public double MaxConvChangePerYear { get; set; }
        public double SkewSmoothing { get; set; }
        public CutOffTypeEnum UserCutOffs { get; set; }
        public double OrcCurvFloor { get; set; }
        public bool SkewFlatning { get; set; }
        public bool NoArbitrageConstraints { get; set; }
        public double TotalAbsDiffPcCc { get; set; }
        public double LbVc { get; set; }
        public double UbVc { get; set; }
        public double LbSc { get; set; }
        public double UbSc { get; set; }
        public double LbPc { get; set; }
        public double UbPc { get; set; }
        public double LbCc { get; set; }
        public double UbCc { get; set; }
        public double VolCapFactor { get; set; }
        public double VolCapSmoothRange { get; set; }
        public double MatBoundLtSt { get; set; }
        public double PutConvSmoothLt { get; set; }
        public double PutConvSmoothSt { get; set; }
        public double CallConvSmoothLt { get; set; }
        public double CallConvSmoothSt { get; set; }

        public double StrikeMin { get; set; }
        public double StrikeMax { get; set; }
        public double StrikeStep { get; set; }

        public double GridWingMin { get; set; }
        public double GridWingMax { get; set; }
        public double GridWingStep { get; set; }

        public double GridATMMin { get; set; }
        public double GridATMMax { get; set; }
        public double GridATMStep { get; set; }

        public bool CheckArbitrage { get; set; }

        public double PenaltySystemVolatilityLT { get; set; }
        public double PenaltySystemVolatilityST { get; set; }

        public double PenaltySystemVolatilitySTLTMaturity { get; set; }

        public double MinSpread { get; set; }
        public double MaxSpread { get; set; }

        public bool ATMChangeDecreasing { get; set; }
        public bool SkewChangeDecreasing { get; set; }
        public bool ConvexityChangeDecreasing { get; set; }

        public double StartNonpositiveSkew { get; set; }

        public double CallPutParityCheckThreshold { get; set; }
        public int NumberOfPublishingIterations { get; set; }

        public int SecondsBetweenPublishingIterations { get; set; }

        public bool UseStrippedReposForImpliedVolatilityalculationsAndPublishing { get; set; }
        public double StrippedForwardRelativeTolerance { get; set; }

        public double TargetVolatility3MCutOffs { get; set; }

        public double ATMBoost { get; set; }
        public string ATMBoostMaturity { get; set; }
        public double ATMBoostStrikeMin { get; set; }
        public double ATMBoostStrikeMax { get; set; }

        public bool UseShortMaturityArtificialSliceExtrapolation { get; set; }
        public bool MissingShortMaturitiesExtrapolation { get; set; }
        public string ShortMaturityReferenceTimeStamp { get; set; }

        #region Overrides of Audit

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Setting Set Rule"; }
        }

        #endregion

        public bool Equals(FoSettingsSetRuleAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.TypeId == TypeId && other.ClassificationId == ClassificationId && other.ATMVolSmoothing.Equals(ATMVolSmoothing) && other.MaxConvChangePerYear.Equals(MaxConvChangePerYear) && other.SkewSmoothing.Equals(SkewSmoothing) && Equals(other.UserCutOffs, UserCutOffs) && other.OrcCurvFloor.Equals(OrcCurvFloor) && other.SkewFlatning.Equals(SkewFlatning) && other.NoArbitrageConstraints.Equals(NoArbitrageConstraints) && other.TotalAbsDiffPcCc.Equals(TotalAbsDiffPcCc) && other.LbVc.Equals(LbVc) && other.UbVc.Equals(UbVc) && other.LbSc.Equals(LbSc) && other.UbSc.Equals(UbSc) && other.LbPc.Equals(LbPc) && other.UbPc.Equals(UbPc) && other.LbCc.Equals(LbCc) && other.UbCc.Equals(UbCc) && other.VolCapFactor.Equals(VolCapFactor) && other.VolCapSmoothRange.Equals(VolCapSmoothRange) && other.MatBoundLtSt.Equals(MatBoundLtSt) && other.PutConvSmoothLt.Equals(PutConvSmoothLt) && other.PutConvSmoothSt.Equals(PutConvSmoothSt) && other.CallConvSmoothLt.Equals(CallConvSmoothLt) && other.StrikeMin.Equals(StrikeMin) && other.CallConvSmoothSt.Equals(CallConvSmoothSt) && other.StrikeMax.Equals(StrikeMax) && other.StrikeStep.Equals(StrikeStep) && other.GridWingMin.Equals(GridWingMin) && other.GridWingMax.Equals(GridWingMax) && other.GridWingStep.Equals(GridWingStep) && other.GridATMMin.Equals(GridATMMin) && other.GridATMMax.Equals(GridATMMax) && other.GridATMStep.Equals(GridATMStep) && other.CheckArbitrage.Equals(CheckArbitrage) && other.PenaltySystemVolatilityLT.Equals(PenaltySystemVolatilityLT) && other.PenaltySystemVolatilityST.Equals(PenaltySystemVolatilityST) && other.PenaltySystemVolatilitySTLTMaturity.Equals(PenaltySystemVolatilitySTLTMaturity) && other.MinSpread.Equals(MinSpread) && other.MaxSpread.Equals(MaxSpread) && other.ATMChangeDecreasing.Equals(ATMChangeDecreasing) && other.SkewChangeDecreasing.Equals(SkewChangeDecreasing) && other.ConvexityChangeDecreasing.Equals(ConvexityChangeDecreasing) && other.StartNonpositiveSkew.Equals(StartNonpositiveSkew) && other.CallPutParityCheckThreshold.Equals(CallPutParityCheckThreshold) && other.NumberOfPublishingIterations == NumberOfPublishingIterations && other.SecondsBetweenPublishingIterations == SecondsBetweenPublishingIterations && other.StrippedForwardRelativeTolerance.Equals(StrippedForwardRelativeTolerance) && other.UseStrippedReposForImpliedVolatilityalculationsAndPublishing.Equals(UseStrippedReposForImpliedVolatilityalculationsAndPublishing) && other.TargetVolatility3MCutOffs.Equals(TargetVolatility3MCutOffs) && other.ATMBoost.Equals(ATMBoost) && Equals(other.ATMBoostMaturity, ATMBoostMaturity) && other.ATMBoostStrikeMin.Equals(ATMBoostStrikeMin) && other.ATMBoostStrikeMax.Equals(ATMBoostStrikeMax) && other.UseShortMaturityArtificialSliceExtrapolation.Equals(UseShortMaturityArtificialSliceExtrapolation) && Equals(other.ShortMaturityReferenceTimeStamp, ShortMaturityReferenceTimeStamp) && other.MissingShortMaturitiesExtrapolation.Equals(MissingShortMaturitiesExtrapolation);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FoSettingsSetRuleAudit)) return false;
            return Equals((FoSettingsSetRuleAudit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = TypeId;
                result = (result*397) ^ Id;
                result = (result*397) ^ JournalTimeStamp.GetHashCode();
                result = (result*397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result*397) ^ ClassificationId;
                result = (result*397) ^ ATMVolSmoothing.GetHashCode();
                result = (result*397) ^ MaxConvChangePerYear.GetHashCode();
                result = (result*397) ^ SkewSmoothing.GetHashCode();
                result = (result*397) ^ UserCutOffs.GetHashCode();
                result = (result*397) ^ OrcCurvFloor.GetHashCode();
                result = (result*397) ^ SkewFlatning.GetHashCode();
                result = (result*397) ^ NoArbitrageConstraints.GetHashCode();
                result = (result*397) ^ TotalAbsDiffPcCc.GetHashCode();
                result = (result*397) ^ LbVc.GetHashCode();
                result = (result*397) ^ UbVc.GetHashCode();
                result = (result*397) ^ LbSc.GetHashCode();
                result = (result*397) ^ UbSc.GetHashCode();
                result = (result*397) ^ LbPc.GetHashCode();
                result = (result*397) ^ UbPc.GetHashCode();
                result = (result*397) ^ LbCc.GetHashCode();
                result = (result*397) ^ UbCc.GetHashCode();
                result = (result*397) ^ VolCapFactor.GetHashCode();
                result = (result*397) ^ VolCapSmoothRange.GetHashCode();
                result = (result*397) ^ MatBoundLtSt.GetHashCode();
                result = (result*397) ^ PutConvSmoothLt.GetHashCode();
                result = (result*397) ^ PutConvSmoothSt.GetHashCode();
                result = (result*397) ^ CallConvSmoothLt.GetHashCode();
                result = (result*397) ^ StrikeMin.GetHashCode();
                result = (result*397) ^ CallConvSmoothSt.GetHashCode();
                result = (result*397) ^ StrikeMax.GetHashCode();
                result = (result*397) ^ StrikeStep.GetHashCode();
                result = (result*397) ^ GridWingMin.GetHashCode();
                result = (result*397) ^ GridWingMax.GetHashCode();
                result = (result*397) ^ GridWingStep.GetHashCode();
                result = (result*397) ^ GridATMMin.GetHashCode();
                result = (result*397) ^ GridATMMax.GetHashCode();
                result = (result*397) ^ GridATMStep.GetHashCode();
                result = (result*397) ^ CheckArbitrage.GetHashCode();
                result = (result*397) ^ PenaltySystemVolatilityLT.GetHashCode();
                result = (result*397) ^ PenaltySystemVolatilityST.GetHashCode();
                result = (result*397) ^ PenaltySystemVolatilitySTLTMaturity.GetHashCode();
                result = (result*397) ^ MinSpread.GetHashCode();
                result = (result*397) ^ MaxSpread.GetHashCode();
                result = (result*397) ^ ATMChangeDecreasing.GetHashCode();
                result = (result*397) ^ SkewChangeDecreasing.GetHashCode();
                result = (result*397) ^ ConvexityChangeDecreasing.GetHashCode();
                result = (result*397) ^ StartNonpositiveSkew.GetHashCode();
                result = (result*397) ^ CallPutParityCheckThreshold.GetHashCode();
                result = (result*397) ^ NumberOfPublishingIterations;
                result = (result*397) ^ SecondsBetweenPublishingIterations;
                result = (result*397) ^ StrippedForwardRelativeTolerance.GetHashCode();
                result = (result*397) ^ UseStrippedReposForImpliedVolatilityalculationsAndPublishing.GetHashCode();
                result = (result*397) ^ TargetVolatility3MCutOffs.GetHashCode();
                result = (result*397) ^ ATMBoost.GetHashCode();
                result = (result*397) ^ (ATMBoostMaturity != null ? ATMBoostMaturity.GetHashCode() : 0);
                result = (result*397) ^ ATMBoostStrikeMin.GetHashCode();
                result = (result*397) ^ ATMBoostStrikeMax.GetHashCode();
                result = (result*397) ^ UseShortMaturityArtificialSliceExtrapolation.GetHashCode();
                result = (result*397) ^ MissingShortMaturitiesExtrapolation.GetHashCode();
                result = (result*397) ^ (ShortMaturityReferenceTimeStamp != null ? ShortMaturityReferenceTimeStamp.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(FoSettingsSetRuleAudit left, FoSettingsSetRuleAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoSettingsSetRuleAudit left, FoSettingsSetRuleAudit right)
        {
            return !Equals(left, right);
        }
    }
}
