using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    public enum YieldCUrveKindEnum
    {
        Normal,
        OIS,
        FxFwd,
        CCS
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("E8A31DA9-B2A4-45d4-9420-0465636559C0")]
    [ComVisible(true)]

    public class YieldCurve
    {
        public int Id { get; set; }
        public int UnderlyingId { get; set; }
        public DateTime Timestamp { get; set; }
        public int FamilyCode { get; set; }
        public string FamilyName { get; set; }
        public int CurrencyCode { get; set; }
        public int YieldCurveCode { get; set; }
        public string YieldCurveName { get; set; }
        public string CurrencyName { get; set; }

        public YieldCUrveKindEnum CurveKind { get; set; }
        
        public YieldCurveValue GetYieldCurveValue(int Day)
        {
            return Values.First((value) => (value.Day == Day));
        }
        public YieldCurve()
        {
            Values = new List<YieldCurveValue>();
            Timestamp = DateTime.Now;
        }
        public void Add(YieldCurveValue p)
        {
            p.YieldCurve = this;
            Values.Add(p);
        }

        [ComVisible(false)]
        public IList<YieldCurveValue> Values { get; set; }
        
        public YieldCurveValues ValuesCollection 
        { 
            get
            {
                return new YieldCurveValues(Values.OrderBy( x => x.Day ).ToList());
            }
        }

        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
            }
        }
        
        static public YieldCurve CreateInstance(Underlying und)
        {
            YieldCurve instance = new YieldCurve();

            instance.Underlying = und;
            instance.Timestamp = DateTime.Now;

            return instance;

        }

        public YieldCurve Clone()
        {
            YieldCurve retval = CreateInstance(Underlying);
            foreach (YieldCurveValue val in Values)
            {
                retval.Add(val.Clone());
            }
            return retval;
        }

        public DateTime[] GetDaysAsMaturities()
        {
            List<DateTime> retval = new List<DateTime>();
            DateTime mat = DateTime.Today;
            foreach (YieldCurveValue point in Values)
            {
                DateTime newMat = mat.AddDays(point.Day);
                retval.Add(newMat);
            }
            return retval.ToArray();
        }

        public double[] GetRates()
        {
            List<double> retval = new List<double>();
            foreach (YieldCurveValue point in Values)
            {
                retval.Add(point.Rate);
            }
            return retval.ToArray();
        }
    }
}
