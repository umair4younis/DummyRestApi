using System;

using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("3BBF3E36-C2BF-4dc7-8C77-A85490D22546")]
    public class ETradingPlatform
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
