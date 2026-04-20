using System;


namespace Puma.MDE
{
    [System.Runtime.InteropServices.ComVisible(false)]
    public class PumaMDEException : System.Exception
    {
        public PumaMDEException()
        {
        }
        public PumaMDEException(String message)
            : base(message)
        {
        }
        public PumaMDEException(String message, Exception e)
            : base(message, e)
        {
        }
    }
}
