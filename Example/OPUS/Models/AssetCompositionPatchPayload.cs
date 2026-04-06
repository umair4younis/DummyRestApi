using System.Collections.Generic;


namespace Example.OPUS.Models
{
    public class AssetCompositionPatchPayload
    {
        public string Name { get; set; } = "Portfolio Composition Update";

        public List<CompositionMember> Members { get; set; } = new List<CompositionMember>();
    }
}
