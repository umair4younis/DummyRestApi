using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    public class SwapInvestmentUniverseRow : Entity
    {
        public string DbName { get; set; }
        public int AccountId { get; set; }
        public int InstrumentId { get; set; }
        public double TargetWeight { get; set; }

        public SwapAccountInstrument Instrument
        {
            get => new SwapAccountInstrument();
        }
    }
}
