using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{
    public class QuixReweightModel : Entity
    {
        public String Name { get; set; }
        public String FormulaReference { get; set; }

        public bool IsManualFee
        {
            get
            {
                return Name.StartsWith("Manual-Fee");
            }
        }

        public bool IsStandardFee
        {
            get
            {
                return Name.StartsWith("Standard-Fee");
            }
        }
    }
}
