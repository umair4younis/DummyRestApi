using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    // data from: select * from euwax_issuer
    public class EuwaxIssuer
    {
        public int    Id              { get; set; }
        public string IssuerName      { get; set; }
        public string Description     { get; set; }
        public string ReutersFeedCode { get; set; }
        public string IssuerId        { get; set; }
    }

    // data from: select * from puma_mde_und_euwax_issuer
    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("F5E09C07-F5E3-4377-81A6-977FC5A22715")]
    public class EuwaxIssuerItem
    {
        public long       Id          { get; set; }
        public Underlying Underlying  { get; set; }
        public string     IssuerName  { get; set; }
        public string     Description { get; set; }
    }

    // container for EuwaxIssuerItem(s)
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("12947754-173A-47d0-987F-232E1BE6B4A3")]
    public class EuwaxIssuerItems : IEnumerable
    {
        IList<EuwaxIssuerItem> collection;
        public EuwaxIssuerItems(IList<EuwaxIssuerItem> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(EuwaxIssuerItem item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public EuwaxIssuerItem this[int index]
        {
            get { return collection[index];  }
            set { collection[index] = value; }
        }
        public int Count
        {
            get { return collection.Count; }
        }
    }
}
