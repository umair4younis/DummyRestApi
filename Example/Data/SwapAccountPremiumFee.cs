using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapAccountPremiumFee : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public DateTime? xDate { get; set; }
        public double? AdditionalFee { get; set; }
    }
}
