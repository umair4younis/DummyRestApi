using System;

namespace Puma.MDE.Data
{
    /// <summary>
    /// This Class stores the base values of a Manast/Sophis movement
    /// </summary>
    public class SwapMovement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SophisMovement"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="SICOVAM">The SICOVAM.</param>
        /// <param name="sophisReference">The sophis reference.</param>
        /// <param name="TradeDate">The trade date.</param>
        /// <param name="ValueDate">The value date.</param>
        /// <param name="contractSize">Size of the contract.</param>
        /// <param name="Nominal">The nominal.</param>
        /// <param name="Price">The price.</param>
        /// <param name="Accrued">The accrued.</param>
        /// <param name="Currency">The currency.</param>
        /// <param name="FXRate">The FX rate.</param>
        /// <param name="Amount">The amount.</param>
        /// <param name="CounterPartyFee">The counter party fee.</param>
        /// <param name="GlobalBrokerFee">The global broker fee.</param>
        /// <param name="GlobalMarketFee">The global market fee.</param>
        /// <param name="Info">The info.</param>
        /// <param name="ReferenceTrader">The reference trader.</param>
        public SwapMovement(TypeSophisMovement type, int SICOVAM, string sophisReference, DateTime TradeDate, DateTime ValueDate, double contractSize, double Nominal, double Price, double Accrued, string Currency, double FXRate, double Amount, double CounterPartyFee, double GlobalBrokerFee, double GlobalMarketFee, string Info, string ReferenceTrader, TypeManstInstrument InstrumentType)
        {
            this.m_type = type;
            this.m_SICOVAM = SICOVAM;
            this.m_sophisReference = sophisReference;
            this.m_tradeDate = TradeDate;
            this.m_valueDate = ValueDate;
            this.m_contractSize = contractSize;
            this.m_nominal = Nominal;
            this.m_price = Price;
            this.m_accrued = Accrued;
            this.m_currency = Currency;
            this.m_fxRate = FXRate;
            this.m_amount = Amount;
            this.m_counterPartyFee = CounterPartyFee;
            this.m_globalBrokerFee = GlobalBrokerFee;
            this.m_globalMarketFee = GlobalMarketFee;
            this.m_info = Info;
            this.m_referenceTrader = ReferenceTrader;
            this.m_instrumentType = InstrumentType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Movement"/> class.
        /// </summary>
        /// <param name="tr">The trade row.</param>
        public SwapMovement(SwapTrade tr)
        {
            this.m_type = TypeSophisMovement.PurchaseOrSale;
            this.m_SICOVAM = Int32.Parse(tr.Instrument.SICOVAM);
            this.m_sophisReference = tr.Instrument.RIC;
            this.m_tradeDate = tr.Order.TradeDate;
            this.m_valueDate = tr.Order.ValueDate;


            this.m_nominal = tr.Nominal;

            if (tr.Instrument.InstrumentType.IsBond)
                this.m_nominal /= 100.0;

            this.m_price = tr.Price;
            this.m_accrued = tr.Accrued;
            this.m_currency = tr.Currency;
            this.m_fxRate = tr.FXRate;
            this.m_counterPartyFee = 0.0;
            this.m_globalBrokerFee = tr.Fee;
            this.m_globalMarketFee = 0.0;
            this.m_amount = tr.Net;
            this.m_info = tr.Order.Description;
            this.m_referenceTrader = string.Empty;
            this.m_instrumentType = TypeManstInstrument.Unknown; // use "tr.InstrumentRow.InstrumentTypeRow.Name" instead ?
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Movement"/> class.
        /// </summary>
        /// <param name="er">The event row.</param>
        public SwapMovement(SwapEvent er)
        {
            this.m_type = TypeSophisMovement.Coupon; // since there isn't 
            this.m_SICOVAM = Int32.Parse(er.PortfolioRowByPortfolioSource.Instrument.SICOVAM);
            this.m_sophisReference = er.PortfolioRowByPortfolioSource.Instrument.RIC;
            this.m_tradeDate = er.ExecutionDate;
            this.m_valueDate = er.ExecutionDate;
            this.m_contractSize = 1.0; // an event has always a conctract size of 1
            this.m_nominal = er.Nominal;
            this.m_price = 1.0;
            this.m_accrued = 0.0;
            this.m_currency = er.Currency;
            this.m_fxRate = er.FXRate;
            this.m_amount = er.Nominal;
            this.m_counterPartyFee = 0.0;
            this.m_globalBrokerFee = 0.0;
            this.m_globalMarketFee = 0.0;
            this.m_info = er.Description;
            this.m_referenceTrader = string.Empty;
            this.m_instrumentType = TypeManstInstrument.Unknown; // use "er.PortfolioRowByPortfolio_Source_Link.InstrumentRow.InstrumentTypeRow.Name" instead ?
        }

        TypeSophisMovement m_type;
        private int m_SICOVAM;
        private string m_sophisReference;
        private DateTime m_tradeDate;
        private DateTime m_valueDate;
        private double m_contractSize = 1.0;
        private double m_nominal = double.NaN;
        private double m_price = double.NaN;
        private double m_accrued = double.NaN;
        private string m_currency = string.Empty;
        private double m_fxRate = double.NaN;
        private double m_amount = double.NaN;
        private double m_counterPartyFee = double.NaN;
        private double m_globalBrokerFee = double.NaN;
        private double m_globalMarketFee = double.NaN;
        private string m_info = string.Empty;
        private bool m_match = false;
        private string m_matchInfo = string.Empty;
        private string m_referenceTrader = string.Empty;
        private TypeManstInstrument m_instrumentType = TypeManstInstrument.Unknown;

        public TypeSophisMovement TypeEnum { get { return m_type; } set { m_type = value; } }
        public string Type { get { return m_type.ToString(); } set { m_type = (TypeSophisMovement)Enum.Parse(typeof(TypeSophisMovement), value); } }

        public int SICOVAM { get { return m_SICOVAM; } }
        public string SophisReference { get { return m_sophisReference; } }
        public DateTime TradeDate { get { return m_tradeDate; } }
        public DateTime ValueDate { get { return m_valueDate; } }
        public double Nominal { get { return m_nominal; } }
        public double Price { get { return m_price; } }
        public double Accrued { get { return m_accrued; } }
        public string Currency { get { return m_currency; } }
        public double ContractSize { get { return m_contractSize; } }
        public double FXRate { get { return m_fxRate; } }
        public double Amount { get { return m_amount; } }
        public double CounterPartyFee { get { return m_counterPartyFee; } }
        public double GlobalBrokerFee { get { return m_globalBrokerFee; } }
        public double GlobalMarketFee { get { return m_globalMarketFee; } }
        public string Info { get { return m_info; } }
        public string ReferenceTrader { get { return m_referenceTrader; } }
        public TypeManstInstrument InstrumentType { get { return m_instrumentType; } }

        public bool Match { get { return m_match; } set { m_match = value; } }
        public string MatchInfo { get { return m_matchInfo; } set { m_matchInfo = value; } }

        // used as value comparison in the tools crosscheck function: check nominal, price, accrued, fx rate
        public double CheckValue { get { return Math.Round(m_nominal * m_price / m_fxRate + Accrued / m_fxRate, 2); } }

        public double TotalFee { get { return m_counterPartyFee + m_globalBrokerFee + m_globalMarketFee; } }
        public double AccruedFX { get { return m_accrued / m_fxRate; } }
        public double ValueFX { get { return m_nominal * m_price / m_fxRate; } }

        public double Gross { get { return ValueFX + AccruedFX * Math.Sign(Nominal); } }
        //public double Gross2 { get { return Math.Abs(ValueFX) + AccruedFX * Math.Sign(Nominal); } }
        public double Net { get { return ValueFX + TotalFee + AccruedFX * Math.Sign(Nominal); } }
        public double Net2
        {
            get
            {
                if (m_nominal == 0.0 || m_price == 0.0)
                    return 0.0;
                else
                    return ValueFX + m_accrued / m_fxRate;
            }
        }
    }

}
