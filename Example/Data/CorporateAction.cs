using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("8BA3592E-DE8A-4db2-A600-00744A8BAA2A")]
    public class CorporateActions : IEnumerable
    
    {
        IList<CorporateAction> collection;
        public CorporateActions(IList<CorporateAction> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(CorporateAction item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public CorporateAction this[int index]
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
        public IList<CorporateAction> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    public class CorporateAction
    {
        public double rFactor { get; set; }
        public DateTime exDate { get; set; }
        public int underlyingSophisId { get; set; }
        public int corprateActionId { get; set; }
        public Underlying underlying { get; set; }
    }
}
