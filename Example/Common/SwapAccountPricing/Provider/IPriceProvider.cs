using System;
using System.Collections.Generic;
using Puma.MDE.Data;

namespace Puma.MDE.SwapAccountPricing.Provider
{
    class KxConnection
    {
        public KxConnection(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; set; }
        public int Port { get; set; }
    }

    public class SubscribedInstrument
    {
        public SubscribedInstrument(InstrumentRowSnapshot ir, DateTime? closeDate)
        {
            mRIC = ir.RIC;
            int.TryParse(ir.SICOVAM, out mSicovam);
            mInstrumentType = TypeManstInstrument.Cash;
            if (closeDate.HasValue)
                mCloseDate = closeDate.Value;
        }

        public override string ToString()
        {
            return string.Format("(ric: {0}, sicovam: {1}, type: {2})", mRIC, mSicovam, mInstrumentType);
        }

        public string mRIC;
        public int mSicovam;
        public TypeManstInstrument mInstrumentType;
        public DateTime? mCloseDate = null;
    }

    public class RealTimePrices // stores last, ask, bid
    {
        public RealTimePrices()
        {
            int count = TypeManastPriceField.eRealTimeBid - TypeManastPriceField.eRealTimeLast + 1; // 3
            mPrices = new List<Price>(count);
            for (TypeManastPriceField e = TypeManastPriceField.eRealTimeLast; e <= TypeManastPriceField.eRealTimeBid; e++)
                mPrices.Add(new Price()); // NaN
        }
        public List<Price> mPrices;
    }

    public class ClosePrices // stores close, fixing1, nav
    {
        public ClosePrices()
        {
            int count = TypeManastPriceField.eNAV - TypeManastPriceField.eClose + 1; // 3
            mPrices = new List<Price>(count);
            for (TypeManastPriceField e = TypeManastPriceField.eClose; e <= TypeManastPriceField.eNAV; e++)
                mPrices.Add(new Price()); // NaN
        }
        public List<Price> mPrices;
    }

    public class ClosePricesHistory
    {
        public ClosePricesHistory()
        {
            mHistory = new List<KeyValuePair<DateTime, ClosePrices>>();
            mStartdate = DateTime.MinValue;
        }
        public List<KeyValuePair<DateTime, ClosePrices>> mHistory; // close prices order by date descendingly
        public DateTime mStartdate; // date passed to SQL query on HISTORIQUE, is always <= "mHistory.Last().Key"
    }

    public interface IPriceProvider : IDisposable
	{
        string ProviderName();
        string GetConnectionInfo();
        void Start();
        void Stop();
        void Suspend();
        void Resume();

        Price GetPrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, bool wait);
        double GetDelta(string instrumentSicovam, string underlyingSicovam);
        double GetGamma(string instrumentSicovam, string underlyingSicovam); 
        void ReloadClosings();
        Price GetFXRate(string ccy1, string ccy2, bool useWMFixing, SPriceRequest priceRequest, bool wait);
        double GetAccrued(DateTime calcDate, string sicovam);
        double GetBdCpnValue(DateTime calcDate, string sicovam);
        bool GetCpnNext(DateTime calcDate, string sicovam, out DateTime exDate, out DateTime payDate);
	}
}
