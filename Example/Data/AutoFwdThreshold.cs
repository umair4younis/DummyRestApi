using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("49BCF8BB-DB11-47a9-AF9D-287366348017")]
    public class AutoFwdThresholds : IEnumerable
    {
        IList<AutoFwdThreshold> collection;
        public AutoFwdThresholds(IList<AutoFwdThreshold> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoFwdThreshold item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoFwdThreshold this[int index]
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

        public AutoFwdThreshold ThresholdAt(DateTime Dt)
        {
            foreach (AutoFwdThreshold threshold in collection.OrderBy(x => x.Maturity))
            {
                if (threshold.Maturity >= Dt)
                    return threshold;
            }

            return collection.Last();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("2A8052DE-6DA6-4b84-9CF4-B1F564FE36AF")]
    public class AutoFwdThreshold
    {
        public AutoFwdThreshold()
        {
            ThresholdSophisOrcWarning = 0.05;
            ThresholdSophisStrippedWarning = 0.05;
            Bucket = "1m";
        }

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
        public double ThresholdSophisOrcWarning { get; set; }
        public double ThresholdSophisStrippedWarning { get; set; }
        public ForwardMonitor Monitor { get; set; }

        public AutoFwdThreshold Clone()
        {
            AutoFwdThreshold retval = new AutoFwdThreshold();

            retval.Bucket = Bucket;
            retval.ThresholdSophisOrcWarning = ThresholdSophisOrcWarning;
            retval.ThresholdSophisStrippedWarning = ThresholdSophisStrippedWarning;

            return retval;
        }
    }
}
