using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("176FD7D7-EDA9-488e-8BE2-144A6487272D")]
    public class AutoYCThresholds : IEnumerable
    {
        IList<AutoYCThreshold> collection;
        public AutoYCThresholds(IList<AutoYCThreshold> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoYCThreshold item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoYCThreshold this[int index]
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

        public AutoYCThreshold ThresholdAt(DateTime Dt)
        {
            foreach (AutoYCThreshold threshold in collection.OrderBy(x => x.Maturity))
            {
                if (threshold.Maturity >= Dt)
                    return threshold;
            }

            return collection.Last();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("D384001D-2B70-4284-809F-AC66F24C34B1")]
    public class AutoYCThreshold
    {
        public AutoYCThreshold()
        {
            ThresholdPublish = 0.01;
            ThresholdWarning = 0.015;
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
        public double ThresholdPublish { get; set; }
        public double ThresholdWarning { get; set; }
        public YieldCurveMonitor Monitor { get; set; }
    }
}
