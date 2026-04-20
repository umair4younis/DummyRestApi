using Puma.MDE.Common;
using Puma.MDE.Common.Utilities;
using System;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{

    public enum UsedPairsModeEnum
    {
        AtTheMoneyPairsOnly = 0,
        PairsInStrikeRange = 1
    }

    public enum DeleteDuplicatesModeEnum
    {
        None = 0,
        Weekly = 1,
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("4E8A9D3B-78D1-4a99-AE9D-723C234DBE13")]
    [ComVisible(true)]
    [Serializable]
    public class QuoteFilterRule
    {
        public QuoteFilterRule Clone()
        {
            return DataFactory.Clone<QuoteFilterRule>(this);
        }

        public QuoteFilterRule()
        {
            MinExpiryDays = 5;
            BidAskCheck = true;
            CorporateActionCheck = false;
            OutlinersTolerance = 0.5;
            SpreadToleranceST = 0.3;
            SpreadToleranceLT = 0.3;
            SpreadToleranceSTLT = "1m";
            MinStrike = 0.1;
            MaxStrike = 2;
            SpreadBreakLimit = 0.05;
            WingSpreadWidening = 60;
            ApplyWingSpreadWideningCheck = true;
            SystemVolatilityTolerance = 0.0;
            ReferenceTolerance = 0.01;
            ReferenceTimestampTolerance = new TimeSpan(0, 0, 0); // 30 min

            UseGeneralFiltersForForwardStripping = true;
            UseImpliedVolFiltersForForwardStripping = true;
            UsePriceFiltersForForwardStripping = true;

            PriceOutliersCheck = false;
            PutCallPairSpreadLimit = 0.3;
            PutCallPairSpreadCheck = false;
            FullSlicesCheckMinNumber = 2;
            RemoveNotKxThresholdNumber = 0;
            OwnSize = 0;
            OwnSizeEnabled = false;

            UsedPairsMode = UsedPairsModeEnum.AtTheMoneyPairsOnly;

            MinStrikePair = 0.8 ;
            MaxStrikePair = 1.2 ;
            MaxNumberOfPairs = 10;
            DeleteDuplicatesMode = DeleteDuplicatesModeEnum.None;

        }

        public int Id { get; set; }
        public int TypeId { get; set; }
        public int ClassificationId { get; set; }
        public int MinExpiryDays { get; set; }
        public Boolean BidAskCheck { get; set; }
        public Boolean CorporateActionCheck { get; set; }
        public double OutlinersTolerance { get; set; }
        public double SpreadToleranceST { get; set; }
        public double SpreadToleranceLT { get; set; }
        public string SpreadToleranceSTLT { get; set; }
        public double MinStrike { get; set; }
        public double MaxStrike { get; set; }
        public double SpreadBreakLimit { get; set; }
        public double ReferenceTolerance { get; set; }
        public bool ApplyFullSlicesCheck { get; set; }
        public int FullSlicesCheckMinNumber { get; set; }
        public int RemoveNotKxThresholdNumber { get; set; }
        public bool ApplyWingSpreadWideningCheck { get; set; }
        public double WingSpreadWidening { get; set; }
        public double WingSpreadWideningEpsilon { get; set; }
        public bool ApplyMarketSpreadWideningCheck { get; set; }
        public int NumberMarketQuotes { get; set; }
        public double SystemVolatilityTolerance { get; set; }
        public TimeSpan ReferenceTimestampTolerance { get; set; }

        public bool UseGeneralFiltersForForwardStripping { get; set; }
        public bool UseImpliedVolFiltersForForwardStripping { get; set; }
        public bool UsePriceFiltersForForwardStripping { get; set; }

        public bool PriceOutliersCheck { get; set; }
        public double PutCallPairSpreadLimit { get; set; }

        public bool PutCallPairSpreadCheck { get; set; }

        public bool OwnSizeEnabled {get;set;}
        public double OwnSize {get;set;}

        public bool ExtrapolateStrippedRepos { get; set; }

        public UsedPairsModeEnum UsedPairsMode { get; set; }

        public double MinStrikePair { get; set; }
        public double MaxStrikePair { get; set; }
        public double MaxNumberOfPairs { get; set; }
        public DeleteDuplicatesModeEnum DeleteDuplicatesMode { get; set; }

    }

    public class FoQuoteFilterRuleAudit : Audit, IEquatable<FoQuoteFilterRuleAudit>
    {
        [IgnoreCompare]
        public int TypeId { get; set; }
        [IgnoreCompare]
        public int ClassificationId { get; set; }

        public int MinExpiryDays { get; set; }
        public Boolean BidAskCheck { get; set; }
        public Boolean CorporateActionCheck { get; set; }
        public double OutlinersTolerance { get; set; }
        public double SpreadTolerance { get; set; }
        public double MinStrike { get; set; }
        public double MaxStrike { get; set; }
        public double SpreadBreakLimit { get; set; }
        public double ReferenceTolerance { get; set; }
        public bool ApplyFullSlicesCheck { get; set; }
        public int FullSlicesCheckMinNumber { get; set; }
        public bool ApplyWingSpreadWideningCheck { get; set; }
        public double WingSpreadWidening { get; set; }
        public double WingSpreadWideningEpsilon { get; set; }
        public bool ApplyMarketSpreadWideningCheck { get; set; }
        public int NumberMarketQuotes { get; set; }
        public double SystemVolatilityTolerance { get; set; }
        public TimeSpan ReferenceTimestampTolerance { get; set; }

        public bool UseGeneralFiltersForForwardStripping { get; set; }
        public bool UseImpliedVolFiltersForForwardStripping { get; set; }
        public bool UsePriceFiltersForForwardStripping { get; set; }

        public bool PriceOutliersCheck { get; set; }
        public double PutCallPairSpreadLimit { get; set; }

        public bool PutCallPairSpreadCheck { get; set; }

        public bool OwnSizeEnabled { get; set; }
        public double OwnSize { get; set; }

        public bool ExtrapolateStrippedRepos { get; set; }

        public UsedPairsModeEnum UsedPairsMode { get; set; }

        public double MinStrikePair { get; set; }
        public double MaxStrikePair { get; set; }
        public double MaxNumberOfPairs { get; set; }
        public DeleteDuplicatesModeEnum DeleteDuplicatesMode { get; set; }

        [IgnoreCompare]
        public override string EntityName
        {
            get { return "Quote Filter"; }
        }

        public bool Equals(FoQuoteFilterRuleAudit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.TypeId == TypeId && other.ClassificationId == ClassificationId && other.MinExpiryDays == MinExpiryDays && other.BidAskCheck.Equals(BidAskCheck) && other.CorporateActionCheck.Equals(CorporateActionCheck) && other.OutlinersTolerance.Equals(OutlinersTolerance) && other.SpreadTolerance.Equals(SpreadTolerance) && other.MinStrike.Equals(MinStrike) && other.MaxStrike.Equals(MaxStrike) && other.SpreadBreakLimit.Equals(SpreadBreakLimit) && other.ReferenceTolerance.Equals(ReferenceTolerance) && other.ApplyFullSlicesCheck.Equals(ApplyFullSlicesCheck) && other.FullSlicesCheckMinNumber == FullSlicesCheckMinNumber && other.ApplyWingSpreadWideningCheck.Equals(ApplyWingSpreadWideningCheck) && other.WingSpreadWidening.Equals(WingSpreadWidening) && other.WingSpreadWideningEpsilon.Equals(WingSpreadWideningEpsilon) && other.ApplyMarketSpreadWideningCheck.Equals(ApplyMarketSpreadWideningCheck) && other.NumberMarketQuotes == NumberMarketQuotes && other.SystemVolatilityTolerance.Equals(SystemVolatilityTolerance) && other.ReferenceTimestampTolerance.Equals(ReferenceTimestampTolerance) && other.UseGeneralFiltersForForwardStripping.Equals(UseGeneralFiltersForForwardStripping) && other.UseImpliedVolFiltersForForwardStripping.Equals(UseImpliedVolFiltersForForwardStripping) && other.UsePriceFiltersForForwardStripping.Equals(UsePriceFiltersForForwardStripping) && other.PriceOutliersCheck.Equals(PriceOutliersCheck) && other.PutCallPairSpreadLimit.Equals(PutCallPairSpreadLimit) && other.PutCallPairSpreadCheck.Equals(PutCallPairSpreadCheck) && other.OwnSizeEnabled.Equals(OwnSizeEnabled) && other.OwnSize.Equals(OwnSize) && other.ExtrapolateStrippedRepos.Equals(ExtrapolateStrippedRepos) && Equals(other.UsedPairsMode, UsedPairsMode) && other.MinStrikePair.Equals(MinStrikePair) && other.MaxStrikePair.Equals(MaxStrikePair) && other.MaxNumberOfPairs.Equals(MaxNumberOfPairs) && other.DeleteDuplicatesMode.Equals(DeleteDuplicatesMode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(FoQuoteFilterRuleAudit)) return false;
            return Equals((FoQuoteFilterRuleAudit)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = TypeId;
                result = (result * 397) ^ Id;
                result = (result * 397) ^ JournalTimeStamp.GetHashCode();
                result = (result * 397) ^ (JournalType != null ? JournalType.GetHashCode() : 0);
                result = (result * 397) ^ ClassificationId;
                result = (result * 397) ^ MinExpiryDays;
                result = (result * 397) ^ BidAskCheck.GetHashCode();
                result = (result * 397) ^ CorporateActionCheck.GetHashCode();
                result = (result * 397) ^ OutlinersTolerance.GetHashCode();
                result = (result * 397) ^ SpreadTolerance.GetHashCode();
                result = (result * 397) ^ MinStrike.GetHashCode();
                result = (result * 397) ^ MaxStrike.GetHashCode();
                result = (result * 397) ^ SpreadBreakLimit.GetHashCode();
                result = (result * 397) ^ ReferenceTolerance.GetHashCode();
                result = (result * 397) ^ ApplyFullSlicesCheck.GetHashCode();
                result = (result * 397) ^ FullSlicesCheckMinNumber;
                result = (result * 397) ^ ApplyWingSpreadWideningCheck.GetHashCode();
                result = (result * 397) ^ WingSpreadWidening.GetHashCode();
                result = (result * 397) ^ WingSpreadWideningEpsilon.GetHashCode();
                result = (result * 397) ^ ApplyMarketSpreadWideningCheck.GetHashCode();
                result = (result * 397) ^ NumberMarketQuotes;
                result = (result * 397) ^ SystemVolatilityTolerance.GetHashCode();
                result = (result * 397) ^ ReferenceTimestampTolerance.GetHashCode();
                result = (result * 397) ^ UseGeneralFiltersForForwardStripping.GetHashCode();
                result = (result * 397) ^ UseImpliedVolFiltersForForwardStripping.GetHashCode();
                result = (result * 397) ^ UsePriceFiltersForForwardStripping.GetHashCode();
                result = (result * 397) ^ PriceOutliersCheck.GetHashCode();
                result = (result * 397) ^ PutCallPairSpreadLimit.GetHashCode();
                result = (result * 397) ^ PutCallPairSpreadCheck.GetHashCode();
                result = (result * 397) ^ OwnSizeEnabled.GetHashCode();
                result = (result * 397) ^ OwnSize.GetHashCode();
                result = (result * 397) ^ ExtrapolateStrippedRepos.GetHashCode();
                result = (result * 397) ^ UsedPairsMode.GetHashCode();
                result = (result * 397) ^ MinStrikePair.GetHashCode();
                result = (result * 397) ^ MaxStrikePair.GetHashCode();
                result = (result * 397) ^ MaxNumberOfPairs.GetHashCode();
                result = (result * 397) ^ DeleteDuplicatesMode.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(FoQuoteFilterRuleAudit left, FoQuoteFilterRuleAudit right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FoQuoteFilterRuleAudit left, FoQuoteFilterRuleAudit right)
        {
            return !Equals(left, right);
        }
    }

}
