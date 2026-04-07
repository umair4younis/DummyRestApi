using System.Collections.Generic;


namespace Puma.MDE.OPUS.Models
{
    public class AssetCompositionPatchPayload
    {
        public string Name { get; set; } = "Portfolio Composition Update";

        public List<CompositionMember> Members { get; set; } = new List<CompositionMember>();
    }
}
