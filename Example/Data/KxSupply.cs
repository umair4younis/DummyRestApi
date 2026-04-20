using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class KxSuppliers : IEnumerable
    {
        IList<KxSupply> collection;
        public KxSuppliers(IList<KxSupply> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(KxSupply item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public KxSupply  this[int index]
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
        public IList<KxSupply> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ComVisible(false)]
    public class KxSupply
    {
        public int Id { get; set; }
        public string Workset { get; set; }
        public int UnderlyingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Boolean Enabled { get; set; }

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

        [ComVisible(false)]
        public TimeSpan StartOffset
        {
            get
            {
                return StartTime - StartTime.Date;
            }
        }
        [ComVisible(false)]
        public TimeSpan EndOffset
        {
            get
            {
                return EndTime - EndTime.Date;
            }
        }
    }
}
