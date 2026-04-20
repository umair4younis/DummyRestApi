using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("7AC13D3F-50C7-433c-8F66-427484975C7B")]
    public class Calendars : IEnumerable
    {
        IList<Calendar> collection;
        public Calendars(IList<Calendar> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(Calendar item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public Calendar this[int index]
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
        public IList<Calendar> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("859C2ED4-8851-4948-813B-CF9E151B4BC7")]
    public class Calendar  // mapping between the various avaliable calendars
    {
        public int Id { get; set; }
        public string AssetControl { get; set; }
        public string Sophis { get; set; }
        public string HolidayCode { get; set; }
        public string ORCPuma { get; set; }
        [ComVisible(false)]
        public DateTime? LastUpdate { get; set; }
    }
}
