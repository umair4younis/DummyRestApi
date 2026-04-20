using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapCountry : Entity
    {
        public String CountryName { get; set; }
        public String Code { get; set; }
        public SwapRegion Region { get; set; }

        public bool IsRegionNull() { return Region == null; }
        public string xCodeWithName
        {
            get
            {
                return string.Format("{0} ({1}) - {2}", Code, CountryName, IsRegionNull() ? "" : Region.RegionName);
            }
        }
    }
}
