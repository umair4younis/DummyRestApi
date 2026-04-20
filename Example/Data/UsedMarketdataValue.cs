using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("BE9F77E2-72D7-40fb-8483-F6CBC86FBCCF")]
    public class UsedMarketdataValues : IEnumerable
    {
        IList<UsedMarketdataValue> collection;
        public UsedMarketdataValues(IList<UsedMarketdataValue> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(UsedMarketdataValue item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public UsedMarketdataValue this[int index]
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
    [Guid("2296912F-A0B6-4aef-8E9C-91D3E6E69351")]
    [ComVisible(true)]

    public class UsedMarketdataValue
    {
        public long Id { get; set; }
        public String Name { get; set; }
        public double Value { get; set; }
        public String ValueAsString { get; set; }

        public UsedMarketdata Data { get; set; }

        public UsedMarketdataValue Clone()
        {
            return new UsedMarketdataValue() 
            { 
                Name = Name, 
                Value = Value, 
                ValueAsString = ValueAsString 
            };
        }
    }

}