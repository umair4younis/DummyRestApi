using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("CE61DE07-61A6-43f2-BCED-05867CF19CE3")]
    public class PublishingSchedules : IEnumerable
    {
        IList<PublishingSchedule> collection;
        public PublishingSchedules(IList<PublishingSchedule> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(PublishingSchedule item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public PublishingSchedule this[int index]
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
    [Guid("75EA72C2-8274-4fad-BC66-7973D6CA233F")]
    [ComVisible(true)]
    [Serializable]
    public class PublishingSchedule
    {
        public const string MilanTemplate = "Puma.MDE.MarketData.PublishingScheduleMilan, PumaMDE";

        public int Id { get; set; }
        public String Name { get; set; }
        public String Template { get; set; }
    }

    //public class FoPublishingSchedule : Audit
    //{
    //}

    //public class AuditPublishingSchedule : Audit
    //{
    //}

    //public class FoAuditPublishingSchedule : Audit
    //{
    //}
}
