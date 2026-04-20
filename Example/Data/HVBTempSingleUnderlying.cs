using System;

namespace Puma.MDE.Data
{
    public class HVBTempSingleUnderlying
    {
        private String sicovam;


        public String Sicovam
        {
            get { return sicovam; }
            set { sicovam = value; }
        }
        private String shiftClassID;

        public String ShiftClassID
        {
            get { return shiftClassID; }
            set { shiftClassID = value; }
        }
        private String underlyingTypeID;

        public String UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
    }
}
