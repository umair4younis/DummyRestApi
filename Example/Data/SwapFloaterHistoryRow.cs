using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapFloaterHistoryRow : Entity
    {
        public string DbName { get; set; } 
        public int InstrumentId { get; set; }
        public string Description { get; set; }
        public DateTime StartDate {  get; set; }
        public DateTime EndDate { get; set; }
        public double FloatingRate { get; set; }
    }
}
