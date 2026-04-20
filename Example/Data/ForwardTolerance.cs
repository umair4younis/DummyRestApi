using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Globalization;


namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("8ED7A132-5CE2-4899-AE16-931840B6CF73")]
    public class ForwardTolerances : IEnumerable
    {
        IList<ForwardTolerance> collection;
        public ForwardTolerances(IList<ForwardTolerance> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ForwardTolerance item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ForwardTolerance this[int index]
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
        public IList<ForwardTolerance> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("A6565E78-1323-490a-970B-A7BDF818998C")]
    [ComVisible(true)]

    [Serializable]
    public class ForwardTolerance
    {
        public int Id { get; set; }
        public SettingsSetRule SettingsSetRule { get; set; }
        public string Bucket { get; set; }
        public double Tolerance { get; set; }

        public ForwardTolerance Clone()
        {
            //return DataFactory.Clone<ForwardTolerance>(this);
            return (ForwardTolerance)MemberwiseClone();
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
