namespace Puma.MDE.Data
{
    public class HVBMassBarrierShiftClass
    {
        public int Id { get; set; }

        private double barrierMax;

        public double BarrierMax
        {
            get { return barrierMax; }
            set { barrierMax = value; }
        }
        private double barrierShiftDigital;

        public double BarrierShiftDigital
        {
            get { return barrierShiftDigital; }
            set { barrierShiftDigital = value; }
        }
        private double barrierShiftContinous;

        public double BarrierShiftContinous
        {
            get { return barrierShiftContinous; }
            set { barrierShiftContinous = value; }
        }
        private double volatilityShift;

        public double VolatilityShift
        {
            get { return volatilityShift; }
            set { volatilityShift = value; }
        }
        private HVBMassTimeInterval hvbMassTimeInterval;

        public HVBMassTimeInterval HvbMassTimeInterval
        {
            get { return hvbMassTimeInterval; }
            set { hvbMassTimeInterval = value; }
        }
        private double barrierMin;

        public double BarrierMin
        {
            get { return barrierMin; }
            set { barrierMin = value; }
        }

        private string barrierMaxDisplay;
        public string BarrierMaxDisplay
        {
            get { return barrierMaxDisplay; }
            set { barrierMaxDisplay = value; }
        }

        private string barrierShiftDigitalDisplay;
        public string BarrierShiftDigitalDisplay
        {
            get { return barrierShiftDigitalDisplay; }
            set { barrierShiftDigitalDisplay = value; }
        }

        private string barrierShiftContinousDisplay;
        public string BarrierShiftContinousDisplay
        {
            get { return barrierShiftContinousDisplay; }
            set { barrierShiftContinousDisplay = value; }
        }

        private string volatilityShiftDisplay;
        public string VolatilityShiftDisplay
        {
            get { return volatilityShiftDisplay; }
            set { volatilityShiftDisplay = value; }
        }

        private string barrierMinDisplay;
        public string BarrierMinDisplay
        {
            get { return barrierMinDisplay; }
            set { barrierMinDisplay = value; }
        }


        public bool isDirty { get; set; }
    }
}
