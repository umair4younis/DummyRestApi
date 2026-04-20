using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    public class HVBMassCorrsqueeze : Entity
    {

        public bool Equals(HVBMassCorrsqueeze hvbMassCorrsqueeze)
        {
            if (ReferenceEquals(null, hvbMassCorrsqueeze)) return false;
            if (ReferenceEquals(this, hvbMassCorrsqueeze)) return true;
            return hvbMassCorrsqueeze.UnderlyingTypeID == UnderlyingTypeID && hvbMassCorrsqueeze.ShiftClassID == ShiftClassID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(HVBMassCorrsqueeze)) return false;
            return Equals((HVBMassCorrsqueeze)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnderlyingTypeID * 397) ^ ShiftClassID;
            }
        }

        private int underlyingTypeID;

        public int UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
        private int shiftClassID;

        public int ShiftClassID
        {
            get { return shiftClassID; }
            set { shiftClassID = value; }
        }
        private double squeeze;

        public double Squeeze
        {
            get { return squeeze; }
            set { squeeze = value; }
        }

    }
}
