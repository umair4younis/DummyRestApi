using System;

namespace Puma.MDE.Data
{
    public class MDESettingsMapping
    {
        private string reference;

        public string Reference
        {
            get { return reference; }
            set { reference = value; }
        }
        private double tradeTimeFrom;

        public double TradeTimeFrom
        {
            get { return tradeTimeFrom; }
            set { tradeTimeFrom = value; }
        }
        private double tradeTimeTo;

        public double TradeTimeTo
        {
            get { return tradeTimeTo; }
            set { tradeTimeTo = value; }
        }
        private double indicTimeTo;

        public double IndicTimeTo
        {
            get { return indicTimeTo; }
            set { indicTimeTo = value; }
        }
        private double indicTimeFrom;

        public double IndicTimeFrom
        {
            get { return indicTimeFrom; }
            set { indicTimeFrom = value; }
        }

        private string mCA;

        public string MCA
        {
            get { return mCA; }
            set { mCA = value; }
        }
        private string approvedByFO;

        public string ApprovedByFO
        {
            get { return approvedByFO; }
            set { approvedByFO = value; }
        }
        private string approvedByBO;

        public string ApprovedByBO
        {
            get { return approvedByBO; }
            set { approvedByBO = value; }
        }
        private string approvedByFOMulti;

        public string ApprovedByFOMulti
        {
            get { return approvedByFOMulti; }
            set { approvedByFOMulti = value; }
        }

        public DateTime IndicTimeFromDisplay { get; set; }
        public DateTime IndicTimeToDisplay { get; set; }
        public DateTime TradeTimeToDisplay { get; set; }
        public DateTime TradeTimeFromDisplay { get; set; }
    }
}
