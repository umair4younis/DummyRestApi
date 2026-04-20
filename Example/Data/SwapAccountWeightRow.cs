using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{


    [ComVisible(true)]
    [Serializable]
    public class SwapAccountWeightRow : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public int Count { get; set; }
        public double Weight { get; set; }
        public int AlertColor { get; set; }
        public int? InstrumentTypeId { get; set; }
        public int ComparisonOperator { get; set; }
        public string AssetClass
        {
            get
            {
                if (InstrumentTypeId.HasValue)
                {
                    SwapAccountInstrumentType instrType = new SwapAccountInstrumentType();
                    return instrType != null ? instrType.TypeName : string.Empty;
                }
                return string.Empty;
            }
        }
        public TypeComparisonOperator ComparisonOperatorEnum
        {
            get { return (TypeComparisonOperator)ComparisonOperator; }
            set
            {
                ComparisonOperator = (int)value;
                NotifyPropertyChanged(() => ComparisonOperator);
                NotifyPropertyChanged(() => ComparisonOperatorEnum);
            }
        }

        public int xAlertColor
        {
            get
            {
                if (AlertColor == 0)
                    return 1;
                else
                    return AlertColor;
            }
            set
            {
                AlertColor = value;
                NotifyPropertyChanged(() => AlertColor);
                NotifyPropertyChanged(() => xAlertColor);
                NotifyPropertyChanged(() => AlertBrush);
            }
        }

        public object AlertBrush
        {
            get
            {
                return null;
            }
        }
    }

}
