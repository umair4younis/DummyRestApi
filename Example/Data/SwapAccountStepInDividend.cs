using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountStepInDividend : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public DateTime ExDate { get; set; }
        public DateTime PayDate { get; set; }
        public double Factor { get; set; }
        public double Div { get; set; }
        public DateTime? Booked { get; set; }
        public SwapUser User { get; set; }
        public String Refcons { get; set; }
        public String RefconsOrEmpty
        {
            get
            {
                return Refcons == null ? "" : Refcons;
            }
        }
    }

}
