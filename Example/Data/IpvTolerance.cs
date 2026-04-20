
using Puma.MDE.Common;
using System;
using System.Linq.Expressions;

namespace Puma.MDE.Data
{
    [Serializable]
    public class IpvTolerance : Entity 
    {
        private string _parameter;
        public string Parameter
        {
            get { return _parameter; }
            set { _parameter = value; NotifyPropertyChanged(() => Parameter); }
        }

        private string _toleranceFor;
        public string ToleranceFor
        {
            get { return _toleranceFor; }
            set { _toleranceFor = value; NotifyPropertyChanged(() => ToleranceFor); }
        }

        private decimal _lowerToleranceValue;
        public decimal LowerToleranceValue
        {
            get { return _lowerToleranceValue; }
            set
            {
                NotifyPropertyChangedDirty(ref _lowerToleranceValue, value, () => LowerToleranceValue);
                CheckToleranceType(() => LowerToleranceValue);
            }
        }

        private decimal _upperToleranceValue;
        public decimal UpperToleranceValue
        {
            get { return _upperToleranceValue; }
            set
            {
                NotifyPropertyChangedDirty(ref _upperToleranceValue, value, () => UpperToleranceValue);
                CheckToleranceType(() => UpperToleranceValue);
            }
        }

        private bool _relative;
        public bool Relative
        {
            get { return _relative; }
            set { NotifyPropertyChangedDirty(ref _relative, value, () => Relative); }
        }

        public bool IsToleranceSet
        {
            get
            {
                return  LowerToleranceValue != 0.0M || 
                        UpperToleranceValue != 0.0M;
            }
        }

        private void CheckToleranceType<T>(Expression<Func<T>> property)
        {
            if (!Equals(LowerToleranceValue, UpperToleranceValue))
            {
                if (Parameter == "Trading Max Relative No Of Bid Ask Outliners" ||
                Parameter == "Risk Max Relative No Of Bid Ask Outliners" ||
                Parameter == "Max Cutoff Deviation")
                {
                    var propertyName = "";
                    if (propertyName == "UpperToleranceValue")
                    {
                        LowerToleranceValue = UpperToleranceValue;
                    }
                    else
                    {
                        UpperToleranceValue = LowerToleranceValue;
                    }

                }
            }
        }

       //public void GetPropertyError(string propertyName, ErrorInfo info)
       // {
       //     switch (propertyName)
       //     {
       //         case "UpperToleranceValue":
       //         case "LowerToleranceValue":

       //             if (LowerToleranceValue >= UpperToleranceValue && (LowerToleranceValue != 0 || UpperToleranceValue != 0))
       //                 SetErrorInfo(info, "Lower tolerance must be less than Upper tolerance.", ErrorType.Critical);
       //             break;
                   
       //     }
       // }

       // public void GetError(ErrorInfo info)
       // {
       //     if (LowerToleranceValue >= UpperToleranceValue && (LowerToleranceValue != 0 || UpperToleranceValue != 0))
       //         SetErrorInfo(info, "Lower tolerance must be less than Upper tolerance.", ErrorType.Critical);
       // }

       // void SetErrorInfo(ErrorInfo info, string errorText, ErrorType errorType)
       // {
       //     info.ErrorText = errorText;
       //     info.ErrorType = errorType;
       // }

    }
}
