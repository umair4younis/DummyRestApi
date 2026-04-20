using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapStopLossKindRow : Entity
    {
        public SwapStopLossKindRow() { }
        public SwapStopLossKindRow(string xName, bool hasDate)
        {
            this.xName = xName;
            this.HasDate = hasDate;
        }

        public string xName { get; set; }
        public bool HasDate {  get; set; }
    }
}
