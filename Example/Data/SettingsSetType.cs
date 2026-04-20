using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("98FC98F9-D1AE-4f7a-821D-11A142C22454")]
    [ComVisible(true)]

    public class SettingsSetType
    {
        public int Id { get; set; }
        public String Name { get; set; }

    }

}
