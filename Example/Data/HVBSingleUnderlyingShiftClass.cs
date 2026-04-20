namespace Puma.MDE.Data
{
    public class HVBSingleUnderlyingShiftClass
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

        private HVBMassShiftClass hvbMassShiftClass;

        public HVBMassShiftClass HvbMassShiftClass
        {
            get { return hvbMassShiftClass; }
            set { hvbMassShiftClass = value; }
        }
        private HVBTitresUnderShiftClass hvbTitresUnderShiftClass;

        public HVBTitresUnderShiftClass HvbTitresUnderShiftClass
        {
            get { return hvbTitresUnderShiftClass; }
            set { hvbTitresUnderShiftClass = value; }
        }

    }
}
