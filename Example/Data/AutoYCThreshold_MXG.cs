using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("6673746D-F7DD-408c-9752-DE21B5563657")]
    public class AutoYCThresholds_MXG : IEnumerable
    {
        IList<AutoYCThreshold_MXG> collection;
        public AutoYCThresholds_MXG(IList<AutoYCThreshold_MXG> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoYCThreshold_MXG item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoYCThreshold_MXG this[int index]
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

        public AutoYCThreshold_MXG ThresholdAt(DateTime Dt)
        {
            foreach (AutoYCThreshold_MXG threshold in collection.OrderBy(x => x.Maturity))
            {
                if (threshold.Maturity >= Dt)
                    return threshold;
            }

            return collection.Last();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("5BF571C9-952F-44d5-A3D2-11DCF8F261E2")]
    public class AutoYCThreshold_MXG : AutoYCThreshold
    {

    }
}
