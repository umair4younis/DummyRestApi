using System;

namespace Puma.MDE.Data
{
    public class HVBMassEpsilonShift
    {
        public bool Equals(HVBMassEpsilonShift hvbMassEpsilonShift)
        {
            if (ReferenceEquals(null, hvbMassEpsilonShift)) return false;
            if (ReferenceEquals(this, hvbMassEpsilonShift)) return true;
            return hvbMassEpsilonShift.UnderlyingTypeId == UnderlyingTypeId && hvbMassEpsilonShift.ShiftClassId == ShiftClassId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassEpsilonShift)) return false;
            return Equals((HVBMassEpsilonShift)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Convert.ToInt32(UnderlyingTypeId) ^ Convert.ToInt32(ShiftClassId);
            }
        }

        private int underlyingTypeId;

        public int UnderlyingTypeId
        {
            get { return underlyingTypeId; }
            set { underlyingTypeId = value; }
        }
        private int shiftClassId;

        public int ShiftClassId
        {
            get { return shiftClassId; }
            set { shiftClassId = value; }
        }
        private double epsilonShift;

        public double EpsilonShift
        {
            get { return epsilonShift; }
            set { epsilonShift = value; }
        }

        private double repoShift;

        public double RepoShift
        {
            get { return repoShift; }
            set { repoShift = value; }
        }
    }
}
