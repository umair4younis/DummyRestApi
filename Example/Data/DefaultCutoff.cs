using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Globalization;


namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("A851E731-FEC8-4688-B9AE-E73D39FA4B98")]
    public class DefaultCutoffs : IEnumerable
    {
        protected IList<DefaultCutoff> collection;
        public DefaultCutoffs(IList<DefaultCutoff> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(DefaultCutoff item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public DefaultCutoff this[int index]
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
        public IList<DefaultCutoff> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("F96007E1-150B-416a-9EA2-CFAD00DA5BDD")]
    [ComVisible(true)]

    [Serializable]
    public class DefaultCutoff
    {
        public int Id { get; set; }
        public SettingsSetRule SettingsSetRule { get; set; }
        public string Bucket { get; set; }
        public double DownCut { get; set; }
        public double UpperCut { get; set; }

        public DefaultCutoff Clone()
        {
            //return DataFactory.Clone<DefaultCutoff>(this);
            return (DefaultCutoff)MemberwiseClone();
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
