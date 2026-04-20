using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{
    public class VolatilityDataAtmMode : Entity
    {
        private int _ClassificationId; 
        public int ClassificationId 
        {
            get { return _ClassificationId; }
            set { _ClassificationId = value; NotifyPropertyChanged(() => ClassificationId); }
        }
        

        public string Maturity
        {
            get { return _MaturityD.ToShortDateString(); }
        }

        private DateTime _MaturityD;
        public DateTime MaturityDate
        {
            get { return _MaturityD; }
            set { _MaturityD = value; NotifyPropertyChanged(() => MaturityDate); NotifyPropertyChanged(() => Maturity); }
        }

        private double _Atm=0.0;
        public double Atm
        {
            get { return _Atm; }
            set { _Atm = value; NotifyPropertyChanged(() => Atm); }
        }

        private double _Spread=0.0;
        public double Spread
        {
            get { return _Spread; }
            set {
                _Spread = value; NotifyPropertyChanged(() => Spread);
            }
        }


        public VolatilityDataAtmMode() { }

       
        //public VolatilityDataAtmMode( int classificationId ,string maturity, double atm, double spread)
        //{
        //    this.ClassificationId = classificationId;
        //    this.Maturity = maturity;
        //    this.Atm = atm;
        //    this.Spread = spread;
        //}

        public VolatilityDataAtmMode(int classificationId, DateTime maturityDate, double atm, double spread)
        {
            this.ClassificationId = classificationId;
            this.MaturityDate = maturityDate;
            this.Atm = atm;
            this.Spread = spread;
        }


        public VolatilityDataAtmMode Clone()
        {
            VolatilityDataAtmMode retval = new VolatilityDataAtmMode();

            retval.ClassificationId = ClassificationId;
            retval.MaturityDate = MaturityDate;
            retval.Atm      = Atm;
            retval.Spread   = Spread;
            
            return retval;
        }

        public bool Equals(VolatilityDataAtmMode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Maturity == Maturity && other.Atm == Atm && other.Spread == Spread && other.ClassificationId == ClassificationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(VolatilityDataAtmMode)) return false;
            return Equals((VolatilityDataAtmMode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Maturity.GetHashCode() * 397) ^ Atm.GetHashCode() ^ Spread.GetHashCode() ^  ClassificationId.GetHashCode();
            }
        }

    }
}
