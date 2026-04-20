using System;

namespace Puma.MDE.Data
{
    class HVBMassAuditShiftClass
    {
        private int changeId;

        public int ChangeId
        {
            get { return changeId; }
            set { changeId = value; }
        }
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
    }
}
