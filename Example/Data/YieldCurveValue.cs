using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("95590635-BD92-4789-BAD4-C735AB959AE0")]
    public class YieldCurveValues : IEnumerable
    {
        IList<YieldCurveValue> collection;
        public YieldCurveValues(IList<YieldCurveValue> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(YieldCurveValue item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public YieldCurveValue this[int index]
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
    [Guid("54FBD5C8-60A0-449c-ADEF-EE924201DF52")]
    [ComVisible(true)]

    public class YieldCurveValue
    {
        public long Id { get; set; }
        public int Day { get; set; }
        public double Rate { get; set; }
        public String Type { get; set; }
        public String Convention { get; set; }
        public double DiscountFactor { get; set; }

        [XmlIgnore]
        public YieldCurve YieldCurve { get; set; }
        public YieldCurveValue Clone()
        {
            YieldCurveValue retval = new YieldCurveValue();

            retval.Day = Day;
            retval.Rate = Rate;
            retval.Type = Type;
            retval.Convention = Convention;
            retval.DiscountFactor = DiscountFactor;

            return retval;
        }
    }
}
