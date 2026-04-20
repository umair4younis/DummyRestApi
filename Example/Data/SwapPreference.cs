using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapPreference 
    {
        public SwapPreference() { }
        public SwapPreference(int id) { this.Id = id; }
        public int Id { get; private set; }
        public String DbName { get; set; }
        public String PrefName { get; set; }
        public double? DoubleValue { get; set; }
        public String StringValue { get; set; }
        public DateTime? DateTimeValue { get; set; }
    }
}
