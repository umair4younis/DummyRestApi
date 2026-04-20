using Puma.MDE.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NLog;

namespace Puma.MDE.SwapAccountPricing.Provider
{
    public class SophisAPIProvider : IPriceProvider
    {
        [DllImport("ole32.dll")]
        static extern void CoUninitialize();

        private static object s_lock = new object();

        public class PriceWrapper
        {
            public long Sicovam = 0;
            public Price Price = new Price(double.NaN, DateTime.MinValue);
            public SortedList<long, double> Deltas = new SortedList<long, double>();
            public SortedList<long, double> Gammas = new SortedList<long, double>();
            public DateTime Requested = DateTime.MinValue;
        }

        private Dictionary<long, PriceWrapper> m_prices = new Dictionary<long, PriceWrapper>();
        private DateTime m_lastUpdate = DateTime.Now;
        private static Dictionary<long, List<Price>> m_historyCache = new Dictionary<long, List<Price>>();

        private Logger Logger { get { return Engine.Instance.Log; } }

        public string GetConnectionInfo()
        {
            string info = string.Format("Db:{0}");
            return info;
        }

        public SophisAPIProvider(string source)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
        }

        ~SophisAPIProvider()
        {
        }

        public void Start()
        { // not yet implemented
        }

        public void Stop()
        { // not yet implemented
        }

        public void Suspend()
        { // not yet implemented
        }

        public void Resume()
        { // not yet implemented
        }

        public void Dispose()
        {
            m_prices.Clear();
            m_historyCache.Clear();
        }

        private PriceWrapper GetPricerWrapper(long sico)
        {
            lock (s_lock)
            {
                PriceWrapper pw;

                if (!m_prices.TryGetValue(sico, out pw))
                {
                    pw = new PriceWrapper();
                    pw.Sicovam = sico;
                    calculatePriceAndGreeks(ref pw);
                    m_prices.Add(sico, pw);
                }
                else
                {
                    if ((DateTime.Now - pw.Requested).TotalMinutes > 15)
                        calculatePriceAndGreeks(ref pw); // recalculate price and greeks every 15 minutes  
                }

                return pw;
            }
        }

        public Price GetPrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, bool wait)
        {
            long sico = 0;
            if ((!long.TryParse(ir.SICOVAM, out sico)) || (sico == 0))
                return new Price(double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);

            PriceWrapper pw = GetPricerWrapper(sico);
            Price p = pw.Price;

            return p;
        }

        public double GetDelta(string instrumentSicovam, string underlyingSicovam)
        {
            long sico = 0;
            if ((!long.TryParse(instrumentSicovam, out sico)) || (sico == 0))
                return 0;
            long underly = 0;
            if ((!long.TryParse(underlyingSicovam, out underly)) || (underly == 0))
                return 0;
            PriceWrapper pw = GetPricerWrapper(sico);
            double delta = 0;
            pw.Deltas.TryGetValue(underly, out delta);
            return delta;
        }

        public double GetGamma(string instrumentSicovam, string underlyingSicovam)
        {
            long sico = 0;
            if ((!long.TryParse(instrumentSicovam, out sico)) || (sico == 0))
                return 0;
            long underly = 0;
            if ((!long.TryParse(underlyingSicovam, out underly)) || (underly == 0))
                return 0;
            PriceWrapper pw = GetPricerWrapper(sico);
            double gamma = 0;
            pw.Gammas.TryGetValue(underly, out gamma);
            return gamma;
        }

        private void calculatePriceAndGreeks(ref PriceWrapper pw)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            pw.Requested = DateTime.Now;
            //pw.Price = new Price(double.NaN, DateTime.Now);
            pw.Price.Value = double.NaN;
            pw.Price.Updated = DateTime.Now;
            pw.Deltas.Clear();
            pw.Gammas.Clear();

            try
            {
                Logger.Info(methodName + " : calculating (sico: " + pw.Sicovam + ") ..");
                double t, d, g;
                //pw.Price = new Price(t, DateTime.Now);
                pw.Price.Value = 1;
                // delta, gamma
#if SOPHIS_7
                var underlyings = new ArrayList();
                instrument.EnumerateUnderlyingCodes(underlyings);

                for (int i = 0; i < underlyings.Count; i++)
                {
                    try
                    {
                        SophisAdapter.GetInstance().GetPriceDeltaGamma(instrument, out t, out d, out g, i);
                        int underlying = Convert.ToInt32(underlyings[i]);
                        pw.Deltas.Add(underlying, d);
                        pw.Gammas.Add(underlying, g);
                    }
                    catch (Exception ex)
                    {
                        Engine.Instance.Log.Warn("**** EXCETPION: " +  ex.Message);
                    }
                }
#else
                for (int i = 0; i < 2; i++)
                {
                    int underlying = 1;
                    pw.Deltas.Add(underlying, 1);
                    pw.Gammas.Add(underlying, 1);
                }
#endif
                Logger.Info(methodName + " : calculated (sico: " + pw.Sicovam + ", price: )");
            }
            catch (Exception ex)
            {
                Logger.Error(methodName + " : exception caught '" + ex.Message + "' for (sico: " + pw.Sicovam + ")");
            }
        }

        public void ReloadClosings()
        {
        }

        public Price GetFXRate(string ccy1, string ccy2, bool useWMFixing, SPriceRequest priceRequest, bool wait)
        {
            ShowNotImplemented();
            return new Price(Double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);
        }

        public double GetAccrued(DateTime calcDate, string sicovam)
        {
            ShowNotImplemented();
            return 0.0;
        }

        public double GetBdCpnValue(DateTime now, string sicovam)
        {
            ShowNotImplemented();
            return double.NaN;
        }

        public bool GetCpnNext(DateTime calcDate, string sicovam, out DateTime exDate, out DateTime payDate)
        {
            ShowNotImplemented();
            exDate = DateTime.MinValue;
            payDate = DateTime.MinValue;
            return false;
        }

        public string ProviderName()
        {
            return "SophisAPI";
        }

        public void ShowNotImplemented()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            string callingMethodeName = stackTrace.GetFrame(1).GetMethod().Name;
            string error = @"The function '" + callingMethodeName + "' has not (yet) been implemented for price provider '" + ProviderName() + "'. Warn the developer!";
            MessageBox.Show(error, "MANAST", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Logger.Error(error);
        }

    }
}
