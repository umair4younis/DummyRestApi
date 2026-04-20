using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("221554F5-C973-470f-9A92-F89DCEC77B34")]
    public class AutoRepoChanges : IEnumerable
    {
        IList<AutoRepoChange> collection;
        public AutoRepoChanges(IList<AutoRepoChange> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AutoRepoChange item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AutoRepoChange this[int index]
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
    [Guid("4348F63F-41A8-45bf-A623-6577B00C3270")]
    public class AutoRepoChange
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
        public double Change { get; set; }

        public VolMonitor Monitor { get; set; }
    }
}
