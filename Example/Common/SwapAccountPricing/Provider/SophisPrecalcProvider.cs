using Puma.MDE.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Puma.MDE.SwapAccountPricing.Provider
{
    /// <summary>
    /// Reuters provider class
    /// </summary>
    public class SophisPrecalcProvider : IPriceProvider
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

        public string GetConnectionInfo()
        {
            string info = string.Format("Db:{0}", "");
            return info;
        }

        public SophisPrecalcProvider(string source)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
        }

        ~SophisPrecalcProvider()
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
                    LoadPrecalcPriceGreeks(pw);
                    m_prices.Add(sico, pw);
                }
                else
                {
                    if ((DateTime.Now - pw.Requested).TotalMinutes > 15)
                        LoadPrecalcPriceGreeks(pw); // refresh price and greeks every 15 minutes  
                }

                return pw;
            }
        }

        public Price GetPrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, bool wait)
        {
            long sico = 0;
            if ((!long.TryParse(ir.SICOVAM, out sico)) || (sico == 0))
                return new Price(double.NaN, DateTime.Now);

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

        private void LoadPrecalcPriceGreeks(PriceWrapper pw)
        {
            pw.Requested = DateTime.Now;
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

        //public DateTime GetCpnPrev(DateTime calcDate, string sicovam, out DateTime exDate, out DateTime payDate)
        //{
        //    ShowNotImplemented();
        //    return DateTime.MinValue;
        //}

        public string ProviderName()
        {
            return "Sophis Precalc";
        }

        public void ShowNotImplemented()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            string callingMethodeName = stackTrace.GetFrame(1).GetMethod().Name;
            string error = @"The function '" + callingMethodeName + "' has not (yet) been implemented for price provider '" + ProviderName() + "'. Warn the developer!";
            MessageBox.Show(error, "MANAST", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }
}
