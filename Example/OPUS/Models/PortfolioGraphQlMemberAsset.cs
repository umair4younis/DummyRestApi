
namespace Puma.MDE.OPUS.Models
{
    public class PortfolioGraphQlMemberAsset
    {
        public long id { get; set; }

        public string uuid { get; set; }

        public string name { get; set; }

        public PortfolioGraphQlSymbol isin { get; set; }

        public PortfolioGraphQlSymbol ric { get; set; }

        public PortfolioGraphQlSymbol bbg { get; set; }
    }
}
