using System;

namespace Puma.MDE.Data
{    
    [Serializable]
    public class TradingIndex
    {
        public string IndexReference { get; set; }
        public string IndexId { get; set; }
        public int SophisIndexId { get; set; }
        public int IndexCompositionSource { get; set; }
        public string IndexCompositionSourceReference { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime? NextUpdate { get; set; }
        public int UseCompositionSource { get; set; }
    }
}
