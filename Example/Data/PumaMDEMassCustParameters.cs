namespace Puma.MDE.Data
{
    public class PumaMDEMassCustParameters
    {
        /*public PumaMDEMassCustParameters(string parameterName, int parameterValue)
        {
            this.ParameterName = parameterName;
            this.ParameterValue = parameterValue;
        }

        private string parameterName;

        public string ParameterName
        {
            get { return parameterName; }
            set { parameterName = value; }
        }
        private int parameterValue;

        public int ParameterValue
        {
            get { return parameterValue; }
            set { parameterValue = value; }
        }
        private bool isDirty;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }*/
        public bool ExpertMode { get; set; }
        public bool AllowHalves { get; set; }
        public int MaxNBMonths { get; set; }
        public int MaxNBWeeks { get; set; }
        public int MaxNBDays { get; set; }
        public bool ReadOnly { get; set; }
    }
}
