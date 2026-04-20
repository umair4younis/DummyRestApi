using System;

namespace Puma.MDE.Data
{
    [Serializable]
    public class UnderlyingLastUpdate
    {
        public DateTime SophisDt { get; set; }
        public DateTime ORCDt { get; set; }
    }
}
