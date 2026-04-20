using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("EE664A9C-7021-4c05-A46D-4556223B17D8")]
    public class AutoRepoThresholds : IEnumerable
    {
        IList<AutoRepoThreshold> collection;
        public AutoRepoThresholds(IList<AutoRepoThreshold> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoRepoThreshold item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoRepoThreshold this[int index]
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

        public AutoRepoThreshold ThresholdAt(DateTime Dt)
        {
            foreach (AutoRepoThreshold threshold in collection.OrderBy(x => x.Maturity))
            {
                if (threshold.Maturity >= Dt)
                    return threshold;
            }

            return collection.Last();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("98D485C9-CA8B-4e14-BBF7-CA5464897458")]
    public class AutoRepoThreshold
    {
        public AutoRepoThreshold()
        {
            ThresholdPublish = 0.01;
            ThresholdWarning = 0.01;
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

        public VolMonitor Monitor { get; set; }
    }
}
