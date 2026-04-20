using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("A2863E4A-9626-4d0e-8C60-80F5785BB069")]
    public class ForwardMonitors : IEnumerable
    {
        IList<ForwardMonitor> collection;
        public ForwardMonitors(IList<ForwardMonitor> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(ForwardMonitor item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public ForwardMonitor this[int index]
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
        public IList<ForwardMonitor> Collection
        {
            get
            {
                return collection;
            }
        }
    }


    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("2C58859A-6FFC-448e-9188-8F4164CD6DB8")]
    [ComVisible(true)]
    public class ForwardMonitor
    {
        public ForwardMonitor()
        {
            Workset = "StandardForward";
            WarningTrigger = 0.01;
            CheckedAt = DateTime.MinValue;
            CheckHorizon = "10y";
            ForwardAlarm = false;

            Thresholds = new List<AutoFwdThreshold>() ;
        }

        public int Id { get; set; }
        public string Workset { get; set; }
        public int UnderlyingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Boolean Enabled { get; set; }
        public double WarningTrigger { get; set; }
        public DateTime CheckedAt { get; set; }

        public Underlying Underlying
        {
            get
            {
                return Engine.Instance.Factory.GetUnderlying(UnderlyingId);
            }
            set
            {
                UnderlyingId = value.Id;
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
        public IList<AutoFwdThreshold> Thresholds { get; set; }

        public AutoFwdThresholds ThresholdsCollection
        {
            get
            {
                return new AutoFwdThresholds(Thresholds);
            }
        }

        public void AddThreshold(AutoFwdThreshold threshold)
        {
            threshold.Monitor = this;
            Thresholds.Add(threshold);
        }

        public string Reference
        {
            get
            {
                if (UnderlyingId != 0)
                    return Underlying.Reference;

                return "";
            }
            set
            {
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

        public bool ForwardAlarm
        {
            get;
            set;
        }

        public string Status
        {
            get;
            set;
        }

        public String CheckHorizon
        {
            get;
            set;
        }
        public DateTime CheckMaturity
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(CheckHorizon, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return new DateTime();
                }
            }
            set
            {
                CheckHorizon = value.ToString("yyyy-MM-dd");
            }
        }

        public ForwardMonitor Clone()
        {
            ForwardMonitor retval = new ForwardMonitor();

            retval.Workset = Workset;
            retval.StartTime = StartTime;
            retval.StartOffset = StartOffset;
            retval.EndTime = EndTime;
            retval.CheckHorizon = CheckHorizon;
            retval.Enabled = false;
            retval.Status = "";

            foreach (AutoFwdThreshold threshold in Thresholds)
            {
                retval.AddThreshold(threshold.Clone());
            }

            return retval;
        }
    }
}
