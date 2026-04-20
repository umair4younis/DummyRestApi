namespace Puma.MDE.Data
{
    public class myRoboMarkupComplexityFactors
    {
        public int      Id              { get; set; }
        public string   Complexity      { get; set; }
        public decimal  ComplexityFactor{ get; set; }
        public bool     IsDirty         { get; set; }

        public override string ToString()
        {
            return Complexity + "\t\t" + ComplexityFactor;
        }

        public myRoboMarkupComplexityFactors Clone()
        {
            myRoboMarkupComplexityFactors retval = new myRoboMarkupComplexityFactors();

            retval.Id               = Id;
            retval.Complexity       = Complexity;
            retval.ComplexityFactor = ComplexityFactor;

            return retval;
        }

    }
}