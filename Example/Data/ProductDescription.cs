using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("4D765F39-954B-4c6b-A558-CD9BF50DE629")]
    public class ProductDescriptions : IEnumerable
    {
        IList<ProductDescription> collection;
        public ProductDescriptions(IList<ProductDescription> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ProductDescription item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ProductDescription this[int index]
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


    public class ProductDescription
    {
        public int Id { get; set; }
        public string PumaProductDescription { get; set; }
    }
}
