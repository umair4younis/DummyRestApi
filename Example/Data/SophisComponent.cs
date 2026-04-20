using Puma.MDE.Common;
using System;

namespace Puma.MDE.Data
{    
    [Serializable]
    public class SophisComponent : Entity
    {
        public int ComponentId { get; set; }
        public string ComponentReference { get; set; }
        public double ComponentWeight { get; set; }
        public SophisComposition SophisComposition { get; set; }

        public SophisComponent()
        {
        }

        public SophisComponent(SophisComponent component)
        {
            ComponentId = component.ComponentId;
            ComponentReference = component.ComponentReference;
            ComponentWeight = component.ComponentWeight;
        }
            
    }
}
