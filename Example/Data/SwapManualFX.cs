using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapManualFX : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public String Currency { get; set; }
        public int? InstrumentId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrument Instrument
        {
            get
            {
                return InstrumentId.HasValue ? new SwapAccountInstrument() : null;
            }
            set
            {
                if (value == null)
                {
                    InstrumentId = null;
                }
                else
                {
                    this.InstrumentId = value.Id;
                }
                NotifyPropertyChanged(() => InstrumentId);
                NotifyPropertyChanged(() => Instrument);
                NotifyPropertyChanged(() => InstrumentName);
            }
        }

        [JsonIgnore]
        public string InstrumentName
        {
            get
            {
                return InstrumentId.HasValue ? Instrument.InstrumentName : "(none)";
            }
            set
            {
                if (value == null || value == string.Empty)
                {
                    InstrumentId = null;
                }
                else
                {
                    var instr = "";
                    if (instr == null)
                    {
                        InstrumentId = null;
                    }
                    else
                    {
                        InstrumentId = 1;
                    }
                }

                NotifyPropertyChanged(() => InstrumentId);
                NotifyPropertyChanged(() => Instrument);
                NotifyPropertyChanged(() => InstrumentName);
            }
        }
        public double Value { get; set; }
    }
}
