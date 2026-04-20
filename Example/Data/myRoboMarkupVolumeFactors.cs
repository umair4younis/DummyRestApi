namespace Puma.MDE.Data
{
    public class myRoboMarkupVolumeFactors
    {
        public int     Id           { get; set; }
        public decimal EURVolume    { get; set; }
        public decimal FactorSTMT   { get; set; }
        public decimal FactorLT     { get; set; }
        public bool    IsDirty      { get; set; }

        public override string ToString()
        {
            return EURVolume + "\t\t" + FactorSTMT + "\t\t" + FactorLT;
        }

        public myRoboMarkupVolumeFactors Clone()
        {
            myRoboMarkupVolumeFactors retval = new myRoboMarkupVolumeFactors();

            retval.Id           = Id;
            retval.EURVolume    = EURVolume;
            retval.FactorSTMT   = FactorSTMT;
            retval.FactorLT     = FactorLT;
            retval.IsDirty      = IsDirty;

            return retval;
        }

    }
}