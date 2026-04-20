using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("1372C2B1-B931-4eaa-BCB2-991DD7CC3394")]
    public class BenchmarkLinks : IEnumerable
    {
        IList<BenchmarkLink> collection;
        public BenchmarkLinks(IList<BenchmarkLink> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(BenchmarkLink item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public BenchmarkLink this[int index]
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

        [ComVisible(false)]
        public IList<BenchmarkLink> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("F867A056-7061-44e5-AF90-0B0390727764")]
    [ComVisible(true)]

    public class BenchmarkLink
    {
        public int Id { get; set; }
        public int UnderlyingId { get; set; }
        public int BenchmarkId { get; set; }
    }
}
