using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{


    [ComVisible(true)]
    [Serializable]
    public class SwapStepInFixing : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public DateTime XDate { get; set; }
        public double Fixing { get; set; }

    }
}
