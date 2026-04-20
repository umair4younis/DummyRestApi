using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountHistory : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public String Type { get; set; }
        public DateTime xDate { get; set; }
        public String Description { get; set; }
        public double IndexFactor { get; set; }
        public double IndexValue { get; set; }
        public double IndexSnapshotValue { get; set; }
        public DateTime IndexSnapShotSaved { get; set; }
        public double HighWaterMark { get; set; }
        public double? CollateralRatio { get; set; }
        public bool IsCollateralRatioNull()
        {
            return !CollateralRatio.HasValue;
        }
        public void SetCollateralRatioNull() { CollateralRatio = null; }
        public double? Ratio { get; set; }
        public bool IsRatioNull()
        {
            return !Ratio.HasValue;
        }
        public void SetRatioNull() { Ratio = null; }

        public int? RowVersion { get; set; }

        public double xCertificates { get { return 1.0 / IndexFactor; } }
        //public double xInvestedValue { get { return xCertificates * AccountRow.InitialIndexValue; } }
        public double xCollateralRatio { get { return (!IsCollateralRatioNull()) ? CollateralRatio.Value : 0.0; } }

    }
}
