using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("58058053-EA2E-463f-ABF6-9D7D66E9835E")]
    public class PublishDestinations : IEnumerable
    {
        IList<PublishDestination> collection;
        public PublishDestinations(IList<PublishDestination> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(PublishDestination item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public PublishDestination this[int index]
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

    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("F6111694-D323-44e2-A9EB-DB0B5C8933FD")]
    public class PublishDestination
    {
        public long Id { get; set; }
        public Underlying Underlying { get; set; }
        public string Destination {get; set;}
        public PublishingSchedule PublishingSchedule { get; set; }

        public object GetMaturityScheduleProvider()
        {
            if (PublishingSchedule == null)
                return Underlying.GetMaturityScheduleProvider();

            return Engine.CreateObject<object>(
                PublishingSchedule.Template);
        }
    }
}
