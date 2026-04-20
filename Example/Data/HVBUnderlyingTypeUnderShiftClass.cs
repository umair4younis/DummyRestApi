namespace Puma.MDE.Data
{
    public class HVBUnderlyingTypeUnderShiftClass
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

        private string client1;
        public string Client1
        {
            get { return client1; }
            set { client1 = value; }
        }

        private string client2;
        public string Client2
        {
            get { return client2; }
            set { client2 = value; }
        }

        private string _isMyOneMarket;
        public string IsMyOneMarket 
        {
            get { return _isMyOneMarket; }
            set { _isMyOneMarket = value; }
        }

        private HVBUnderlyingTypes hvbMassUnderlyingTypes;

        public HVBUnderlyingTypes HvbMassUnderlyingTypes
        {
            get { return hvbMassUnderlyingTypes; }
            set { hvbMassUnderlyingTypes = value; }
        }

        private Underlying underlying;

        public Underlying Underlying
        {
            get { return underlying; }
            set { underlying = value; }
        }
    }
}
