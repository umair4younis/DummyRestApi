using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("9489C033-18F1-4cca-BDA4-4BB73790977E")]
    public class AutoATMChanges : IEnumerable
    {
        IList<AutoATMChange> collection;
        public AutoATMChanges(IList<AutoATMChange> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoATMChange item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoATMChange this[int index]
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
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("DC8F898D-98A6-43aa-952A-A8784E0DD3E4")]
    public class AutoATMChange
    {
        public long Id { get; set; }
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
                    return Engine.Instance.Today;
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }
        public double Change { get; set; }

        public VolMonitor Monitor { get; set; }
    }
}
