namespace Puma.MDE.Data
{
    public class HVBMassUnderlyingShift
    {
        private int sicovam;

        public int Sicovam
        {
            get { return sicovam; }
            set { sicovam = value; }
        }
        private int shiftClassId;

        public int ShiftClassId
        {
            get { return shiftClassId; }
            set { shiftClassId = value; }
        }
        private int underlyingTypeId;

        public int UnderlyingTypeId
        {
            get { return underlyingTypeId; }
            set { underlyingTypeId = value; }
        }

        private int client1;
        public int Client1
        {
            get { return client1; }
            set { client1 = value; }
        }

        private int client2;
        public int Client2
        {
            get { return client2; }
            set { client2 = value; }
        }

        private int _isMyOneMarket;
        public int IsMyOneMarket
        {
            get { return _isMyOneMarket; }
            set { _isMyOneMarket = value; }
        }

    }
}
