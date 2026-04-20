namespace Puma.MDE.Data
{
    public class HVBMassMarkups
    {
        private long id;

        public long Id
        {
            get { return id; }
            set { id = value; }
        }

        private int clientID;

        public int ClientID
        {
            get { return clientID; }
            set { clientID = value; }
        }
        private int underlyingTypeID;

        public int UnderlyingTypeID
        {
            get { return underlyingTypeID; }
            set { underlyingTypeID = value; }
        }
        private string maturity;

        public string Maturity
        {
            get { return maturity; }
            set { maturity = value; }
        }
        private string markup;

        public string Markup
        {
            get { return markup; }
            set { markup = value; }
        }

        public bool IsDirty { get; set; }

        public override string ToString()
        {
            return Maturity + "\t" + Markup;
        }

        public HVBMassMarkups Clone()
        {
            HVBMassMarkups retval = new HVBMassMarkups();

            retval.Id               = Id;
            retval.ClientID         = ClientID;
            retval.UnderlyingTypeID = UnderlyingTypeID;
            retval.Maturity         = Maturity;
            retval.Markup           = Markup;
            retval.IsDirty          = IsDirty;

            return retval;
        }
    }
}
