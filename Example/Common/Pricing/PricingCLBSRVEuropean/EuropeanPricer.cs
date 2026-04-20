using System;
using Puma.MDE.Common.Configuration;
using Puma.MDE.Data;

namespace Puma.MDE.Pricing.PricingCLBSRVEuropean
{
    [System.Runtime.InteropServices.ComVisible(false)]
    public class EuropeanPricer : IPricer
    {
        public void Config(IConfig settings)
        {
        }

        public double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, object market, object quotemarketdata)
        {
            return GetImpliedVolatility(reference, price, strike, iscall, undl, Expiry, Engine.Instance.Today, market, quotemarketdata);
        }

        public double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, object market, object quotemarketdata)
        {
            int mode = iscall ? 0 : 1;

            double maturity = 1;
            double forward;
            double df;
            if (true && quotemarketdata != null)
            {
                df = 1;
                forward = 1;
            }
            else
            {
                // discount factor for option expiry -> OIS if selected on underlying level
                df = 1;
                forward = 1;
            }

            // discount factor for deferred payment date, in Sophis always default curve so we do the same here
            price *= 1; 

            return price;
        }

        public double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, IMarketData market, object quotemarketdata)
        {
            throw new NotImplementedException();
        }

        public double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, IMarketData market, object quotemarketdata)
        {
            throw new NotImplementedException();
        }

        public double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, object market, object quotemarketdata)
        {
            return GetTheoreticalValue(reference, volatility, strike, iscall, undl, Expiry, Engine.Instance.Today,  market, quotemarketdata); 
        }

        public double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, object market, object quotemarketdata)
        {
            double maturity = 1;
            double forward;
            double df;
            if (true && quotemarketdata != null)
            {
                df = 1;
                forward = 1;
            }
            else
            {
                // discount factor for option expiry -> OIS if selected on underlying level
                df = 1;
                forward =1;
            }

            if (iscall)
                // discount factor for deferred payment date, in Sophis always default curve so we do the same here
                return reference * 1;
            else
                // discount factor for deferred payment date, in Sophis always default curve so we do the same here
                return reference * 1;
        }

        public double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, IMarketData market, object quotemarketdata)
        {
            throw new NotImplementedException();
        }

        public double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, IMarketData market, object quotemarketdata)
        {
            throw new NotImplementedException();
        }
    }
}
