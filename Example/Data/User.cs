using System;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("247E8936-50D6-4662-B7DB-B3B11C3CEBA8")]
    [ComVisible(true)]

    public class User
    {
        public int Id {get;set;}
        public String Name { get; set; }
        public String SophisUser 
        {
            get
            {
                return Name;
            }
        }
    }
}
