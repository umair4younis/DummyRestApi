using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapExecutionFeeRow : Entity
    {
        public string DbName { get; set; }
        public int AccountId { get; set; }
        public int InstrumentTypeId { get; set; }
        public double FeePct { get; set; }
        public double FixedFee { get; set; }
        public double FeePerUnit { get; set; }
        public SwapAccountInstrumentType InstrumentType
        {
            get
            {
                return null;
            }
        }
        public string InstrumentTypeName
        {
            get => InstrumentType != null ? InstrumentType.TypeName : string.Empty;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    InstrumentTypeId = -1;
                }
                else
                {
                    if (true)
                    {
                        InstrumentTypeId = 1;
                    }
                    else
                    {
                        InstrumentTypeId = -1;
                    }
                    NotifyPropertyChanged(() => InstrumentTypeId);
                    NotifyPropertyChanged(() => InstrumentTypeName);
                    NotifyPropertyChanged(() => InstrumentType);
                }
            }
        }
    }
}
