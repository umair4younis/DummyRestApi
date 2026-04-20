using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("2731F5D7-77FC-4193-AD99-11BD8F3AD15E")]
    public class YieldCurveMonitors : IEnumerable
    {
        IList<YieldCurveMonitor> collection;
        public YieldCurveMonitors(IList<YieldCurveMonitor> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(YieldCurveMonitor item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public YieldCurveMonitor this[int index]
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
        public IList<YieldCurveMonitor> Collection
        {
            get
            {
                return collection;
            }
        }
    }


    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("B428C43A-8C8A-45cf-85FC-C1573FB0F89B")]
    [ComVisible(true)]
    public class YieldCurveMonitor
    {
        public YieldCurveMonitor()
        {
            Workset = "YieldCurveStandard";
            Difference = 0;
            AutomaticalTrigger = 0.01;
            WarningTrigger = 0.02;
            PublishedAt = DateTime.MinValue;
            CheckedAt = DateTime.MinValue;

            YieldCurveAlarm = false;

            Changes         = new List<AutoYCChange>();
            Thresholds      = new List<AutoYCThreshold>();
            Thresholds_MXG  = new List<AutoYCThreshold_MXG>();  //VDP:45506
            Childs          = new List<YieldCurveMonitorChild>();
            NotificationSettings = new List<YieldCurveMonitorNotification>();
        }

        public int Id { get; set; }
        public string Workset { get; set; }
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
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PublishInterval { get; set; }
        public Boolean Enabled { get; set; }
        public double AutomaticalTrigger { get; set; }
        public double WarningTrigger { get; set; }
        public double Difference { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime CheckedAt { get; set; }

        public double LtStMaturityThreshold { get; set; }
        public double WarningTrigger_ST { get; set; }

        public bool OrcRelevantCurveBroken { get; set; }

        Underlying _underlying = null;
        public Underlying Underlying
        {
            get
            {
                if (_underlying != null)
                    return _underlying;

                using (var session = Engine.Instance.getFactory().OpenStatelessSession())
                    _underlying = session.Get<Underlying>(UnderlyingId);

                return _underlying;
            }
            set
            {
                UnderlyingId = value.Id;
                _underlying = null;
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
        public IList<AutoYCThreshold> Thresholds { get; set; }

        [ComVisible(false)]
        public IList<AutoYCThreshold_MXG> Thresholds_MXG { get; set; }  //VDP:45506

        public AutoYCThresholds ThresholdsCollection
        {
            get
            {
                return new AutoYCThresholds(Thresholds);
            }
        }


        public AutoYCThresholds_MXG ThresholdsCollection_MXG  //VDP:45506
        {
            get
            {
                return new AutoYCThresholds_MXG(Thresholds_MXG);
            }
        }

        [ComVisible(false)]
        public IList<YieldCurveMonitorChild> Childs { get; set; }

        public YieldCurveMonitorChilds CildsCollection
        {
            get
            {
                return new YieldCurveMonitorChilds(Childs);
            }
        }

        public void AddChild(YieldCurveMonitorChild child)
        {
            child.Monitor = this;
            Childs.Add(child);
        }

        [ComVisible(false)]
        public IList<YieldCurveMonitorNotification> NotificationSettings { get; set; }

        public YieldCurveMonitorNotification GetNotificationSetting(YieldCurve curve)
        {
            return NotificationSettings.Where(x => x.YcName.Equals(curve.YieldCurveName)).FirstOrDefault();
        }

        public YieldCurveMonitorNotifications NotificationSettingsCollection
        {
            get
            {
                return new YieldCurveMonitorNotifications(NotificationSettings);
            }
        }

        public void AddNotificationSetting(YieldCurveMonitorNotification notification)
        {
            notification.Monitor = this;
            NotificationSettings.Add(notification);
        }

        [ComVisible(false)]
        public IList<AutoYCChange> Changes { get; set; }

        public void AddChange(AutoYCChange change)
        {
            change.Monitor = this;
            Changes.Add(change);
        }

        public void AddThreshold(AutoYCThreshold threshold)
        {
            threshold.Monitor = this;
            Thresholds.Add(threshold);
        }

        public void AddThreshold_MXG(AutoYCThreshold_MXG threshold_MXG) //VDP:45506
        {
            threshold_MXG.Monitor = this;
            Thresholds_MXG.Add(threshold_MXG);
        }

        public AutoYCChanges ChangesCollection
        {
            get
            {
                return new AutoYCChanges(Changes);
            }
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

        public bool YieldCurveAlarm
        {
            get;
            set;
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

        public bool CheckThresholds
        {
            get;
            set;
        }
    }
}
