using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("402C955C-3987-408a-942C-CF75A9D8AE63")]
    public class VolMonitors : IEnumerable
    {
        IList<VolMonitor> collection;
        public VolMonitors(IList<VolMonitor> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(VolMonitor item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public VolMonitor this[int index]
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
        public IList<VolMonitor> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("ADD205B6-CF1E-4293-BD4C-36224A344FB6")]
    [ComVisible(true)]
    public class VolMonitor
    {
        public VolMonitor()
        {
            Workset = "Standard";
            Difference = 0;
            AutomaticalTrigger = 0.01;
            WarningTrigger = 0.02;
            PublishedAt = DateTime.MinValue;
            CheckedAt = DateTime.MinValue;

            CompleteRepublish = true;
            VolatilityAlarm = false;
            RepoAlarm = false;

            AutoRepoAdjustment = false;

            Changes = new List<AutoATMChange>();
            Thresholds = new List<AutoATMThreshold>() ;

            //RcoEODIssues = new List<RcoEODIssue>(); //VDP:DEBUG_#30626

            RepoChanges = new List<AutoRepoChange>();
            RepoThresholds = new List<AutoRepoThreshold>();

            FoRcoThresholds = new List<AutoFoRcoThreshold>();

            CheckThresholds = true;
            StandardStatus = DataFactory.WorksetStatusEnum.Regular;
            RerefEnabled = false;
            _status = "";
            CheckFoRcoThreshold = false;
            CheckPrevDayThreshold = true;

            LongTermExtrapolationEnabled = true;
            MaxDaysBetweenSophisUpdates = 1;

            UpdateTimeThreshold = 0;
        }

        public void CopyFrom(VolMonitor mon)
        {
            //Workset = mon.Workset;
            Difference = mon.Difference;
            
            //AutomaticalTrigger = mon.AutomaticalTrigger;
            //WarningTrigger = mon.WarningTrigger;
            
            PublishedAt = mon.PublishedAt;
            CheckedAt = mon.CheckedAt;

            //CompleteRepublish = mon.CompleteRepublish;
            VolatilityAlarm = mon.VolatilityAlarm;
            RepoAlarm = mon.RepoAlarm;

            //AutoRepoAdjustment = mon.AutoRepoAdjustment;

            Changes.Clear();
            foreach (AutoATMChange item in mon.Changes)
                AddChange(item);

            RepoChanges.Clear();
            foreach (AutoRepoChange item in mon.RepoChanges)
                AddRepoChange(item);

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
        public Boolean Enabled { get; set; }
        public double AutomaticalTrigger { get; set; }
        public double WarningTrigger { get; set; }
        public double Difference { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime CheckedAt { get; set; }
        public int MaxNumberOfSophisUpdates { get; set; }
        public int MinMinutesBetweenSophisUpdates { get; set; }
        public int MaxDaysBetweenSophisUpdates { get; set; }
        public Boolean CheckFoRcoThreshold { get; set; }
        public double MaxNumberOutliers { get; set; }
        public double VegaThreshold { get; set; }

        Underlying _underlying = null;
        public Underlying Underlying
        {
            get
            {
                if (_underlying != null)
                    return _underlying;
                
                using (var session = Engine.Instance.getFactory().OpenStatelessSession()) 
                    _underlying= session.Get<Underlying>(UnderlyingId);

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
    public IList<AutoATMThreshold> Thresholds { get; set; }

        public AutoATMThresholds ThresholdsCollection
        {
            get
            {
                return new AutoATMThresholds(Thresholds);
            }
        }

        [ComVisible(false)]
        public IList<AutoATMChange> Changes { get; set; }

        public void AddChange(AutoATMChange change)
        {
            change.Monitor = this;
            Changes.Add(change);
        }

        public void AddThreshold(AutoATMThreshold threshold)
        {
            threshold.Monitor = this;
            Thresholds.Add(threshold);
        }

        public AutoATMChanges ChangesCollection
        {
            get
            {
                return new AutoATMChanges(Changes);
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

        public bool CompleteRepublish
        {
            get;
            set;
        }

        public bool VolatilityAlarm
        {
            get;
            set;
        }

        public bool RepoAlarm
        {
            get;
            set;
        }

        public bool AutoRepoAdjustment
        {
            get;
            set;
        }

        [ComVisible(false)]
        public IList<AutoRepoThreshold> RepoThresholds { get; set; }

        public AutoRepoThresholds RepoThresholdsCollection
        {
            get
            {
                return new AutoRepoThresholds(RepoThresholds);
            }
        }

        [ComVisible(false)]
        public IList<AutoRepoChange> RepoChanges { get; set; }

        public void AddRepoChange(AutoRepoChange change)
        {
            change.Monitor = this;
            RepoChanges.Add(change);
        }

        public void AddRepoThreshold(AutoRepoThreshold threshold)
        {
            threshold.Monitor = this;
            RepoThresholds.Add(threshold);
        }

        public AutoRepoChanges RepoChangesCollection
        {
            get
            {
                return new AutoRepoChanges(RepoChanges);
            }
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
                    if (value.Length > 256)
                        _status = value.Substring(0, 256);
                    else
                        _status = value;
                }
            }
        }

        public bool RunOnce
        {
            get;
            set;
        }


        public bool BelongsEODGroup
        {
            get;
            set;
        }

        public bool CheckVolatilityMarketAfterProducing
        {
            get;
            set;
        }

        public int PublishingIteration
        {
            get;
            set;
        }
    
        public bool DelayedPublishing
        {
            get
            {
                return PublishingIteration>0 ;
            }
        }

        public DataFactory.WorksetStatusEnum StandardStatus
        {
            get;
            set;
        }

        public bool CheckThresholds
        {
            get;
            set;
        }

        public DateTime CurrentStarting
        {
            get
            {
                return Engine.Instance.Today + StartOffset;
            }
        }
        public DateTime CurrentEnding
        {
            get
            {
                return Engine.Instance.Today + EndOffset;
            }
        }

        public string SophisPublishMaxTime
        {
            get;
            set;
        }

        public string PauseTimeRange
        {
            get;
            set;
        }

        public Tuple<DateTime, DateTime>[] PauseTimeRanges
        {
            get
            {
                var retVal = new List<Tuple<DateTime, DateTime>>();
                try
                {
                    if (!String.IsNullOrEmpty(PauseTimeRange))
                    {
                        var ranges = PauseTimeRange.Split(new char[] { ';', ',' });
                        foreach (var range in ranges)
                        {
                            if (String.IsNullOrEmpty(range))
                                continue;
                            
                            var times = range.Split(new char[] { '-' });
                            if (times.Count() != 2)
                                continue;

                            retVal.Add(new Tuple<DateTime, DateTime>(
                                Engine.Instance.Today.Add(TimeSpan.Parse(times[0])),
                                Engine.Instance.Today.Add(TimeSpan.Parse(times[1]))
                                ));
                        }
                    }

                }
                catch
                {
                }
                return retVal.ToArray();
            }
        }

        public bool IsInPauseTimeRange
        {
            get
            {
                bool retVal = false;
                try
                {
                    var ranges = PauseTimeRanges;
                    foreach (var range in ranges)
                    {
                        if (DateTime.Now >= range.Item1 &&
                            DateTime.Now <= range.Item2)
                        {
                            retVal = true;
                            break;
                        }
                    }
                }
                catch
                {
                }
                return retVal;
            }
        }

        public DateTime CurrentSophisPublishMaxTime
        {
            get
            {
                var retval = DateTime.MaxValue;
                
                if (String.IsNullOrEmpty(SophisPublishMaxTime))
                    return retval;

                try
                {
                    var offset = TimeSpan.Parse(SophisPublishMaxTime);

                    return retval = Engine.Instance.Today.Add(offset);
                }
                catch
                {
                }

                return retval;
            }
        }
        

        public VolMonitor Clone()
        {
            VolMonitor retval = new VolMonitor();
            
            retval.AutomaticalTrigger = AutomaticalTrigger;
            retval.WarningTrigger = WarningTrigger;
            retval.Workset = Workset;
            retval.VolatilityAlarm = false;
            retval.RepoAlarm = false;
            retval.StartTime = StartTime;
            retval.EndTime = EndTime;
            retval.CheckVolatilityMarketAfterProducing = CheckVolatilityMarketAfterProducing;
            retval.CheckThresholds = CheckThresholds;
            retval.CompleteRepublish = CompleteRepublish;
            retval.CheckedAt = new DateTime(2000, 1, 1);
            retval.PublishedAt = new DateTime(2000, 1, 1);
            retval.PublishingIteration = PublishingIteration;
            retval.RunOnce = RunOnce;
            retval.StandardStatus = StandardStatus;
            retval.Status = "";
            retval.DontUseBrokerQuotes = DontUseBrokerQuotes;
            retval.DontUseListedQuotes = DontUseListedQuotes;
            retval.UseManualQuotes = UseManualQuotes;
            retval.LongTermExtrapolationEnabled = LongTermExtrapolationEnabled;
            retval.MaxNumberOfSophisUpdates = MaxNumberOfSophisUpdates;
            retval.MinMinutesBetweenSophisUpdates = MinMinutesBetweenSophisUpdates;
            retval.MaxDaysBetweenSophisUpdates = MaxDaysBetweenSophisUpdates;
            retval.CheckFoRcoThreshold = CheckFoRcoThreshold;
            retval.CheckPrevDayThreshold = CheckPrevDayThreshold;
            retval.UpdateTimeThreshold = UpdateTimeThreshold;
            retval.SophisPublishMaxTime = SophisPublishMaxTime;
            retval.PauseTimeRange = PauseTimeRange;

            foreach (AutoATMThreshold threshold in Thresholds)
            {
                retval.AddThreshold(threshold.Clone());
            }

            return retval;
        }

        public bool DontUseBrokerQuotes
        {
            get;
            set;
        }

        public bool DontUseListedQuotes
        {
            get;
            set;
        }

        public bool UseManualQuotes
        {
            get;
            set;
        }

        public bool LongTermExtrapolationEnabled
        {
            get;
            set;
        }

        public bool RerefEnabled
        {
            get;
            set;
        }

        public bool OnlyMonitoring
        {
            get;
            set;
        }

        public bool CheckPrevDayThreshold
        {
            get;
            set;
        }

        public int MaxAllowedRefTimeStampDev
        {
            get;
            set;
        }

        public int UpdateTimeThreshold
        {
            get;
            set;
        }
        
        [ComVisible(false)]
        public IList<AutoFoRcoThreshold> FoRcoThresholds { get; set; }

        public AutoFoRcoThresholds FoRcoThresholdsCollection
        {
            get
            {
                return new AutoFoRcoThresholds(FoRcoThresholds);
            }
        }

        public void AddFoRcoThreshold(AutoFoRcoThreshold threshold)
        {
            threshold.Monitor = this;
            FoRcoThresholds.Add(threshold);
        }
    }
}