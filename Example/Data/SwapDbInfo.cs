using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapDbInfo
    {
        public SwapDbInfo(string name) { this.Name = name; }
        public string Name { get; set; }

        public bool isValid()
        {
            return (!String.IsNullOrEmpty(Name));
        }
    }
}
