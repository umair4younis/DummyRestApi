using System;
using System.Runtime.InteropServices;
using Puma.MDE.Common.Configuration;
using Puma.MDE.Data;

namespace Puma.MDE.Pricing
{
    [Guid("BCFA8E78-8ED3-45ca-B777-DE4D943DC148")]
    public interface IPricer
    {
        [ComVisible(false)]
        void Config(IConfig settings);
        double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, IMarketData market, object quotemarketdata);
        double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, IMarketData market, object quotemarketdata);
        double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, IMarketData market, object quotemarketdata);
        double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, IMarketData market, object quotemarketdata);
    }
}
