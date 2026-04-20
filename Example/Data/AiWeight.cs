using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("636AD3BA-B26F-4a73-9858-0A96B864F55E")]
    public class AiWeights : IEnumerable
    {
        IList<AiWeight> collection;
        public AiWeights(IList<AiWeight> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AiWeight item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AiWeight this[int index]
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
        public IList<AiWeight> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("65D58DEC-4957-4e1f-B499-8A8FA6E3CF44")]
    [ComVisible(true)]
    public class AiWeight
    {
        public int Id { get; set; }
        public int GenerationId { get; set; }
        public int Layer { get; set; }
        public int InputNode { get; set; }
        public int OutputNode { get; set; }
        public double Weight { get; set; }
    }
}
