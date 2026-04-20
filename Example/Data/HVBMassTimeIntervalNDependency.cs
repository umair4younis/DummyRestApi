namespace Puma.MDE.Data
{
    public class HVBMassTimeIntervalNDependency
    {
        private int timeIntervalId;

        public int TimeIntervalId
        {
            get { return timeIntervalId; }
            set { timeIntervalId = value; }
        }
        private double barrierMinTimeInterval;

        public double BarrierMinTimeInterval
        {
            get { return barrierMinTimeInterval; }
            set { barrierMinTimeInterval = value; }
        }
        private string maturityMax;

        public string MaturityMax
        {
            get { return maturityMax; }
            set { maturityMax = value; }
        }
        private int underlyingTypeId;

        public int UnderlyingTypeId
        {
            get { return underlyingTypeId; }
            set { underlyingTypeId = value; }
        }

        private string minMaturity;

        public string MinMaturity
        {
            get { return minMaturity; }
            set { minMaturity = value; }
        }

        private string interval;

        public string Interval
        {
            get { return interval; }
            set { interval = value; }
        }
    }
}
