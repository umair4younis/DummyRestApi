using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("D0F91C2C-D511-4B55-9F66-6557A81E73FF")]
    public class CubicSplineParams : IEnumerable
    {
        protected IList<CubicSplineParam> collection;
        public CubicSplineParams(IList<CubicSplineParam> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(CubicSplineParam item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public CubicSplineParam this[int index]
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
        public IList<CubicSplineParam> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("F08022B4-9B45-44B7-A7F4-522CEE72F636")]
    [ComVisible(true)]

    [Serializable]
    public class CubicSplineParam
    {
        public int Id { get; set; }
        public SettingsSetRule SettingsSetRule { get; set; }
        public string Bucket { get; set; }

        public double PenaltyNonLinearity { get; set; }

        public CubicSplineParam Clone()
        {
            return (CubicSplineParam)MemberwiseClone();
        }

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
 
    }
}
