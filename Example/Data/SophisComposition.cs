using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{
    [Serializable]
    public class SophisComposition : Entity
    {
        public int Sicovam { get; set; }
        public int SicoPanier { get; set; }
        public string Reference { get; set; }
        public double? Weight { get; set; }        
        public bool IsDifference { get; set; }
    }
}
