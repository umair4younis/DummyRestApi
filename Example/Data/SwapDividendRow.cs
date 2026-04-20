using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapDividendRow : Entity
    {
        public DateTime PayDate {get; set; }
        public DateTime ExDate {get; set; }
        public double Div {  get; set; }


    }
}
