
namespace Example.OPUS.Models
{
    public class CompositionMember
    {
        public string Asset { get; set; }   // UUID of the child asset

        public string Unit { get; set; }    // e.g. "EUR/Pieces", "Pieces", etc.

        public WeightObject Weight { get; set; }
    }
}
