namespace Puma.MDE.Data
{
    public class HVBMassMUMarkups
    {
        public int Id { get; set; }
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

        private bool isDirty;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        public override string ToString()
        {
            return Maturity + "\t" + Markup;
        }

        public HVBMassMUMarkups Clone()
        {
            HVBMassMUMarkups retval = new HVBMassMUMarkups();

            retval.Id = Id;
            retval.ClientID = ClientID;
            retval.UnderlyingTypeID = UnderlyingTypeID;
            retval.Maturity = Maturity;
            retval.Markup = Markup;
            retval.IsDirty = IsDirty;

            return retval;
        }

    }
}
