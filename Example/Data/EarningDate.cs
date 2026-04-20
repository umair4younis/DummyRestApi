using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("68DDE8E2-4654-496f-A4DE-F190E90584C6")]
    public class EarningDates : IEnumerable
    {
        IList<EarningDate> collection;
        public EarningDates(IList<EarningDate> collection)
        {
            this.collection = collection;
        }

        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(EarningDate item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public EarningDate this[int index]
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
        public IList<EarningDate> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("EAB1CD09-7A55-4cb7-A7E9-95409E8BD1A5")]
    [Serializable]
    public class EarningDate
    {
        public long Id { get; set; }
        public Underlying Underlying { get; set; }
        public DateTime EarningDt { get; set; }
        [ComVisible(false)]
        public DateTime? ExDate { get; set; }
        [ComVisible(false)]
        public decimal? Amount { get; set; }
    }
}
