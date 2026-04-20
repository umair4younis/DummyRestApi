using System.Text;

namespace Puma.MDE.Data
{
    public class MDESmartMarkupAlgo
    {
        public int     Id                   { get; set; }
        public string  Payoff               { get; set; }
        public int     SmartMarkup          { get; set; }
        public decimal UpperMaxMarkupChange { get; set; }
        public decimal LowerMaxMarkupChange { get; set; }
        public decimal MaxMaturityChange    { get; set; }
        public decimal MaxVolumeChange      { get; set; }
        public decimal HistTradeWindow      { get; set; }
        public decimal TargetHitRatio       { get; set; }
        public decimal MinTradeNumber       { get; set; }
        public bool    IsDirty              { get; set; }
        public bool    IsValid              { get; set; }


        public MDESmartMarkupAlgo Clone()
        {
            MDESmartMarkupAlgo retval = new MDESmartMarkupAlgo();

            retval.Payoff               = Payoff;
            retval.SmartMarkup          = SmartMarkup;
            retval.UpperMaxMarkupChange = UpperMaxMarkupChange;
            retval.LowerMaxMarkupChange = LowerMaxMarkupChange;
            retval.MaxMaturityChange    = MaxMaturityChange;
            retval.MaxVolumeChange      = MaxVolumeChange;
            retval.HistTradeWindow      = HistTradeWindow;
            retval.TargetHitRatio       = TargetHitRatio;
            retval.MinTradeNumber       = MinTradeNumber;
            retval.IsDirty              = IsDirty;
            retval.IsValid              = IsValid;

            return retval;
        }

        public bool Equals(MDESmartMarkupAlgo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.Payoff               == Payoff                 &&
                   other.SmartMarkup          == SmartMarkup            &&
                   other.UpperMaxMarkupChange == UpperMaxMarkupChange   &&
                   other.LowerMaxMarkupChange == LowerMaxMarkupChange   &&
                   other.MaxMaturityChange    == MaxMaturityChange      &&
                   other.MaxVolumeChange      == MaxVolumeChange        &&
                   other.HistTradeWindow      == HistTradeWindow        &&
                   other.TargetHitRatio       == TargetHitRatio         &&
                   other.MinTradeNumber       == MinTradeNumber;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("MDESmartMarkupAlgo<Payoff: " + Payoff);
            str.Append(", SmartMarkup: "             + SmartMarkup);
            str.Append(", UpperMaxMarkupChange: "    + UpperMaxMarkupChange);
            str.Append(", LowerMaxMarkupChange: "    + LowerMaxMarkupChange);
            str.Append(", MaxMaturityChange: "       + MaxMaturityChange);
            str.Append(", MaxVolumeChange: "         + MaxVolumeChange);
            str.Append(", HistTradeWindow: "         + HistTradeWindow);
            str.Append(", TargetHitRatio: "          + TargetHitRatio);
            str.Append(", MinTradeNumber: "          + MinTradeNumber + ">");

            return str.ToString();
        }
    }
}
