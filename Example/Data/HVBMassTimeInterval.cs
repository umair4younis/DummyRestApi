using System;

namespace Puma.MDE.Data
{
    public class HVBMassTimeInterval: Puma.MDE.Common.Entity
    {
        private int timeIntervalId;
        public int TimeIntervalId
        {
            get { return timeIntervalId; }
            set { timeIntervalId = value; NotifyPropertyChanged(() => TimeIntervalId); }
        }

        private double barrierMinTimeInterval;
        public double BarrierMinTimeInterval
        {
            get { return barrierMinTimeInterval; }
            set { barrierMinTimeInterval = value; NotifyPropertyChanged(() => BarrierMinTimeInterval); }
        }

        private string maturityMax;
        public string MaturityMax
        {
            get { return maturityMax; }
            set { maturityMax = value; NotifyPropertyChanged(() => MaturityMax); }
        }

        private HVBUnderlyingTypes hvbMassUnderlyingTypes;
        public HVBUnderlyingTypes HvbMassUnderlyingTypes
        {
            get { return hvbMassUnderlyingTypes; }
            set { hvbMassUnderlyingTypes = value; NotifyPropertyChanged(() => HvbMassUnderlyingTypes); }
        }

        private string interval;
        public string Interval
        {
            get { return interval; }
            set { interval = value; NotifyPropertyChanged(() => Interval); }
        }

        public String minMaturity;
        public String MinMaturity
        {
            get { return minMaturity; }
            set { minMaturity = value; NotifyPropertyChanged(() => MinMaturity); }
        }

    }
}
