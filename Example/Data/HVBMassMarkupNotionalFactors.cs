namespace Puma.MDE.Data
{
    public class HVBMassMarkupNotionalFactors
    {
        public int     Id       { get; set; }
        public decimal Notional { get; set; }
        public decimal Factor   { get; set; }
        public bool    IsDirty  { get; set; }


        public override string ToString()
        {
            return Notional + "\t" + Factor;
        }

        public HVBMassMarkupNotionalFactors Clone()
        {
            HVBMassMarkupNotionalFactors retval = new HVBMassMarkupNotionalFactors();

            retval.Id       = Id;
            retval.Notional = Notional;
            retval.Factor   = Factor;
            retval.IsDirty  = IsDirty;

            return retval;
        }

    }
}