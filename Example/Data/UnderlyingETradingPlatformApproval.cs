using System;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("905B57FD-1C18-461e-8DED-97887225F38C")]
    public class UnderlyingETradingPlatformApproval
    {
        public int Id {get;set;}
        public int UnderlyingId {get;set;}
        public int PlatformId { get; set; }

        public bool ApprovedByFO { get; set; }
        public bool ApprovedByFOMulti { get; set; }

        public bool ApprovedByBO { get; set; }

        public string MCA { get; set; }
    }
}
