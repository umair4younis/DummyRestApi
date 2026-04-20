using Puma.MDE.Data;

namespace Quotation
{
    internal class Processing
    {
        public class QuoteProcessor
        {
            private Underlying und;

            public QuoteProcessor(Underlying und)
            {
                this.und = und;
            }
        }
    }
}