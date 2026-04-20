using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("28D121F6-7176-4ed6-BD75-A4FEE497D969")]
    public class YieldCurveMonitorChilds : IEnumerable
    {
        IList<YieldCurveMonitorChild> collection;
        public YieldCurveMonitorChilds(IList<YieldCurveMonitorChild> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(YieldCurveMonitorChild item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public YieldCurveMonitorChild this[int index]
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
        public IList<YieldCurveMonitorChild> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("B2A691E6-733F-4d5c-A316-AFAA2EF29B37")]
    public class YieldCurveMonitorChild
    {
        public int Id { get; set; }
        public string YcName { get; set; }
        public YieldCurveMonitor Monitor { get; set; }
        public bool Alarmed { get; set; }
        public bool CheckThresholds { get; set; }

        public YieldCurveMonitorChild()
        {
            CheckThresholds = true;
        }

        string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value == null)
                    _status = null;
                else
                {
                    if (value.Length > 2048)
                        _status = value.Substring(0, 2048);
                    else
                        _status = value;
                }
            }
        }
    }
}
