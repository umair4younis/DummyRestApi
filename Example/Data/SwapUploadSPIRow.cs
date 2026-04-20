using Newtonsoft.Json;
using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    public class SwapUploadSPIRow : Entity
    {
        public string DbName { get; set; }
        public int AccountId { get; set; }
        public int? InstrumentTypeId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrumentType InstrumentType
        {
            get
            {
                if (InstrumentTypeId.HasValue)
                {
                    return new SwapAccountInstrumentType();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                InstrumentTypeId = value.Id;
                NotifyPropertyChanged(() => InstrumentType);
                NotifyPropertyChanged(() => InstrumentTypeId);
            }
        }
        public string xInstrumentTypeName
        {
            get
            {
                return InstrumentType == null ? string.Empty : InstrumentType.TypeName;
            }
            set
            {
                var instrumentType = value;
                if (instrumentType != null)
                {
                    InstrumentType = new SwapAccountInstrumentType();
                }
            }
        }
        public string InstrumentReference { get; set; }
    }
}
