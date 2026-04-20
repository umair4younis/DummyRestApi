using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("26892FBF-976D-4b13-AE52-AA2EC36366B5")]
    public class RepoPublishDestinations : IEnumerable
    {
        IList<RepoPublishDestination> collection;
        public RepoPublishDestinations(IList<RepoPublishDestination> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(RepoPublishDestination item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public RepoPublishDestination this[int index]
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
    [Guid("32F5DF22-F755-441a-A194-4E849E25A381")]
    public class RepoPublishDestination
    {
        public long Id { get; set; }
        public Underlying Underlying { get; set; }
        public string Destination {get; set;}
    }
}
