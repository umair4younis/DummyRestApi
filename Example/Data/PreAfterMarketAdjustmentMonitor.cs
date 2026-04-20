using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("ED78B033-6146-49ef-8A1C-3BC0A817D61B")]
    public class PreAfterMarketAdjustmentMonitors : IEnumerable
    {
        IList<PreAfterMarketAdjustmentMonitor> collection;
        public PreAfterMarketAdjustmentMonitors(IList<PreAfterMarketAdjustmentMonitor> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(PreAfterMarketAdjustmentMonitor item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public PreAfterMarketAdjustmentMonitor this[int index]
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
        public IList<PreAfterMarketAdjustmentMonitor> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("4BD7D19E-3596-49f1-BD74-679099F48779")]
    [ComVisible(true)]

    public class PreAfterMarketAdjustmentMonitor
    {
        public PreAfterMarketAdjustmentMonitor()
        {
            Workset = "Standard";

            PublishedAt = DateTime.MinValue;
            CheckedAt = DateTime.MinValue;

            Alarm = false;
        }

        public void CopyFrom(PreAfterMarketAdjustmentMonitor mon)
        {

            PublishedAt = mon.PublishedAt;
            CheckedAt = mon.CheckedAt;

            Alarm = mon.Alarm;
        }

        public int Id { get; set; }
        public string Workset { get; set; }

        public Boolean Enabled { get; set; }

        int underlyingid = 0;
        public int UnderlyingId 
        {
            get
            {
                return underlyingid;
            }
            set
            {
                underlyingid = value;
                _underlying = null;
            }
        }

        public int BenchmarkId { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public DateTime ReferenceTime { get; set; }
        public int ReferenceDateOffset { get; set; }


        public DateTime PublishedAt { get; set; }
        public DateTime CheckedAt { get; set; }

        public bool Alarm { get; set; }

        Underlying _underlying = null;
        public Underlying Underlying
        {
            get
            {
                if (_underlying != null)
                    return _underlying;

                _underlying = Engine.Instance.Factory.GetUnderlying(UnderlyingId);
                return _underlying;
            }
            set
            {
                UnderlyingId = value.Id;
                _underlying = null;
            }
        }

        public Underlying Benchmark
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(BenchmarkId);
            }
            set
            {
                BenchmarkId = value.Id;
            }
        }

        [ComVisible(false)]
        public TimeSpan StartOffset
        {
            get
            {
                return StartTime - StartTime.Date;
            }
            set
            {
                StartTime = DateTime.MinValue.Date + value;
            }
        }

        [ComVisible(false)]
        public TimeSpan EndOffset
        {
            get
            {
                return EndTime - EndTime.Date;
            }
            set
            {
                EndTime = DateTime.MinValue.Date + value;
            }
        }

        [ComVisible(false)]
        public TimeSpan ReferenceOffset
        {
            get
            {
                return ReferenceTime - ReferenceTime.Date;
            }
            set
            {
                ReferenceTime = DateTime.MinValue.Date + value;
            }
        }

        string _UnderlyingReference = string.Empty;
        public string UnderlyingReference
        {
            get
            {
                if (UnderlyingId != 0)
                    return Underlying.Reference;
#if SOPHIS_7
                return _UnderlyingReference;
#else
                return "";
#endif
            }
            set
            {
                _UnderlyingReference = value;
                try
                {
                    Underlying = Engine.Instance.Factory.GetUnderlying(value);
                }
                catch (Exception)
                {
                    UnderlyingId = 0;
                }
            }
        }
 
        public string BenchmarkReference
        {
            get
            {
                if (BenchmarkId != 0)
                    return Benchmark.Reference;

                return "";
            }
            set
            {
                try
                {
                    Benchmark = Engine.Instance.Factory.GetUnderlying(value);
                }
                catch (Exception)
                {
                    BenchmarkId = 0;
                }
            }
        }

        public DateTime ReferenceDateTime
        {
            get
            {
                DateTime retval = new DateTime();
                return retval + ReferenceOffset;
            }
        }
    
        public string Status
        {
            get;
            set;
        }
    
    }
}