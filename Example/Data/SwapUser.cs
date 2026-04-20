using Puma.MDE.Common;
using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapUser : Entity
    {
        public string Login { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String Department { get; set; }
        public String UserRole { get; set; }
    }
}
