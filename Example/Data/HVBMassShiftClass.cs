using System;

namespace Puma.MDE.Data
{
    public class HVBMassShiftClass
    {
        private int shiftClassId;

        public int ShiftClassId
        {
            get { return shiftClassId; }
            set { shiftClassId = value; }
        }
        private String classification;

        public String Classification
        {
            get { return classification; }
            set { classification = value; }
        }
        private double shiftMultiplier;

        public double ShiftMultiplier
        {
            get { return shiftMultiplier; }
            set { shiftMultiplier = value; }
        }

        private int volThreshold;

        public int VolThreshold
        {
            get { return volThreshold; }
            set { volThreshold = value; }
        }


        public bool isDirty;
        public bool IsDirty
        {
            get;
            set;
        }
    }
}
