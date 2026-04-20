using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Puma.MDE.Data
{
    [ComVisible(true)]
    [Serializable]
    public class SwapTrade : INotifyPropertyChanged
    {
        public SwapTrade() { this.Id = 1; }
        public SwapTrade(int id) { Id = id; }
        public SwapTrade(SwapOrder order) : this(TypeManastTradeSide.Buy, "", "", "", 0, 0, 0, 0, "", 0, 0, "", 0, "", 0, 0, "", "", true)
        {
            this.Id = 1;
            this.Order = order;
            this.DbName = "";
        }
        public SwapTrade(TypeManastTradeSide side, string name, string ric, string isin, double size, double price, double brokerFee, double HVBFee, string currency, double fxRate, double accrued, string comment, double contractSize, string instrumentTypeName, double targetWeight, double targetNominal, string bbg, string wkn, bool pool)
        {
            this.Id = 1;
            m_side = side;
            m_name = name;
            xInstrumentRic = ric;
            xInstrumentIsin = isin;
            m_nominal = size;
            Price = price;
            m_brokerFee = brokerFee;
            m_HVBFee = HVBFee;
            Currency = currency;
            m_fxRate = fxRate;
            m_accrued = accrued;
            Description = comment;
            TargetWeight = targetWeight;
            m_TargetNominal = targetNominal;
            Pool = pool;
        }


        public int Id { get; protected set; }
        public String DbName { get; set; }
        public int OrderId { get; set; }

        [JsonIgnore]
        public SwapOrder Order
        {
            get
            {
                return null;
            }
            set
            {
                this.OrderId = value.Id;
                NotifyChange();
            }
        }
        public int? InstrumentId { get; set; }

        [JsonIgnore]
        public SwapAccountInstrument Instrument
        {
            get
            {
                if (InstrumentId.HasValue)
                {
                    return new SwapAccountInstrument();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    InstrumentId = null;
                }
                else
                {
                    InstrumentId = value.Id;
                }
                NotifyChange();
            }
        }

        public String Description { get; set; }

        private double m_nominal;
        public double Nominal
        {
            get => m_nominal; set
            {
                m_nominal = value;
                NotifyChange();
            }

        }
        public double Price { get; set; }

        private double m_accrued;
        public double Accrued
        {
            get => m_accrued;
            set
            {
                m_accrued = value;
                NotifyChange();
            }
        }
        public double Fee { get; set; }
        public String Currency { get; set; }

        private double m_fxRate;
        public double FXRate
        {
            get => m_fxRate; set
            {
                m_fxRate = value;
                NotifyChange();
            }
        }

        public bool FXRateIsSet { get { return ((FXRate != 1.0) && (FXRate != 0.0)); } }

        public double TargetWeight { get; set; }
        public int RowVersion { get; set; }
        public bool Pool { get; set; }

        private TypeManastTradeSide m_side = TypeManastTradeSide.Buy;
        public TypeManastTradeSide Side { get { return this.m_side; } set { this.m_side = value; NotifyChange(); } }
        public string TradeSide
        {
            get { return (this.m_side == TypeManastTradeSide.Buy) ? "Buy" : "Sell"; }
            set
            {
                this.m_side = (value == "Buy") ? TypeManastTradeSide.Buy : TypeManastTradeSide.Sell;
                NotifyChange();
            }
        }

        // additional
        private string m_name;
        private bool m_match = false;
        private double m_TargetNominal;
        private double m_brokerFee;
        private double m_HVBFee;
        private double m_ContractSize;

        public double AccruedFX { get { return Accrued / FXRate; } }
        public double Sign { get { return (TradeSide == TypeManastTradeSide.Buy.ToString() ? +1.0 : -1.0); } }
        public string Name { get { return this.m_name; } set { this.m_name = value; } }
        public double ValueFX
        {
            get
            {
                if (Instrument == null || Instrument.InstrumentType == null) return double.NaN;
                else return Nominal * Price * Instrument.ContractSize / ((Instrument.InstrumentType.IsBond) ? 100.0 : 1.0) / FXRate;
            }
        }
        public double Gross { get { return Math.Abs(ValueFX) + AccruedFX * Math.Sign(Nominal); } }
        public double Net { get { return ValueFX + Fee + AccruedFX * Math.Sign(Nominal); } }

        public bool Match { get { return m_match; } set { m_match = value; NotifyChange(); } }
        public double TargetNominal { get { return this.m_TargetNominal; } set { this.m_TargetNominal = value; NotifyChange(); } }
        public double ContractSize { get { return this.m_ContractSize; } set { this.m_ContractSize = value; NotifyChange(); } }
        public double BrokerFee
        {
            get { return this.m_brokerFee; }
            set
            {
                this.m_brokerFee = value;
                NotifyChange();
            }
        }
        public double HVBFee
        {
            get { return this.m_HVBFee; }
            set
            {
                this.m_HVBFee = value;
                NotifyChange();
            }
        }
        public double TotalFee { get { return this.m_HVBFee + this.m_brokerFee; } }

        // Getters and Setters for creation of trade based on single column entry for Instrument

        public string xInstrumentName
        {
            get { return Instrument != null ? Instrument.InstrumentName : string.Empty; }
            set
            {
                string tempValue = value;
                if (tempValue != null && tempValue != string.Empty)
                {
                    SwapAccountInstrument tempInstrument = new SwapAccountInstrument();
                    if (tempInstrument != null)
                    {
                        Instrument = tempInstrument;
                        NotifyChange();
                    }
                }
            }
        }
        public string xInstrumentRic
        {
            get { return Instrument != null ? Instrument.RIC : string.Empty; }
            set
            {
                string tempValue = value;
                if (tempValue != null && tempValue != string.Empty)
                {
                    SwapAccountInstrument tempInstrument = new SwapAccountInstrument();
                    if (tempInstrument != null)
                    {
                        Instrument = tempInstrument;
                        NotifyChange();
                    }
                }
            }
        }


        public string xInstrumentType { get { return Instrument != null && Instrument.InstrumentType != null ? Instrument.InstrumentType.TypeName : string.Empty; } }
        public string xInstrumentIsin
        {
            get { return Instrument != null ? Instrument.ISIN : string.Empty; }
            set
            {
                string tempValue = value;
                if (tempValue != null && tempValue != string.Empty)
                {
                    SwapAccountInstrument tempInstrument = new SwapAccountInstrument();
                    if (tempInstrument != null)
                    {
                        Instrument = tempInstrument;
                        NotifyChange();
                    }
                }
            }
        }

        public void NotifyChange()
        {
            var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in properties)
            {
                OnPropertyChanged(prop.Name);
            }
        }

        public ObservableCollection<string> InstrumentNamesCollection
        {
            get => null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // used as sum row in Trade details
    public class TotalsRow : INotifyPropertyChanged
    {
        private int _count =0;
        private double _accrued = 0, _fee = 0, _valueFX = 0, _accruedFX = 0, _gross = 0, _net = 0;

        public int Count { get => _count; set { if (_count != value) { _count = value; OnPropertyChanged(nameof(Count)); } } }
        public double Accrued { get => _accrued; set { if (_accrued != value) { _accrued = value; OnPropertyChanged(nameof(Accrued)); } } }
        public double Fee { get => _fee; set { if (_fee != value) { _fee = value; OnPropertyChanged(nameof(Fee)); } } }
        public double ValueFX { get => _valueFX; set { if (_valueFX != value) { _valueFX = value; OnPropertyChanged(nameof(ValueFX)); } } }
        public double AccruedFX { get => _accruedFX; set { if (_accruedFX != value) { _accruedFX = value; OnPropertyChanged(nameof(AccruedFX)); } } }
        public double Gross { get => _gross; set { if (_gross != value) { _gross = value; OnPropertyChanged(nameof(Gross)); } } }
        public double Net { get => _net; set { if (_net != value) { _net = value; OnPropertyChanged(nameof(Net)); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
