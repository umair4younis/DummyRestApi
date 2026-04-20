using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapSophisPortfolioRow : Entity
    {
        public String DbName { get; set; }
        public int AccountId { get; set; }
        public int PortfolioId { get; set; }
    }
}
