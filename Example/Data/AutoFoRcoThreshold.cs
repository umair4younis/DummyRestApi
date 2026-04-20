using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Globalization;

namespace Puma.MDE.Data
{
    public class AutoFoRcoThresholds : IEnumerable
    {
        IList<AutoFoRcoThreshold> collection;
        public AutoFoRcoThresholds(IList<AutoFoRcoThreshold> collection)
        {
            this.collection = collection;
        }
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoFoRcoThreshold item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoFoRcoThreshold this[int index]
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

        public AutoFoRcoThreshold ThresholdAt(DateTime Dt)
        {
            foreach (AutoFoRcoThreshold threshold in collection.OrderBy(x => x.Maturity))
            {
                if (threshold.Maturity >= Dt)
                    return threshold;
            }

            return collection.OrderBy(x => x.Maturity).Last();
        }
    }
      
    public class AutoFoRcoThreshold
    {
        public int Id { get; set; }
        public String Bucket { get; set; }
        public DateTime Maturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(Bucket, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }
        public double ThresholdAtm { get; set; }
        public double Threshold_80_120 { get; set; }
        public double Threshold_50_150 { get; set; }

        public VolMonitor Monitor { get; set; }

        public AutoFoRcoThreshold Clone()
        {
            AutoFoRcoThreshold retval = new AutoFoRcoThreshold();

            retval.Bucket = Bucket;
            retval.ThresholdAtm = ThresholdAtm;
            retval.Threshold_80_120 = Threshold_80_120;
            retval.Threshold_50_150 = Threshold_50_150;

            return retval;
        }

        public double GetThresholdForStrike(double strike)
        {
            if (strike == 0.5 || strike == 1.5)
            {
                return this.Threshold_50_150;
            }
            else if (strike == 0.8 || strike == 1.2)
            {
                return this.Threshold_80_120;
            }
            else if (strike == 1.0)
            {
                return this.ThresholdAtm;
            }
            return 0.0;
        }

    }
}
