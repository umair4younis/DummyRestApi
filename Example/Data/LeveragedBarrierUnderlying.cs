using System;

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public enum LeveragedBarrierStyleEnum
    {
        [Description("Down")]
        Down,
        [Description("Down Or Equal")]
        DownOrEqual,
        [Description("Up")]
        Up,
        [Description("Up Or Equal")]
        UpOrEqual
    }

    [ComVisible(false)]
    public enum LeveragedBarrierKindEnum
    {
        [Description("%")]
        Percentage,
        [Description("Abs")]
        Absolute
    }

    [ComVisible(false)]
    public enum LeveragedBarrierObservationEnum
    {
        [Description("Close")]
        Close,
        [Description("Close & Intraday")]
        CloseAndIntraday
    }

    [ComVisible(false)]
    public enum LeveragedBarrierDeskEnum
    {
        [Description("All Desks")]
        All,
        [Description("Single Index")]
        SingleIndex,
        [Description("Single Stock")]
        SingleStock
    }

    [ComVisible(false)]
    public class LeveragedBarrierUnderlying : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        long id;
        public long Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                NotifyPropertyChanged("Id");
            }
        }


        string leveragedunderlying;
        
        public string LeveragedUnderlying
        {
            get
            {
                return leveragedunderlying;
            }
            set
            {
                leveragedunderlying = value;
                NotifyPropertyChanged("LeveragedUnderlying");
            }
        }

        string baseunderlying;
        
        public string BaseUnderlying
        {
            get
            {
                return baseunderlying;
            }
            set
            {
                baseunderlying = value;
                NotifyPropertyChanged("BaseUnderlying");
            }
        }

        LeveragedBarrierStyleEnum leveragedbarrierstyle;
        
        public LeveragedBarrierStyleEnum LeveragedBarrierStyle
        {
            get
            {
                return leveragedbarrierstyle;
            }
            set
            {
                leveragedbarrierstyle = value;
                NotifyPropertyChanged("LeveragedBarrierStyle");
            }
        }

        LeveragedBarrierKindEnum leveragedbarrierkind;
        
        public LeveragedBarrierKindEnum LeveragedBarrierKind
        {
            get
            {
                return leveragedbarrierkind;
            }
            set
            {
                leveragedbarrierkind = value;
                NotifyPropertyChanged("LeveragedBarrierKind");
            }
        }

        LeveragedBarrierObservationEnum leveragedbarrierobservation;
        
        public LeveragedBarrierObservationEnum LeveragedBarrierObservation
        {
            get
            {
                return leveragedbarrierobservation;
            }
            set
            {
                leveragedbarrierobservation = value;
                NotifyPropertyChanged("LeveragedBarrierObservation");
            }
        }

        decimal leveragedbarrier;
        
        public decimal LeveragedBarrier
        {
            get
            {
                return leveragedbarrier;
            }
            set
            {
                leveragedbarrier = value;
                NotifyPropertyChanged("LeveragedBarrier");
            }
        }

        int leveragedperiod;
        
        public int LeveragedPeriod
        {
            get
            {
                return leveragedperiod;
            }
            set
            {
                leveragedperiod = value;
                NotifyPropertyChanged("LeveragedPeriod");
                NotifyPropertyChanged("BusinessDaysLeft");
            }
        }


        LeveragedBarrierObservationEnum basebarrierobservation;
        
        public LeveragedBarrierObservationEnum BaseBarrierObservation
        {
            get
            {
                return basebarrierobservation;
            }
            set
            {
                basebarrierobservation = value;
                NotifyPropertyChanged("BaseBarrierObservation");
            }
        }

        LeveragedBarrierStyleEnum basebarrierstyle;
        
        public LeveragedBarrierStyleEnum BaseBarrierStyle
        {
            get
            {
                return basebarrierstyle;
            }
            set
            {
                basebarrierstyle = value;
                NotifyPropertyChanged("BaseBarrierStyle");
            }
        }

        LeveragedBarrierKindEnum basebarrierkind;
        
        public LeveragedBarrierKindEnum BaseBarrierKind
        {
            get
            {
                return basebarrierkind;
            }
            set
            {
                basebarrierkind = value;
                NotifyPropertyChanged("BaseBarrierKind");
            }
        }

        decimal basebarrier;
        
        public decimal BaseBarrier
        {
            get
            {
                return basebarrier;
            }
            set
            {
                basebarrier = value;
                NotifyPropertyChanged("BaseBarrier");
            }
        }

        DateTime? breachedon;
        
        public DateTime? BreachedOn
        {
            get
            {
                return breachedon;
            }
            set
            {
                breachedon = value;
                NotifyPropertyChanged("BreachedOn");
                NotifyPropertyChanged("BusinessDaysLeft");
            }
        }


        LeveragedBarrierDeskEnum leveragedbarrierdesk;
        
        public LeveragedBarrierDeskEnum LeveragedBarrierDesk
        {
            get
            {
                return leveragedbarrierdesk;
            }
            set
            {
                leveragedbarrierdesk = value;
                NotifyPropertyChanged("LeveragedBarrierDesk");
            }
        }

        DateTime? doNotCheckBefore;

        public DateTime? DoNotCheckBefore
        {
            get
            {
                return doNotCheckBefore;
            }
            set
            {
                doNotCheckBefore = value;
                NotifyPropertyChanged("DoNotCheckBefore");
            }
        }

        static bool IsBreached(double spot, double barrier, LeveragedBarrierStyleEnum type)
        {
            if (spot == 0)
                return false;

            if (type == LeveragedBarrierStyleEnum.Down)
                return spot < barrier;
            if (type == LeveragedBarrierStyleEnum.DownOrEqual)
                return spot <= barrier;
            if (type == LeveragedBarrierStyleEnum.Up)
                return spot > barrier;
            if (type == LeveragedBarrierStyleEnum.UpOrEqual)
                return spot >= barrier;

            throw new NotImplementedException();
        }

        public int? BusinessDaysLeft
        {
            get
            {
                if (!BreachedOn.HasValue)
                    return null;

                try
                {
                    SR2COM.Engine engine = new SR2COM.Engine();
                    SR2COM.IInstrument ins = engine.get_Instrument(LeveragedUnderlying);

                    SR2COM.ICalendar cal = ins.Calendar;
                    if (cal == null)
                        cal = ins.Currency.Calendar;

                    var periodEnd = cal.AddNumberOfDays(BreachedOn.Value, LeveragedPeriod);
                    int retval = Convert.ToInt32((periodEnd - Engine.Instance.Today).TotalDays);
                    if (retval < 0)
                        return null;

                    return retval;
                }
                catch
                {
                    return null;
                }

            }
        }

        public bool BelongsTo(string workset)
        {
            if (LeveragedBarrierDesk == LeveragedBarrierDeskEnum.All)
                return true;

            bool isstockworkset = workset.ToLower().Contains("stock");
            bool isindexworkset = workset.ToLower().Contains("indices") || workset.ToLower().Contains("index");
            if (LeveragedBarrierDesk == LeveragedBarrierDeskEnum.SingleStock && isstockworkset)
                return true;
            if (LeveragedBarrierDesk == LeveragedBarrierDeskEnum.SingleIndex && isindexworkset)
                return true;
            
            return false;
        }

        public string Message
        {
            get
            {
                if (IsLeverageBarrierBreached)
                {

                    if (BusinessDaysLeft > 0)
                        return String.Format("Leveraged Underlying '{0}' - Corporate Action: ReverseStockSplit expected. Please perform necessary Adjustments. {1} Days left.",
                            LeveragedUnderlying,
                                BusinessDaysLeft);

                    return String.Format("POSSIBLE EXDATE TODAY Leveraged Underlying '{0}' - Corporate Action: ReverseStockSplit expected. Please perform necessary adjustments",
                        LeveragedUnderlying);
                }

                if (IsBaseBarrierBreached)
                {
                    return String.Format("Warning: Base Underlying '{1}' Barrier Breached, Leveraged Underlying '{0}'",
                        LeveragedUnderlying, 
                            BaseUnderlying);
                }

                return String.Empty;
            
            }
        }

        static bool IsBarrierBreached(string reference, decimal barrierdef, LeveragedBarrierStyleEnum style, LeveragedBarrierKindEnum kind, LeveragedBarrierObservationEnum observation, int period, ref DateTime? breachedOn)
        {
            bool retval = false;
            try
            {
                int maxDaysFixingsGoBack = 100;
                try
                {
                    maxDaysFixingsGoBack = Convert.ToInt32(Engine.Instance.Config.Get("max_days_levbar_fixings_go_back"));
                }
                catch
                {
                }
                
                SR2COM.Engine engine = new SR2COM.Engine();
                SR2COM.IInstrument ins = engine.get_Instrument(reference);

                SR2COM.ICalendar cal = ins.Calendar;
                if (cal == null)
                    cal = ins.Currency.Calendar;

                if (breachedOn.HasValue)
                {
                    if (cal.AddNumberOfDays(breachedOn.Value, period) > Engine.Instance.Today)
                        breachedOn = null;
                    else
                        return true;
                }

                double close = 0;
                double barrier = 0;
                DateTime closedt;

                int count;

                DateTime dt = Engine.Instance.Today;
                count = 0;
                while (close == 0 && count < maxDaysFixingsGoBack)
                {
                    dt = dt.AddDays(-1);
                    count++;
                    close = ins.get_HistoricalValue("HVB_CLOSE", dt);
                }
                
                if (count>=maxDaysFixingsGoBack)
                    throw new PumaMDEException(String.Format("can't find fixing {0} max_days={1}", ins.Reference, maxDaysFixingsGoBack));

                closedt = dt;

                double closebefore = 0;
                double barrierbefore = 0;
                DateTime closebeforedt;
                count = 0;
                while (closebefore == 0 && count < maxDaysFixingsGoBack)
                {
                    dt = dt.AddDays(-1);
                    count++;
                    closebefore = ins.get_HistoricalValue("HVB_CLOSE", dt);
                }
                
                if (count >= maxDaysFixingsGoBack)
                    throw new PumaMDEException(String.Format("can't find fixing {0} max_days={1}", ins.Reference, maxDaysFixingsGoBack));

                closebeforedt = dt;


                if (kind == LeveragedBarrierKindEnum.Percentage)
                {
                    double barrierStep = Convert.ToDouble(barrierdef);
                    if (style == LeveragedBarrierStyleEnum.Down ||
                        style == LeveragedBarrierStyleEnum.DownOrEqual)
                        barrierStep = -barrierStep;

                    barrier = close * (100 + barrierStep) / 100;
                    barrierbefore = closebefore * (100 + barrierStep) / 100;
                }
                else
                {
                    barrier = Convert.ToDouble(barrierdef);
                    barrierbefore = Convert.ToDouble(barrierdef);
                }

                if (observation == LeveragedBarrierObservationEnum.Close ||
                    observation == LeveragedBarrierObservationEnum.CloseAndIntraday)
                {
                    if (IsBreached(close, barrierbefore, style))
                    {
                        retval = true;
                        breachedOn = closedt;
                    }
                }

                if (observation == LeveragedBarrierObservationEnum.CloseAndIntraday)
                {
                    if (IsBreached(ins.TheoreticalValue, barrier, style))
                    {
                        retval = true;
                        breachedOn = Engine.Instance.Today;
                    }

                    if (IsBreached(ins.get_HistoricalValue("HVB_LOW", closedt), barrierbefore, style))
                    {
                        retval = true;
                        breachedOn = closedt;
                    }

                    if (IsBreached(ins.get_HistoricalValue("HVB_HIGH", closedt), barrierbefore, style))
                    {
                        retval = true;
                        breachedOn = closedt;
                    }
                }
            }
            catch (Exception e)
            {
                Engine.Instance.ErrorException("breach check", e);
            }

            if (!retval)
                breachedOn = null;

            return retval;
        }
        
        
        public bool IsLeverageBarrierBreached
        {
            get
            {
                DateTime? breachedOn = BreachedOn;

                bool retval = false;

                if (DoNotCheckBefore.HasValue && DoNotCheckBefore.Value >= Engine.Instance.Today)
                {
                    retval = false;
                    breachedOn = null;
                }
                else
                {
                    retval = IsBarrierBreached(
                         LeveragedUnderlying,
                             LeveragedBarrier,
                                 LeveragedBarrierStyle,
                                     LeveragedBarrierKind,
                                         LeveragedBarrierObservation,
                                             LeveragedPeriod,
                                                 ref breachedOn);
                }
                
                if (BreachedOn != breachedOn)
                    BreachedOn = breachedOn;
                
                return retval;
            }
        }

        public bool IsBaseBarrierBreached
        {
            get
            {
                DateTime? breachedOn = null;

                bool retval = IsBarrierBreached(
                    BaseUnderlying,
                        BaseBarrier,
                            BaseBarrierStyle,
                                BaseBarrierKind,
                                    BaseBarrierObservation,
                                        LeveragedPeriod,
                                            ref breachedOn);

                return retval;
            }
        }
    }
}
