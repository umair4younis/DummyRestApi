using Puma.MDE.Common;

namespace Puma.MDE.Data
{
    public class SwapCollateralCheckRow : Entity
    {
        public string DbName { get; set; }
        public int AccountId { get; set; }
        public int Lookback { get; set; }
        public double RatioLimit { get; set; }
    }
}
