
namespace Puma.MDE.OPUS.Models
{
    public class PortfolioGraphQlMember
    {
        public PortfolioGraphQlMemberAsset asset { get; set; }

        public PercentAmountValue weight { get; set; }

        public string unit { get; set; }
    }
}
