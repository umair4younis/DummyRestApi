using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountPremium : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public DateTime? xDate { get; set; }
        public double? Rate { get; set; }
        public double? Basis { get; set; }
        public int? xNotionalCalc { get; set; }
        public bool IsNotionalCalcNull() { return !xNotionalCalc.HasValue; }
        public int NotionalCalc
        {
            get
            {
                return IsNotionalCalcNull() ? 1 : xNotionalCalc.Value;
            }
            set
            {
                xNotionalCalc = value;
            }
        }
        public TypeAccountPremiumNotionalCalc NotionalCalcEnum
        {
            get => (TypeAccountPremiumNotionalCalc)NotionalCalc;
            set
            {
                NotionalCalc = (int)value;
            }
        }

        public int? xDayCalc { get; set; }
        public bool IsDayCalcNull() { return !xDayCalc.HasValue; }
        public int DayCalc
        {
            get
            {
                return IsDayCalcNull() ? 1 : xDayCalc.Value;
            }
            set
            {
                xDayCalc = value;
            }
        }
        public TypeAccountPremiumDayCalc DayCalcEnum
        {
            get => (TypeAccountPremiumDayCalc)DayCalc;
            set
            {
                DayCalc = (int)value;
            }
        }
    }
}
