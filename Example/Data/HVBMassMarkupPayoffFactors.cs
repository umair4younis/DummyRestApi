namespace Puma.MDE.Data
{
    public class HVBMassMarkupPayoffFactors
    {
        public int     Id      { get; set; }
        public string  Payoff  { get; set; }
        public decimal Factor  { get; set; }
        public bool    IsDirty { get; set; }


        public override string ToString()
        {
            return Payoff + "\t" + Factor;
        }

        public HVBMassMarkupPayoffFactors Clone()
        {
            HVBMassMarkupPayoffFactors retval = new HVBMassMarkupPayoffFactors();

            retval.Id       = Id;
            retval.Payoff   = Payoff;
            retval.Factor   = Factor;
            retval.IsDirty  = IsDirty;

            return retval;
        }
    }
}
