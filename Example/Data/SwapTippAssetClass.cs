using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapTippAssetClass : Entity
    {
        public String TippAssetClassName { get; set; }
    }
}
