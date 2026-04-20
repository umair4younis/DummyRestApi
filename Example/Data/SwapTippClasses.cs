using System;
using System.Collections.Generic;

namespace Puma.MDE.Data
{

    public class TippAllianzClassData
    {
        public DateTime mNAVdate = DateTime.MinValue;
        public double mNAV = 0.0;
        public double mUnits = 0.0;
        public double mUnitNAV = 0.0;
        public double mGuaranteedAmount = 0.0;
        public double mDailyPutPremiumAmount = 0.0;
        public double mAccruedPutPremiumAmount = 0.0;
        public double mDailyGuaranteeFeeAmount = 0.0;
        public double mAccruedGuaranteeFeeAmount = 0.0;
        public double mPutOptionIntrinsicValue = 0.0;
    }
    public class TippReportData
    {
        public TippReportData()
        {
            int size = Enum.GetNames(typeof(TypeTippAllianzFundClass)).Length; // 2
            mAllianzClassData = new TippAllianzClassData[size]; // do not use "List<TippAllianzClassData>" because allocates memory (capacity) but size remains 0
            mAllianzClassData[(int)TypeTippAllianzFundClass.ClassI] = new TippAllianzClassData();
            mAllianzClassData[(int)TypeTippAllianzFundClass.ClassS] = new TippAllianzClassData();
            mPutValuationDate = DateTime.MinValue;
        }

        // common data
        public DateTime mDateTime;
        public double mMaxRQ;
        public double mMinRQ;
        public double mTargetRQ;
        public double mRiskyWeight;
        public bool mActionRequired;
        public double mMultiplier;
        public double mCushion;
        public double mVolatility;
        public double mIndexValue;
        public double mIndexProtect;
        public double mBuffer;
        public double mFilter;
        public string mRemark;

        // Allianz specific
        public DateTime mPutValuationDate;
        public TippAllianzClassData[] mAllianzClassData;
        public double mHighRiskEquityExposure;
        public double mHighRiskBondExposure;
        public double mMarketValue;
        public double mVolatility2;
        public double mRealizedVolatility;
        public double mTriggerBuffer;
        public bool mTriggerEvent;
    }

    public class TippRawNAV
    {
        public DateTime mDateTime;
        public string mType;
        public double mNAV;
        public double mUnits;
    }

    public class TippAllianzNAV
    {
        public TippAllianzNAV(DateTime DateTime_I, DateTime DateTime_S, double NAV_I, double Units_I, double NAV_S, double Units_S)
        {
            int size = Enum.GetNames(typeof(TypeTippAllianzFundClass)).Length; // 2

            mDateTime = new List<DateTime>(size) { DateTime_I, DateTime_S };
            mNAV = new List<double>(size) { NAV_I, NAV_S };
            mUnits = new List<double>(size) { Units_I, Units_S };
        }
        // NAV date
        public List<DateTime> mDateTime;
        public DateTime getDateTime(TypeTippAllianzFundClass fund) { return mDateTime[(int)fund]; }
        public void setDateTime(TypeTippAllianzFundClass fund, DateTime value) { mDateTime[(int)fund] = value; }
        // NAV
        private List<double> mNAV;
        public double getNAV(TypeTippAllianzFundClass fund) { return mNAV[(int)fund]; }
        public void setNAV(TypeTippAllianzFundClass fund, double value) { mNAV[(int)fund] = value; }
        // Units
        private List<double> mUnits;
        public double getUnits(TypeTippAllianzFundClass fund) { return mUnits[(int)fund]; }
        public void setUnits(TypeTippAllianzFundClass fund, double value) { mUnits[(int)fund] = value; }
        // Unit NAV
        public double getUnitNAV(TypeTippAllianzFundClass fund) { double units = mUnits[(int)fund]; return (units != 0.0) ? (mNAV[(int)fund] / units) : (0.0); }
    }
}
