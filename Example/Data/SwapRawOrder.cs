using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Puma.MDE.Data
{
    public class SwapRawIndexHistory
    {
        public DateTime Date = DateTime.MinValue;
        public double IndexValue = 0.0;
        public double Certificates = 0.0;
        public double HighWaterMark = 0.0;
        public string Description = string.Empty;
        public double IndexFactor { get { return (Certificates != 0.0) ? (1.0 / Certificates) : (0.0); } }
        public double? CollateralRatio = null;
        public double? Ratio = null;
    }

    /// <summary>
    /// Contains the values of an Excel sheet order
    /// </summary>
    public class SwapRawOrder : INotifyPropertyChanged
    {
        public string IntoXML()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SwapRawOrder));
            System.IO.StringWriter writer = new System.IO.StringWriter();
            serializer.Serialize(writer, this);
            string xml = writer.ToString();
            return xml;
        }
        static public SwapRawOrder FromXML(string xml)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SwapRawOrder));
            SwapRawOrder order = serializer.Deserialize(new System.IO.StringReader(xml)) as SwapRawOrder;
            return order;
        }
        public string AccountName;
        public string Description;
        public TypeManastOrder Type;
        public double IndexValue;
        public DateTime TradeDate;
        public DateTime ValueDate;
        public DateTime TradingInstruction;
        //public List<TradeRow> Trades;
        public ObservableCollection<TradeRow> Trades { get; } = new ObservableCollection<TradeRow>();
        //public ObservableCollection<TradeRow> TradesObservable
        //{
        //    get
        //    {
        //        m_TradesObservable.Clear();
        //        foreach (var row in Trades)
        //        {
        //            m_TradesObservable.Add(row);
        //        }
        //        return m_TradesObservable;
        //    }
        //}

        public double Certificates;
        public double Net()
        {
            double sum = 0.0;
            foreach (TradeRow rotr in Trades)
                sum += rotr.Net;
            return sum;
        }
        public TypeManastUpdatePoolUpon UpdatePoolUpon = TypeManastUpdatePoolUpon.eDoNotUpdate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapRawOrder"/> class.
        /// </summary>
        public SwapRawOrder()
        {
            //Trades = new List<TradeRow>();
            Trades = new ObservableCollection<TradeRow>();
        }

        /// <summary>
        /// Contains the values of a trade
        /// </summary>
		public class TradeRow : INotifyPropertyChanged
        {
            private TypeManastTradeSide m_side;
            private string m_name;
            private string m_description;
            private string m_RIC;
            private string m_RICAppendix;
            private string m_ISIN;
            private double m_nominal; // referred to as SIZE in the Excel sheet
            private double m_price;
            private double m_brokerFee;
            private double m_HVBFee;
            private string m_currency;
            private double m_accrued;
            private double m_fxRate;
            private double m_ContractSize;
            private string m_InstrumentTypeName;
            private double m_TargetWeight;
            private double m_TargetNominal;
            private string m_BBG;
            private string m_WKN;
            private bool m_pool;

            public TypeManastTradeSide Side
            {
                get { return this.m_side; }
                set { this.m_side = value; }
            }
            public string TradeSide
            {
                get { return (this.m_side == TypeManastTradeSide.Buy) ? "Buy" : "Sell"; }
                set { this.m_side = (value == "Buy") ? TypeManastTradeSide.Buy : TypeManastTradeSide.Sell; }
            }
            public double Sign { get { return (TradeSide == TypeManastTradeSide.Buy.ToString() ? +1.0 : -1.0); } }

            private void SetInstrumentProperties(SwapAccountInstrument instr)
            {
                m_name = instr.InstrumentName;
                m_RIC = instr.RIC;
                m_WKN = instr.WKN;
                m_BBG = instr.BBG;
                m_ISIN = instr.ISIN;
                m_InstrumentTypeName = instr.InstrumentType.TypeName;
                m_currency = instr.Currency;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(RIC));
                OnPropertyChanged(nameof(WKN));
                OnPropertyChanged(nameof(BBG));
                OnPropertyChanged(nameof(ISIN));
                OnPropertyChanged(nameof(InstrumentTypeName));
                OnPropertyChanged(nameof(Currency));
                OnPropertyChanged(nameof(ValueFX));
                OnPropertyChanged(nameof(Gross));
                OnPropertyChanged(nameof(Net));
            }
            public string Name
            {
                get { return this.m_name; }
                set
                {
                    SetInstrumentProperties(null);
                }
            }
            public string Description
            {
                get { return this.m_description; }
                set { this.m_description = value; }
            }
            public string RIC
            {
                get { return this.m_RIC; }
                set
                {
                    SetInstrumentProperties(null);
                }
            }
            public string BBG
            {
                get { return this.m_BBG; }
                set { this.m_BBG = value; }
            }
            public string WKN
            {
                get { return this.m_WKN; }
                set { this.m_WKN = value; }
            }
            public string RICAppendix
            {
                get { return this.m_RICAppendix; }
                set { this.m_RICAppendix = value; }
            }
            public string ISIN
            {
                get { return this.m_ISIN; }
                set
                {
                    SetInstrumentProperties(null);
                }
            }
            public double Nominal
            {
                get { return this.m_nominal; }
                set
                {
                    this.m_nominal = value;
                    OnPropertyChanged(nameof(Nominal));

                    OnPropertyChanged(nameof(ValueFX));
                    OnPropertyChanged(nameof(Gross));
                    OnPropertyChanged(nameof(Net));
                }
            }
            public double Price
            {
                get { return this.m_price; }
                set
                {
                    this.m_price = value;
                    OnPropertyChanged(nameof(Price));

                    OnPropertyChanged(nameof(ValueFX));
                    OnPropertyChanged(nameof(Gross));
                    OnPropertyChanged(nameof(Net));
                }
            }
            public double BrokerFee
            {
                get { return this.m_brokerFee; }
                set
                {
                    this.m_brokerFee = value;
                    OnPropertyChanged(nameof(BrokerFee));
                    OnPropertyChanged(nameof(TotalFee));
                    OnPropertyChanged(nameof(Net));
                }
            }
            public double HVBFee
            {
                get { return this.m_HVBFee; }
                set
                {
                    this.m_HVBFee = value;
                    OnPropertyChanged(nameof(HVBFee));
                    OnPropertyChanged(nameof(TotalFee));
                    OnPropertyChanged(nameof(Net));
                }
            }

            // referred to as the sum of broker and HVB fee in the Excel sheet
            public double TotalFee { get { return this.HVBFee + this.BrokerFee; } }
            public string Currency
            {
                get { return this.m_currency; }
                set { this.m_currency = value; }
            }
            public double Accrued
            {
                get { return this.m_accrued; }
                set
                {
                    this.m_accrued = value;
                    OnPropertyChanged(nameof(Accrued));
                    OnPropertyChanged(nameof(AccruedFX));
                    OnPropertyChanged(nameof(ValueFX));
                    OnPropertyChanged(nameof(Gross));
                    OnPropertyChanged(nameof(Net));
                }
            }

            public double AccruedFX { get { return this.Accrued / this.FXRate; } }
            //public double ValueFX { get { return this.m_nominal / ((m_InstrumentTypeName == "Bond") ? 100.0 : 1.0) * this.m_price * this.m_ContractSize / this.m_fxRate * ((this.m_side == TypeManastTradeSide.Buy) ? 1.0 : -1.0); } }
            public double ValueFX { get { return Nominal / ((InstrumentTypeName == "Bond") ? 100.0 : 1.0) * Price * ContractSize / FXRate * Sign; } }

            public bool FXRateIsSet { get { return ((FXRate != 1.0) && (FXRate != 0.0)); } }
            public double FXRate
            {
                get { return this.m_fxRate; }
                set
                {
                    this.m_fxRate = value;
                    OnPropertyChanged(nameof(FXRate));
                    OnPropertyChanged(nameof(FXRateIsSet));
                    OnPropertyChanged(nameof(AccruedFX));
                    OnPropertyChanged(nameof(ValueFX));
                    OnPropertyChanged(nameof(Gross));
                    OnPropertyChanged(nameof(Net));
                }
            }
            public double ContractSize
            {
                get { return this.m_ContractSize; }
                set
                {
                    this.m_ContractSize = value;
                    OnPropertyChanged(nameof(ContractSize));
                    OnPropertyChanged(nameof(ValueFX));
                    OnPropertyChanged(nameof(Gross));
                    OnPropertyChanged(nameof(Net));
                }
            }

            public double Gross { get { return Math.Abs(ValueFX + AccruedFX * Sign); } }
            public double Net { get { return ValueFX + AccruedFX * Sign + TotalFee; } }
            public string InstrumentTypeName
            {
                get { return this.m_InstrumentTypeName; }
                set
                {
                    this.m_InstrumentTypeName = value;
                    OnPropertyChanged(nameof(InstrumentTypeName));
                    OnPropertyChanged(nameof(ValueFX));
                    OnPropertyChanged(nameof(Gross));
                    OnPropertyChanged(nameof(Net));
                }
            }
            public double TargetWeight
            {
                get { return this.m_TargetWeight; }
                set
                {
                    this.m_TargetWeight = value;
                    OnPropertyChanged(nameof(TargetWeight));
                }
            }
            public double TargetNominal
            {
                get { return this.m_TargetNominal; }
                set
                {
                    this.m_TargetNominal = value;
                    OnPropertyChanged(nameof(TargetNominal));
                }
            }
            public bool Pool
            {
                get { return this.m_pool; }
                set
                {
                    this.m_pool = value;
                    OnPropertyChanged(nameof(Pool));
                }
            }

            public TradeRow(TypeManastTradeSide side, string name, string ric, string isin, double size, double price, double brokerFee, double HVBFee, string currency, double fxRate, double accrued, string comment, double contractSize, string instrumentTypeName, double targetWeight, double targetNominal, string bbg, string wkn, bool pool)
            {
                m_side = side;
                m_name = name;
                m_RIC = ric;
                m_ISIN = isin;
                m_nominal = size;
                m_price = price;
                m_brokerFee = brokerFee;
                m_HVBFee = HVBFee;
                m_currency = currency;
                m_fxRate = fxRate;
                m_accrued = accrued;
                m_description = comment;
                m_ContractSize = contractSize;
                m_InstrumentTypeName = instrumentTypeName;
                m_TargetWeight = targetWeight;
                m_TargetNominal = targetNominal;
                m_BBG = bbg;
                m_WKN = wkn;
                m_pool = pool;
            }

            public TradeRow() : this(TypeManastTradeSide.Buy, "", "", "", 0, 0, 0, 0, "", 0, 0, "", 0, "", 0, 0, "", "", true) { }


            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
