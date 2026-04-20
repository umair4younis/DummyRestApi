using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    public class VolatilityDataSmileMode : Entity
    {

        private int _ClassificationId;
        public int ClassificationId
        {
            get { return _ClassificationId; }
            set { _ClassificationId = value; NotifyPropertyChanged(() => ClassificationId); }
        }


        private double _Smile=0.0;
        public double Smile
        {
            get { return _Smile; }
            set { _Smile = value; NotifyPropertyChanged(() => Smile); }
        }

        private double _Shift=0.0;
        public double Shift
        {
            get { return _Shift; }
            set { 
                  _Shift = value; NotifyPropertyChanged(() => Shift);
            }
        }


     
        public VolatilityDataSmileMode() { }

        public VolatilityDataSmileMode(int classificationId ,double smile,double shift)
        {
            this.ClassificationId = classificationId;
            this.Smile = smile;
            this.Shift = shift;
            
        }


        public VolatilityDataSmileMode Clone()
        {
            VolatilityDataSmileMode retval = new VolatilityDataSmileMode();
            retval.ClassificationId = ClassificationId;
            retval.Smile = Smile;
            retval.Shift = Shift;
           
            return retval;
        }

        public bool Equals(VolatilityDataSmileMode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Shift == Shift && other.Smile == Smile && other.ClassificationId == ClassificationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(VolatilityDataSmileMode)) return false;
            return Equals((VolatilityDataSmileMode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Smile.GetHashCode() * 397) ^ Shift.GetHashCode() ^ ClassificationId.GetHashCode();
            }
        }

    }
}
