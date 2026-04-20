using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("8575BC3A-C35D-4118-A03F-671B1999D26A")]
    public class AiAlgorithmLinks : IEnumerable
    {
        IList<AiAlgorithmLink> collection;
        public AiAlgorithmLinks(IList<AiAlgorithmLink> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AiAlgorithmLink item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AiAlgorithmLink this[int index]
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
        public IList<AiAlgorithmLink> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("91FDA269-C2D9-4428-9868-389491836855")]
    [ComVisible(true)]
    public class AiAlgorithmLink
    {
        public int Id { get; set; }
        public int PddaMonitorId { get; set; }
        public int AlgorithmId { get; set; }
    }
}
