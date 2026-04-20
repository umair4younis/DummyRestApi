using Puma.MDE.Common.ViewModel;
using System;

namespace Puma.MDE.Data
{
    public class HVBMassAxeList : ViewModelBase
    {
        private int sicovam;
        public int Sicovam 
        {
            get { return sicovam; }
            set { sicovam = value; NotifyPropertyChanged(() => Sicovam); }
        }

        private double axeFactor;
        public double AxeFactor
        {
            get { return axeFactor; }
            set { axeFactor = value; NotifyPropertyChanged(() => AxeFactor); }
        }

        private DateTime validUntil = new DateTime();
        public DateTime ValidUntil
        {
            get { return validUntil; }
            set { validUntil = value; NotifyPropertyChanged(() => ValidUntil); }
        }

        private double maturityInYears ;
        public double MaturityInYears
        {
            get { return maturityInYears; }
            set { maturityInYears = value; NotifyPropertyChanged(() => MaturityInYears); }
        }


        private HVBMassUnderlyingShift hvbUnderShiftClass;
        public HVBMassUnderlyingShift HvbUnderShiftClass
        {
            get { return hvbUnderShiftClass; }
            set { hvbUnderShiftClass = value; NotifyPropertyChanged(() => HvbUnderShiftClass); }
        }

        private Titres titres;
        public Titres Titres
        {
            get { return titres; }
            set { titres = value; NotifyPropertyChanged(() => Titres); }
        }

        private bool isDirtyy;
        public bool IsDirtyy
        {
            get { return isDirtyy; }
            set { isDirtyy = value; NotifyPropertyChanged(() => IsDirtyy); }
        }

        private String underlyingReference;
        public String UnderlyingReference
        {
            get { return underlyingReference; }
            set { underlyingReference = value; NotifyPropertyChanged(() => UnderlyingReference); }
        }
    }
}
