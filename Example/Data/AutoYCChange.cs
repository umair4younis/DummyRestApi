using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("E1CA09BF-3F8B-4985-A7D8-39F5B4A38016")]
    public class AutoYCChanges : IEnumerable
    {
        IList<AutoYCChange> collection;
        public AutoYCChanges(IList<AutoYCChange> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoYCChange item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoYCChange this[int index]
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
    [Guid("E6CA726C-2143-4917-9BF4-15D739332E3D")]
    public class AutoYCChange
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
                    return new DateTime();
                }
            }
            set
            {
                Bucket = value.ToString("yyyy-MM-dd");
            }
        }
        public double Change { get; set; }

        public YieldCurveMonitor Monitor { get; set; }
    }
}
