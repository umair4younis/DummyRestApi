using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("20B0D655-5897-4b22-B200-70B6B4CE74D4")]
    public class AiSubscribers : IEnumerable
    {
        IList<AiSubscriber> collection;
        public AiSubscribers(IList<AiSubscriber> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AiSubscriber item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AiSubscriber this[int index]
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
        public IList<AiSubscriber> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("B8D3AC89-A40E-4d26-8C7E-705AC8530379")]
    [ComVisible(true)]
    public class AiSubscriber
    {
        public int      Id        { get; set; }
        public string   UserName  { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo   { get; set; }
    }
}