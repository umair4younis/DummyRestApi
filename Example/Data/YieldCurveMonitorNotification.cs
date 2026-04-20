using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;
using System.Xml.Serialization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("e71e7093-63a9-461a-a0eb-d41f12bb9ebd")]
    public class YieldCurveMonitorNotifications : IEnumerable
    {
        IList<YieldCurveMonitorNotification> collection;
        public YieldCurveMonitorNotifications(IList<YieldCurveMonitorNotification> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(YieldCurveMonitorNotification item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public YieldCurveMonitorNotification this[int index]
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
        public IList<YieldCurveMonitorNotification> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("62ad2982-3e0b-47a3-9702-18c71298b2e6")]
    public class YieldCurveMonitorNotification
    {
        public int Id { get; set; }
        // Yield Curve Name/Id
        public string YcName { get; set; }

        [XmlIgnore]
        public YieldCurveMonitor Monitor { get; set; }
       
        /// Reporting Threshold
        public decimal Threshold { get; set; }

        /// Smallest checked maturity
        public decimal Maturity { get; set; }
    }
}
