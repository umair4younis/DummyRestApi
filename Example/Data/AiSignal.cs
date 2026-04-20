using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("943CAF04-E5F0-45d3-90FA-E5F68F755FF1")]
    public class AiSignals : IEnumerable
    {
        IList<AiSignal> collection;
        public AiSignals(IList<AiSignal> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(AiSignal item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public AiSignal this[int index]
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
        public IList<AiSignal> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("F5B2BB0C-BD8A-4661-A603-50346D89367F")]
    [ComVisible(true)]
    public class AiSignal
    {
        public int Id { get; set; }
        public double SignalValue { get; set; }
        public string SignalType { get; set; }
        public int AiAlgoId { get; set; }
        public int PddaMonitorId { get; set; }
        public int UnderlyingId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
