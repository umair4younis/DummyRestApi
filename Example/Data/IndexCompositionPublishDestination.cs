using System;
using System.Collections;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    public class IndexCompositionPublishDestinations : IEnumerable
    {
        IList<IndexCompositionPublishDestination> collection;
        public IndexCompositionPublishDestinations(IList<IndexCompositionPublishDestination> collection)
        {
            this.collection = collection;
        }
        
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(IndexCompositionPublishDestination item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public IndexCompositionPublishDestination this[int index]
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
    public class IndexCompositionPublishDestination
    {
        public long Id { get; set; }
        public Underlying Underlying { get; set; }
        public string Destination { get; set; }
    }
}
