using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("7CA12F82-4756-4e78-A4DE-1CAB1BB37F7B")]
    [ComVisible(true)]

    public class QuoteFilterType
    {
        public int Id { get; set; }
        public String Name { get; set; }

    }

}
