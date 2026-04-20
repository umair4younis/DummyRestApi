using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("0BABEA22-E305-4cff-83ED-9333B907F5E6")]
    public class PddaMonitors : IEnumerable
    {
        IList<PddaMonitor> collection;
        public PddaMonitors(IList<PddaMonitor> collection)
        {
            this.collection = collection;
        }
        [DispId(-4)]
        public IEnumerator GetEnumerator()
        {
            return collection.GetEnumerator();
        }
        public void Add(PddaMonitor item)
        {
            collection.Add(item);
        }
        public void Clear()
        {
            collection.Clear();
        }
        public PddaMonitor this[int index]
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
        public IList<PddaMonitor> Collection
        {
            get
            {
                return collection;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("C715F7B3-608A-4714-B2AA-9C7042C23BAD")]
    [ComVisible(true)]
    public class PddaMonitor
    {
        public PddaMonitor()
        {
            Workset = "Standard";
            CheckedAt = DateTime.MinValue;
            CalculationInterval = 60;
            Enabled = false;
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
        public bool Enabled { get; set; }
        public bool Acknowledged { get; set; }
        public DateTime CheckedAt { get; set; }
        public double CalculationInterval { get; set; }
        public double TriggerThreshold { get; set; }
        public double StrongTriggerThreshold { get; set; }
        public string Status { get; set; }
        public bool Alert { get; set; }

        //PDDAOrc _pddaOrc = null;
        //public PDDAOrc PDDAOrc
        //{
        //    get
        //    {
        //        if (_pddaOrc != null)
        //            return _pddaOrc;

        //        _pddaOrc = new PDDAOrc();
        //        return _pddaOrc;
        //    }
        //}

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

        AiAlgorithm _aiAlgorithm = null;
        public AiAlgorithm GetAiAlgorithm()
        {
            if (_aiAlgorithm != null)
                return _aiAlgorithm;

            AiAlgorithmLink link = Engine.Instance.Factory.GetAiAlgorithmForPddaMonitor(this.Id);
            if (link != null)
            {
                _aiAlgorithm = Engine.Instance.Factory.GetAiAlgorithm(link.AlgorithmId);
                return _aiAlgorithm;
            }

            return null;
        }

        public PddaMonitor Clone()
        {
            PddaMonitor retval = new PddaMonitor();

            retval.Workset = Workset;
            retval.StartTime = StartTime;
            retval.EndTime = EndTime;
            retval.CheckedAt = new DateTime(2000, 1, 1);
            retval.CalculationInterval = CalculationInterval;
            retval.TriggerThreshold = TriggerThreshold;
            retval.StrongTriggerThreshold = StrongTriggerThreshold;

            return retval;
        }
    }
}
