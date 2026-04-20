using System;
using System.Collections.Generic;

namespace Puma.MDE.Data
{
    [Serializable]
    public class EQBrainTrade
    {
        [Serializable]
        public class EQBrainTradeId
        {
            public long TradeTag { get; protected set; }
            public int DbId { get; protected set; }

            protected EQBrainTradeId() {}

            public EQBrainTradeId(long _TradeTag, int _DbId)
            {
                TradeTag = _TradeTag;
                DbId = _DbId;
            }

            public override bool Equals(object obj)
            {
                EQBrainTrade tradeObj = obj as EQBrainTrade;
                if (tradeObj == null)
                    return false;

                return (tradeObj.Id.TradeTag == TradeTag) && (tradeObj.Id.DbId == DbId);
            }

            public override Int32 GetHashCode()
            {
                return TradeTag.GetHashCode() ^ DbId;
            }
        }

        public EQBrainTradeId Id { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public string Isin { get; protected set; }
        public string Kind { get; protected set; }
        public string Underlying { get; protected set; }
        public string Description { get; protected set; }

        double _Price;
        public double Price { get { return _Price; } protected set { _Price = Math.Round(value, 6); } }

        public long Volume { get; protected set; }
        public string BuySell { get; protected set; }
        public string Currency { get; protected set; }
        public string Market { get; protected set; }
        public string Counterpart { get; protected set; }
        public string Portfolio { get; protected set; }

        double? _Strike = null;
        public double? Strike { get { return _Strike; } protected set { _Strike = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _Barrier = null;
        public double? Barrier { get { return _Barrier; } protected set { _Barrier = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        public long? NetPos { get; protected set; }

        public EQBrainAnalysis Analysis { get; protected set; }

        protected EQBrainTrade() {}

        public EQBrainTrade(long tradeTag, 
            int DbId,
            DateTime _timestamp,
            string _isin,
            string _kind,
            string _underlying,
            string _description,
            double _price,
            long _volume,
            string _buySell,
            string _currency,
            string _market,
            string _counterpart,
            double? _strike,
            double? _barrier,
            long? _netPos,
            string _portfolio,
            EQBrainAnalysis _analysis)
        {
            Id = new EQBrainTradeId(tradeTag, DbId);

            Timestamp = _timestamp;
            Isin = _isin;
            Kind = _kind;
            Underlying = _underlying;
            Description = _description;
            Price = _price;
            Volume = _volume;
            BuySell = _buySell;
            Currency = _currency;
            Market = _market;
            Counterpart = _counterpart;
            Strike = _strike;
            Barrier = _barrier;
            NetPos = _netPos;
            Portfolio = _portfolio;
            Analysis = _analysis;
        }
    }

    public class EQBrainAnalysis
    {
        public long Id { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public string Isin { get; protected set; }

        double? _SophisFV = null;
        public double? SophisFV { get { return _SophisFV; } protected set { _SophisFV = value.HasValue ? Math.Round(value.Value, 6) : value; } }
        
        double? _SophisDelta = null;
        public double? SophisDelta { get { return _SophisDelta; } protected set { _SophisDelta = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _SophisSpot = null;
        public double? SophisSpot { get { return _SophisSpot; } protected set { _SophisSpot = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _SophisFwd = null;
        public double? SophisFwd { get { return _SophisFwd; } protected set { _SophisFwd = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _SophisVol = null;
        public double? SophisVol { get { return _SophisVol; } protected set { _SophisVol = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _SophisFxEur = null;
        public double? SophisFxEur { get { return _SophisFxEur; } protected set { _SophisFxEur = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteAsk = null;
        public double? QuoteAsk { get { return _QuoteAsk; } protected set { _QuoteAsk = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteBid = null;
        public double? QuoteBid { get { return _QuoteBid; } protected set { _QuoteBid = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteFV = null;
        public double? QuoteFV { get { return _QuoteFV; } protected set { _QuoteFV = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteDelta = null;
        public double? QuoteDelta { get { return _QuoteDelta; } protected set { _QuoteDelta = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteVega = null;
        public double? QuoteVega { get { return _QuoteVega; } protected set { _QuoteVega = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteTheta = null;
        public double? QuoteTheta { get { return _QuoteTheta; } protected set { _QuoteTheta = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteSpot = null;
        public double? QuoteSpot { get { return _QuoteSpot; } protected set { _QuoteSpot = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteFwd = null;
        public double? QuoteFwd { get { return _QuoteFwd; } protected set { _QuoteFwd = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteVol = null;
        public double? QuoteVol { get { return _QuoteVol; } protected set { _QuoteVol = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteAskVol = null;
        public double? QuoteAskVol { get { return _QuoteAskVol; } protected set { _QuoteAskVol = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _QuoteBidVol = null;
        public double? QuoteBidVol { get { return _QuoteBidVol; } protected set { _QuoteBidVol = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        int? _SophisStatus = null;
        public int? SophisStatus { get { return _SophisStatus; } protected set { _SophisStatus = value; } }

        int? _QuoteStatus = null;
        public int? QuoteStatus { get { return _QuoteStatus; } protected set { _QuoteStatus = value; } }

        double? _MarginDistribution = null;
        public double? MarginDistribution { get { return _MarginDistribution; } protected set { _MarginDistribution = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _MarginCreditSpreadAdj = null;
        public double? MarginCreditSpreadAdj { get { return _MarginCreditSpreadAdj; } protected set { _MarginCreditSpreadAdj = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _TotMarkup = null;
        public double? TotMarkup { get { return _TotMarkup; } protected set { _TotMarkup = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        public string OneSide { get; protected set; }

        double? _SoldOut = null;
        public double? SoldOut { get { return _SoldOut; } protected set { _SoldOut = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _ConstVolaMarkup = null;
        public double? ConstVolaMarkup { get { return _ConstVolaMarkup; } protected set { _ConstVolaMarkup = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        public DateTime? TradingSD { get; protected set; }

        public DateTime? TradingED { get; protected set; }

        double? _MaxHedgeEquity = null;
        public double? MaxHedgeEquity { get { return _MaxHedgeEquity; } protected set { _MaxHedgeEquity = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        protected IList<EQBrainAnalysisQuote> _Quotes = new List<EQBrainAnalysisQuote>();
        public IList<EQBrainAnalysisQuote> Quotes { get { return _Quotes; } protected set { _Quotes = value; } }

        protected IList<EQBrainTrade> _Trade = new List<EQBrainTrade>();
        public IList<EQBrainTrade> Trade { get { return _Trade; } protected set { _Trade = value; } }

        protected IList<EQBrainAnalysisRequest> _Request = new List<EQBrainAnalysisRequest>();
        public IList<EQBrainAnalysisRequest> Request { get { return _Request; } protected set { _Request = value; } }

        protected EQBrainAnalysis() { }

        public EQBrainAnalysis(DateTime _Timestamp, 
            string _Isin,
            double? _QuoteDelta,
            double? _QuoteVega,
            double? _QuoteTheta,
            double? _QuoteSpot,
            double? _QuoteFwd,
            double? _QuoteVol,
            double? _QuoteAskVol,
            double? _QuoteBidVol)
        {
            Init(_Timestamp,
                _Isin,
                _QuoteDelta,
                _QuoteVega,
                _QuoteTheta,
                _QuoteSpot,
                _QuoteFwd,
                _QuoteVol,
                _QuoteAskVol,
                _QuoteBidVol);
        }

        public void Init(DateTime _Timestamp,
            string _Isin,
            double? _QuoteDelta,
            double? _QuoteVega,
            double? _QuoteTheta,
            double? _QuoteSpot,
            double? _QuoteFwd,
            double? _QuoteVol,
            double? _QuoteAskVol,
            double? _QuoteBidVol)
        {
            Timestamp = _Timestamp;
            Isin = _Isin;
            QuoteDelta = _QuoteDelta;
            QuoteVega = _QuoteVega;
            QuoteTheta = _QuoteTheta;
            QuoteSpot = _QuoteSpot;
            QuoteFwd = _QuoteFwd;
            QuoteVol = _QuoteVol;
            QuoteAskVol = _QuoteAskVol;
            QuoteBidVol = _QuoteBidVol;
        }

        public void AddOrcValues(double? _QuoteFV,
            double? _QuoteAsk,
            double? _QuoteBid,
            bool _QuoteStatus)
        {
            QuoteFV = _QuoteFV;
            QuoteAsk = _QuoteAsk;
            QuoteBid = _QuoteBid;
            QuoteStatus = _QuoteStatus ? 1 : 0;
        }

        public void AddLiqValues(double? _MarginDistribution,
            double? _MarginCreditSpreadAdj,
            double? _TotMarkup,
            string _OneSide,
            double? _SoldOut,
            double? _ConstVolaMarkup,
            DateTime? _TradingSd,
            DateTime? _TradingEd,
            double? _MaxHedgeEquity)
        {
            MarginDistribution = _MarginDistribution;
            MarginCreditSpreadAdj = _MarginCreditSpreadAdj;
            TotMarkup = _TotMarkup;
            OneSide = _OneSide;
            SoldOut = _SoldOut;
            ConstVolaMarkup = _ConstVolaMarkup;
            TradingSD = _TradingSd;
            TradingED = _TradingEd;
            MaxHedgeEquity = _MaxHedgeEquity;
        }

        public void AddSophisValues(DateTime _Timestamp,
            double? _SophisFV,
            double? _SophisDelta,
            double? _SophisSpot,
            double? _SophisFwd,
            double? _SophisVol,
            double? _SophisFxEur,
            bool _SophisStatus)
        {
            Timestamp = _Timestamp;
            SophisFV = _SophisFV;
            SophisDelta = _SophisDelta;
            SophisSpot = _SophisSpot;
            SophisFwd = _SophisFwd;
            SophisVol = _SophisVol;
            SophisFxEur = _SophisFxEur;
            SophisStatus = _SophisStatus ? 1 : 0;
        }
    }

    public class EQBrainAnalysisQuote
    {
        public long Id { get; protected set; }
        public DateTime Timestamp { get; protected set; }
        public string Isin { get; protected set; }
        public string Issuer { get; protected set; }

        double? _Ratio = null;
        public double? Ratio { get { return _Ratio; } protected set { _Ratio = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _Ask = null;
        public double? Ask { get { return _Ask; } protected set { _Ask = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _AskAdj = null;
        public double? AskAdj { get { return _AskAdj; } protected set { _AskAdj = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _AskVol = null;
        public double? AskVol { get { return _AskVol; } protected set { _AskVol = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _AskVolAdj = null;
        public double? AskVolAdj { get { return _AskVolAdj; } protected set { _AskVolAdj = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _Bid = null;
        public double? Bid { get { return _Bid; } protected set { _Bid = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _BidAdj = null;
        public double? BidAdj { get { return _BidAdj; } protected set { _BidAdj = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _BidVol = null;
        public double? BidVol { get { return _BidVol; } protected set { _BidVol = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        double? _BidVolAdj = null;
        public double? BidVolAdj { get { return _BidVolAdj; } protected set { _BidVolAdj = value.HasValue ? Math.Round(value.Value, 6) : value; } }

        public EQBrainAnalysis Analysis { get; protected set; }

        protected EQBrainAnalysisQuote() { }

        public EQBrainAnalysisQuote(DateTime _Timestamp,
            string _Isin,
            string _Issuer,
            double? __Ratio,
            double? __Ask,
            double? __AskAdj,
            double? __AskVol,
            double? __AskVolAdj,
            double? __Bid,
            double? __BidAdj,
            double? __BidVol,
            double? __BidVolAdj,
            EQBrainAnalysis _Analysis)
        {
            Timestamp = _Timestamp;
            Isin = _Isin;
            Issuer = _Issuer;
            Ratio = __Ratio;
            Ask = __Ask;
            AskAdj = __AskAdj;
            AskVol = __AskVol;
            AskVolAdj = __AskVolAdj;
            Bid = __Bid;
            BidAdj = __BidAdj;
            BidVol = __BidVol;
            Analysis = _Analysis;
            BidVolAdj = __BidVolAdj;
        }
    }

    public class EQBrainAnalysisRequest
    {
        public long Id { get; protected set; }
        public DateTime Timestamp { get; set; }
        public string Isin { get; set; }
        public string Kind { get; set; }
        public string Underlying { get; set; }
        public string Requestor { get; protected set; }
        public string Status { get; set; }
        public long DbId { get; set; }
        public EQBrainAnalysis Analysis { get; set; }

        public EQBrainAnalysisRequest Truncate(EQBrainAnalysis analysis)
        {
            EQBrainAnalysisRequest copy = new EQBrainAnalysisRequest();
            copy.Id = Id;
            copy.Timestamp = Timestamp;
            copy.Isin = Isin;
            copy.Kind = Kind;
            copy.Underlying = Underlying;
            copy.Requestor = Requestor;
            copy.Status = Status;
            copy.DbId = DbId;
            copy.Analysis = analysis;

            return copy;
        }
    }
}
