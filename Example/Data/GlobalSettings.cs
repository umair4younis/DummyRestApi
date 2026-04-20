using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("6BD1C6AE-201C-4223-8F4B-938085F09A54")]
    [ComVisible(true)]

    public class GlobalSettings
    {
        public int Id { get; set; }
        public String Setting1 { get; set; }
        public String Setting2 { get; set; }
        public String Setting3 { get; set; }
        public int FXVolMarketServiceWait { get; set; }
        public bool AutoBOApprovalEnabled { get; set; }
        public bool KDBConnectionCheckEnabled { get; set; }
        public string KDBConnectionCheckFunction { get; set; }
        public bool KDBProtocollingEnabled { get; set; }
    }

}
