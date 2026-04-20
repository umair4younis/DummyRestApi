using System;

namespace Puma.MDE.Data
{
    public class HVBMassMUParameters
    {
        private int underlyingTypeID;

        public int UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
        private double shiftSize;

        public double ShiftSize
        {
            get { return shiftSize; }
            set { shiftSize = value; }
        }

        private int spotThreshold;

        public int SpotThreshold
        {
            get { return spotThreshold; }
            set { spotThreshold = value; }
        }
        private int useMUBarrierShift;

        public int UseMUBarrierShift
        {
            get { return useMUBarrierShift; }
            set { useMUBarrierShift = value; }
        }
        private double correlationThreshold;

        public double CorrelationThreshold
        {
            get { return correlationThreshold; }
            set { correlationThreshold = value; }
        }

        private String useMUBarrierShiftString;

        public String UseMUBarrierShiftString
        {
            get
            {
                if (this.UseMUBarrierShift == 0)
                {
                    return "NO";
                }
                else
                {
                    return "YES";
                }
            }
            set { useMUBarrierShiftString = value; }
        }
        private bool isDirty;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }
    }
}
