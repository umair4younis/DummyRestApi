namespace Puma.MDE.Data
{
    public class HVBMassSkewShifts
    {
        private double skewShift;

        public double SkewShift
        {
            get { return skewShift; }
            set { skewShift = value; }
        }
        private double skewFactor;

        public double SkewFactor
        {
            get { return skewFactor; }
            set { skewFactor = value; }
        }
        private int useSkewShift;

        public int UseSkewShift
        {
            get { return useSkewShift; }
            set { useSkewShift = value; }
        }
        private int underlyingTypeID;

        public int UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
    }
}
