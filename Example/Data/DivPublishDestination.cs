using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("93A933E9-C482-49f6-B0E5-C74A0B335EA6")]
    public class DivPublishDestinations : IEnumerable
    {
        IList<DivPublishDestination> collection;
        public DivPublishDestinations(IList<DivPublishDestination> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(DivPublishDestination item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public DivPublishDestination this[int index]
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
    [Guid("50F01926-CE3B-4890-A8DD-CC9A4285CE9E")]
    public class DivPublishDestination
    {
        public long Id { get; set; }
        public Underlying Underlying { get; set; }
        public string Destination {get; set;}
    }
}
