using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NLog;
using Puma.MDE.Data;

namespace Puma.MDE.SwapAccountPricing.Provider
{
    public class KxProvider : IPriceProvider
    {
        public static string NAME = "Kx-Sophis";

        // real-time prices
        private Dictionary<string, SubscribedInstrument> mSubscribedRICs = new Dictionary<string, SubscribedInstrument>();
        private Thread mThreadRealTime;
        private volatile bool mReadRealTimePrices = true;
        private ManualResetEvent mSuspendRTEvent = new ManualResetEvent(true); // true -> thread will NOT wait on first call to "WaitOne"
        private ManualResetEvent mSleepRealTimeEvent = new ManualResetEvent(false); // false -> thread will wait on first call to "WaitOne"
        private Dictionary<string, RealTimePrices> mRealTimePrices = new Dictionary<string, RealTimePrices>(); // (ric, real-time prices)
        static private int priceDelayMinutes = 30; // read delayed prices from Kx
        private int priceRefreshIntervalSeconds = 0; // price refresh frequency

        // close prices
        private Dictionary<int, SubscribedInstrument> mSubscribedSicovams = new Dictionary<int, SubscribedInstrument>();
        private Thread mThreadClose;
        private volatile bool mReadClosePrices = true;
        private ManualResetEvent mSuspendCloseEvent = new ManualResetEvent(true);
        public ManualResetEvent mSleepCloseEvent = new ManualResetEvent(false);
        private Dictionary<int, ClosePricesHistory> mClosePrices = new Dictionary<int, ClosePricesHistory>(); // (sicovam, closings)
        //private OracleConnection mClosePriceConnection = null; // used for reading close prices from Sophis (table HISTORIQUE)

        private string mConnection1Host = "";
        private string mConnection1PortString = "";
        private string mConnection2Host = "";
        private string mConnection2PortString = "";
        private string mUser = "";

        public string GetConnectionInfo()
        {
            string info = string.Format("Host:{0}, Port:{1}, User:{2}", mConnection1Host, mConnection1PortString, mUser);
            return info;
        }

        private Logger Logger { get { return Engine.Instance.Log; } }

        public KxProvider(string source)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName + " : initialising KxProvider..");

            mConnection1Host = Engine.Configuration.Get("Kx.Connection1Host");
            mConnection1PortString = Engine.Configuration.Get("Kx.Connection1Port");
            mConnection2Host = Engine.Configuration.Get("Kx.Connection2Host");
            mConnection2PortString = Engine.Configuration.Get("Kx.Connection2Port");
            mUser = Engine.Configuration.Get("Kx.User");

            var passwordEncrypted = Engine.Configuration.Get("Kx.Password.Encrypted");
            var password = "";
            if (passwordEncrypted != null)
                password = passwordEncrypted;
            else
                password = Engine.Configuration.Get("Kx.Password");

            Engine.Instance.Log.Info(methodName + " : Host1: {0}, Port1: {1}, Host2: {2}, Port2: {3}, User: {4}, Password: {5}",
                mConnection1Host, mConnection1PortString, mConnection2Host, mConnection2PortString, mUser, password);

            int connection1Port;
            if (!int.TryParse(mConnection1PortString, out connection1Port))
            {
                string error = string.Format("Can't parse port number:{0}", mConnection1PortString);
                ShowAndLogError(error);
            }

            int connection2Port;
            if (!int.TryParse(mConnection2PortString, out connection2Port))
            {
                string error = string.Format("Can't parse port number:{0}", mConnection2PortString);
                ShowAndLogError(error);
            }

            bool[] failed = new bool[2];

            for (int idx = 0; idx < 10; idx++)
            {
                int connectionNumber = idx + 1;
                Logger.Info(methodName + " : connecting to Kx connection {0}..", connectionNumber);

                try
                {
                    
                }
                catch (Exception ex)
                {
                    Logger.Info(ex);
                    Logger.Warn(methodName + " : failed to connect to {0}", connectionNumber);
                }

                if (true)
                {
                    Logger.Info(methodName + " : connected to {0}", connectionNumber);
                }
                else
                {
                    Logger.Warn(methodName + " : failed to connect to {0}", connectionNumber);
                    failed[idx] = true;
                }
            }

            string priceDelay = Engine.Configuration.Get("Kx.PriceDelay");
            if (int.TryParse(priceDelay, out priceDelayMinutes))
            {
                Logger.Info(methodName + " : price delay is {0} mins", priceDelayMinutes);
            }
            else
            {
                Logger.Info(methodName + " : no price delay set. Using default value {0} mins", priceDelayMinutes);
            }

            string refreshInterval = Engine.Configuration.Get("Kx.PriceRefreshInterval");
            if (!int.TryParse(refreshInterval, out priceRefreshIntervalSeconds))
                priceRefreshIntervalSeconds = 10; // by default, refresh every 10 seconds
            Logger.Info(methodName + " : price refresh interval: {0}", priceRefreshIntervalSeconds);

            if (failed.All(x => x))
            {
                ShowAndLogError("Couldn't obtain connection to Kx!");
                return;
            }

            // real-time prices
            mThreadRealTime = new Thread(ReadRealTimePrices);
            mThreadRealTime.IsBackground = true;

            // close prices
            mThreadClose = new Thread(ReadClosePrices);
            mThreadClose.IsBackground = true;

            Logger.Info(methodName + ": KxProvider initialised");
        }

        public void ReloadClosings()
        {
            mSleepCloseEvent.Set(); // allow the "ReadClosePrices" thread to continue when it is perhaps waiting at a WaitOne statement
        }

        public void Start()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            mThreadRealTime.Start();
            mThreadClose.Start();
            Logger.Info(methodName + " : started KxProvider");
        }

        public void Stop()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName + " : stopping KxProvider..");

            Logger.Info(methodName + " : stopping thread 'ReadRealTimePrices'..");
            //mThreadRealTime.Abort(); // do not call "Abort" since throws "System.Threading.ThreadAbortException" at the point where thread is waiting at "WaitOne"
            mReadRealTimePrices = false;
            mSleepRealTimeEvent.Set(); // allow the "ReadRealTimePrices" thread to continue when it is perhaps waiting at a WaitOne statement, so the "while (mReadRealTimePrices)" loop can stop immediately after

            Logger.Info(methodName + " : stopping thread 'ReadClosePrices'..");
            //mThreadClose.Abort(); // do not call "Abort"
            mReadClosePrices = false;
            mSleepCloseEvent.Set(); // allow the "ReadClosePrices" thread to continue when it is perhaps waiting at a WaitOne statement, so the "while (mReadClosePrices)" loop can stop immediately after
        }

        public void Suspend()
        // we do not use the deprecated "Thread.Suspend", but implement our own using a ManualResetEvent
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName + " : suspended KxProvider");
            mSuspendRTEvent.Reset();
            mSuspendCloseEvent.Reset();
        }

        public void Resume()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName + " : resumed KxProvider");
            mSuspendRTEvent.Set();
            mSuspendCloseEvent.Set();
        }

        public Price GetPrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, bool wait)
        {
            if (ir.IsCash)
                return new Price(1.0, DateTime.Now, priceRequest, priceRequest.mField);

            switch (priceRequest.mField)
            {
                case TypeManastPriceField.eRealTimeLast:
                case TypeManastPriceField.eRealTimeAsk:
                case TypeManastPriceField.eRealTimeBid:
                    {
                        return GetRealTimePrice(ir, priceRequest, wait);
                    }
                case TypeManastPriceField.eClose:
                case TypeManastPriceField.eFixing1:
                case TypeManastPriceField.eNAV:
                    {
                        DateTime d = DateTime.MinValue;
                        switch (priceRequest.mRefDate)
                        {
                            case TypeManastCloseRefDate.eTMinus1:
                                d = DateTime.Now.Date.AddDays(-1);
                                d = PortfolioPricer.getWeekday(d); // weekday (Mon-Fri) that is <= d
                                break;
                            case TypeManastCloseRefDate.eT:
                                d = DateTime.Now.Date;
                                break;
                            case TypeManastCloseRefDate.eSpecificDate:
                                if (!priceRequest.mCloseDate.HasValue)
                                    throw new ArgumentOutOfRangeException("closeDate", "date cannot be null when type is 'Specific date'");
                                d = priceRequest.mCloseDate.Value;
                                break;
                            default:
                                d = DateTime.Now.Date;
                                break;
                        }
                        return GetClosePrice(ir, priceRequest, d, wait);
                    }
                default:
                    break;
            }

            return new Price(double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);
        }

        private Price GetRealTimePrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, bool wait)
        // note: parameter "wait" is not yet implemented
        {
            if ((priceRequest.mField < TypeManastPriceField.eRealTimeLast) || (priceRequest.mField > TypeManastPriceField.eRealTimeBid))
                throw new ArgumentOutOfRangeException("type", "type must be Last, Ask or Bid");

            string ric = ir.RIC;
            RealTimePrices prices = null;
            bool doRegister = false;

            lock (mRealTimePrices)
            {
                if (!mRealTimePrices.TryGetValue(ric, out prices))
                {
                    prices = new RealTimePrices(); // initializes Last=NaN, Ask=NaN, Bid=NaN
                    mRealTimePrices.Add(ir.RIC, prices);
                    doRegister = true;
                }
            }

            // to avoid deadlocks, we don't want to call Register inside the above lock statement
            if (doRegister)
            {
                RegisterRIC(ric, ir);
                mSleepRealTimeEvent.Set(); // allow the "ReadRealTimePrices" thread to continue when it is perhaps waiting at a WaitOne statement
            }

            return new Price(prices.mPrices[priceRequest.mField - TypeManastPriceField.eRealTimeLast], priceRequest, priceRequest.mField);
        }

        private void RegisterRIC(string ric, InstrumentRowSnapshot ir)
        {
            lock (mSubscribedRICs)
            {
                if (!mSubscribedRICs.ContainsKey(ric))
                {
                    mSubscribedRICs[ric] = new SubscribedInstrument(ir, null);
                }
            }
        }

        private Price GetClosePrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest, DateTime closeDate, bool wait)
        {
            if ((priceRequest.mField < TypeManastPriceField.eClose) || (priceRequest.mField > TypeManastPriceField.eNAV))
                throw new ArgumentOutOfRangeException("type", "type must be Close, Fixing1 or NAV");

            if ((priceRequest.mRefDate < TypeManastCloseRefDate.eTMinus1) || (priceRequest.mRefDate > TypeManastCloseRefDate.eSpecificDate))
                throw new ArgumentOutOfRangeException("refDate", "refDate must be TMinus1, T or SpecificDate");

            int sicovam = 0;
            if ((!int.TryParse(ir.SICOVAM, out sicovam)) || (sicovam == 0))
                return new Price(double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);

            ClosePricesHistory prices = null;
            bool doRegister = false;

            lock (mClosePrices)
            {
                if (!mClosePrices.TryGetValue(sicovam, out prices))
                {
                    prices = new ClosePricesHistory();
                    prices.mStartdate = closeDate; // added by DD on Sept 9, 2022
                    mClosePrices.Add(sicovam, prices);
                    doRegister = true;
                }
                else
                {
                    if (closeDate < prices.mStartdate)
                    {
                        prices.mStartdate = closeDate; // added by DD on Sept 9, 2022
                        doRegister = true; // get older prices
                    }
                }
            }

            // to avoid deadlocks, we don't want to call Register inside the above lock statement
            if (doRegister)
            {
                RegisterSicovam(sicovam, ir, closeDate);
                if (wait)
                { // wait until prices are loaded before continuing
                    LoadClosePrices();
                }
                else
                { // release the thread that loads the prices, but do not wait until it has finished
                    mSleepCloseEvent.Set(); // allow the "ReadClosePrices" thread to continue when it is perhaps waiting at a WaitOne statement
                }
            }

            // search after date and price field in the (descendingly) sorted "prices.mHistory"
            // if not found on the specified date and field is different from Close then search after field = Close

            TypeManastPriceField field = priceRequest.mField;

            while (true)
            {
                int idx = field - TypeManastPriceField.eClose;

                int i = 0;
                while ((i < prices.mHistory.Count()) && ((prices.mHistory[i].Key > closeDate)))
                    i++;

                if (i < prices.mHistory.Count())
                {
                    if (field != TypeManastPriceField.eClose)
                    {
                        if (prices.mHistory[i].Key == closeDate)
                            return new Price(prices.mHistory[i].Value.mPrices[idx], priceRequest, field); // eg. Fixing1 found on the specified date
                        else
                            field = TypeManastPriceField.eClose; // field (eg. Fixing1) not found on specified date -> search after Close on date <= specified date
                    }
                    else
                    {
                        return new Price(prices.mHistory[i].Value.mPrices[idx], priceRequest, field); // Close found on date <= specified date
                    }
                }
                else
                {
                    if (field != TypeManastPriceField.eClose)
                        field = TypeManastPriceField.eClose; // field (eg. Fixing1) not found at all -> search after Close
                    else
                        break; // Close not found at all -> stop
                }
            }

            return new Price(double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);
        }

        private void RegisterSicovam(int sicovam, InstrumentRowSnapshot ir, DateTime closeDate)
        {
            lock (mSubscribedSicovams)
            {
                SubscribedInstrument i = null;
                if (!mSubscribedSicovams.TryGetValue(sicovam, out i))
                {
                    mSubscribedSicovams[sicovam] = new SubscribedInstrument(ir, closeDate);
                }
                else
                {
                    if (closeDate < i.mCloseDate)
                        i.mCloseDate = closeDate; // store the smallest (oldest) date
                }
            }
        }

        public Price GetFXRate(string ccy1, string ccy2, bool useWMFixing, SPriceRequest priceRequest, bool wait)
        // return ccy2/ccy1. For example, a call to GetFXRate(CHF,USD) returns USD/CHF
        {
            if (ccy1 == ccy2)
                return new Price(1.0, DateTime.Now, priceRequest, priceRequest.mField);
            else if ((ccy1 == "GBX") && (ccy2 == "GBP"))
                return new Price(100, DateTime.Now, priceRequest, priceRequest.mField); // GBP/GBX
            else if ((ccy1 == "GBP") && (ccy2 == "GBX"))
                return new Price(0.01, DateTime.Now, priceRequest, priceRequest.mField); // GBX/GBP

            bool inverse = false;
            string fxName = (useWMFixing) ? string.Format("WMFIXING {0}/{1}", ccy2, ccy1) : string.Format("{0}/{1}", ccy2, ccy1); // eg. search after "WMFIXING USD/CHF" or "USD/CHF"
            SwapAccountInstrument ir = null; // eg. try to get instrument "USD/CHF"
            if (ir == null)
            {
                inverse = true;
                fxName = (useWMFixing) ? string.Format("WMFIXING {0}/{1}", ccy1, ccy2) : string.Format("{0}/{1}", ccy1, ccy2);
                ir = null; // eg. try to get instrument "CHF/USD"
                if (ir == null)
                    return new Price(double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);
            }
            InstrumentRowSnapshot snapshot = new InstrumentRowSnapshot(ir);
            Price fxRate = GetPrice(snapshot, priceRequest, wait);

            if (true)
            {
                if ((ir.ContractSize != 1.0) || (inverse))
                {
                    fxRate = new Price(fxRate); // copy the price as otherwise the below *= modifies the price inside the cache
                    fxRate.Value *= ir.ContractSize;
                    if (inverse)
                        fxRate.Value = 1.0 / fxRate.Value; // eg. price for USD/EUR
                }
            }

            return fxRate;
        }

        public double GetAccrued(DateTime calcDate, string sicovam)
        {
            int sico = 0;
            if (!Int32.TryParse(sicovam, out sico))
                return 0.0;
            return 0.0;
        }

        public double GetBdCpnValue(DateTime calcDate, string sicovam)
        {
            int sico = 0;
            if (!Int32.TryParse(sicovam, out sico))
                return 0.0;

            DateTime exDate, payDate;
            return 0;
        }

        public bool GetCpnNext(DateTime calcDate, string sicovam, out DateTime exDate, out DateTime payDate)
        {
            exDate = DateTime.MinValue;
            payDate = DateTime.MinValue;
            int sico = 0;
            if (!Int32.TryParse(sicovam, out sico))
                return false;
            double coupon = 0;
            return true;
        }

        public string ProviderName()
        {
            return "Kx";
        }

        public void Dispose()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
        }

        /// <summary>
        /// This method is executed in a separate thread, periodically reads real-time prices from Kx and Sophis DRT
        /// </summary>
        /// 
        private void ReadRealTimePrices()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName + " : started thread");

            while (mReadRealTimePrices)
            {
                try
                {
                    mSuspendRTEvent.WaitOne(); // wait until receive signal to resume, see functions "KxProvider.Suspend" and "KxProvider.Resume"

                    mSleepRealTimeEvent.WaitOne(priceRefreshIntervalSeconds * 1000); // wait typically 15 seconds, but can be released sooner in case "Set" is called inside "Register"
                    mSleepRealTimeEvent.Reset(); // make sure this thread again blocks at calling "WaitOne" at the next iteration

                    if (!mReadRealTimePrices)
                        break;

                    if (mSubscribedRICs.Count == 0)
                        continue;

                    Dictionary<string, SubscribedInstrument> localCopy = null; // work on a local copy
                    lock (mSubscribedRICs)
                    {
                        localCopy = new Dictionary<string, SubscribedInstrument>(mSubscribedRICs); // work on a local copy, this is NOT a deep copy
                    }

                    string[] rics = localCopy.Keys.ToArray();

                    {
                        DateTime start = DateTime.Now;

                        // read LAST, BID and ASK from Kx
                        List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
                        if (data == null)
                            continue;

                        foreach (Dictionary<string, object> price in data)
                        {
                            string symbol = price["sym"] as string;
                            if (string.IsNullOrEmpty(symbol))
                                continue;

                            Double? doubleLast = price["rtrLast"] as Double?;
                            if (doubleLast.HasValue)
                            {
                                SaveRealTimePrice(symbol, TypeManastPriceField.eRealTimeLast, doubleLast.Value);
                            }

                            Double? doubleAsk = price["rtrAsk"] as Double?;
                            if (doubleAsk.HasValue)
                            {
                                SaveRealTimePrice(symbol, TypeManastPriceField.eRealTimeAsk, doubleAsk.Value);
                            }

                            Double? doubleBid = price["rtrBid"] as Double?;
                            if (doubleBid.HasValue)
                            {
                                SaveRealTimePrice(symbol, TypeManastPriceField.eRealTimeBid, doubleBid.Value);
                            }

                            if ((!doubleLast.HasValue || double.IsNaN(doubleLast.Value)) && doubleAsk.HasValue && doubleBid.HasValue)
                            {
                                SubscribedInstrument i = null;
                                if ((localCopy.TryGetValue(symbol, out i)) && (i.mInstrumentType != TypeManstInstrument.Stock)) // #46126 : when Last is missing then set Last equal to Mid but NOT for stocks
                                {
                                    double mid = (doubleAsk.Value + doubleBid.Value) / 2;
                                    SaveRealTimePrice(symbol, TypeManastPriceField.eRealTimeLast, mid);
                                }
                            }

                            localCopy[symbol] = null; // to mark the ric as found
                        }

                        DateTime end = DateTime.Now;
                        Logger.Info(methodName + " : read prices from Kx in {0} (hh:mi:ss.ms). Subscribed: {1}, received: {2}", (end - start), rics.Length, data.Count);
                    }

                    if (!mReadRealTimePrices)
                        break;

                    {
                        DateTime start = DateTime.Now;

                        int count = 0;
                        int retrieved = 0;

                        foreach (KeyValuePair<string, SubscribedInstrument> keyValue in localCopy)
                        {
                            if (keyValue.Value == null)
                                continue; // this ric was found in Kx -> continue

                            count = count + 1;

                            string ric = keyValue.Key;
                            int sicovam = keyValue.Value.mSicovam;

                            if (sicovam == 0)
                            {
                                Logger.Info(methodName + " : {0} not found in Kx, cannot read from Sophis because has no sicovam", keyValue.Value);
                                continue;
                            }

                            double spot = 0;

                            if (!Double.IsNaN(spot))
                            {
                                SaveRealTimePrice(ric, TypeManastPriceField.eRealTimeLast, spot);
                                SaveRealTimePrice(ric, TypeManastPriceField.eRealTimeAsk, spot);
                                SaveRealTimePrice(ric, TypeManastPriceField.eRealTimeBid, spot);
                                Logger.Info(methodName + " : {0} not found in Kx, read from Sophis: {1:N2}", keyValue.Value, spot);
                                retrieved = retrieved + 1;
                            }
                            else
                            {
                                Logger.Info(methodName + " : {0} not found in Kx nor Sophis", keyValue.Value);
                            }
                        }

                        DateTime end = DateTime.Now;
                        Logger.Info(methodName + " : read prices from Sophis in {0} (hh:mi:ss.ms). Subscribed: {1}, retrieved: {2}", (end - start), count, retrieved);
                    }
                }
                catch (Exception e)
                {
                    Logger.Info(e.ToString());
                    mReadRealTimePrices = false; // stop loop, to avoid getting same error over and over, causing log file to explode
                    Logger.Error("stopped " + methodName);
                }
            } // while

            Logger.Info(methodName + " : stopped thread");
        }

        private List<Dictionary<string, object>> ReadData(string function, object[] parameters)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            object result = null;

            for (int idx = 0; idx < 10; idx++)
            {
                try
                {
                    result = null;
                    if (result != null) break;
                }
                catch (Exception ex)
                {
                    Logger.Warn(methodName + " : exception caught querying connection {0} : {1}", idx + 1, ex.Message);
                    Logger.Info(ex);
                }
            }

            return new List<Dictionary<string, object>>();
        }

        private void SaveRealTimePrice(string ric, TypeManastPriceField field, double value)
        {
            lock (mRealTimePrices)
            {
                RealTimePrices prices;
                if (!mRealTimePrices.TryGetValue(ric, out prices)) // normally does not happen, since entry is created inside "GetPrice"?
                {
                    prices = new RealTimePrices();
                    mRealTimePrices.Add(ric, prices);
                }
                prices.mPrices[(int)field] = new Price(value, DateTime.Now);
            }
        }

        private void ReadClosePrices()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName + " : started thread");

            while (mReadClosePrices)
            {
                try
                {
                    mSuspendCloseEvent.WaitOne(); // wait until receive signal to resume, see functions "KxProvider.Suspend" and "KxProvider.Resume"

                    mSleepCloseEvent.WaitOne(); // wait until released by call to "Set" inside "Register"
                    mSleepCloseEvent.Reset(); // make sure this thread again blocks at calling "WaitOne" at the next iteration

                    if (!mReadClosePrices)
                        break;

                    if (mSubscribedSicovams.Count == 0)
                        continue;

                    LoadClosePrices();
                }
                catch (Exception e)
                {
                    Logger.Info(e);
                    mReadClosePrices = false; // stop loop, to avoid getting same error over and over, causing log file to explode
                    Logger.Error("stopped " + methodName);
                }
            }

            Logger.Info(methodName + " : stopped thread");
        }

        public void LoadClosePrices()
        {
            int LOOKBACK = 14;

            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            SubscribedInstrument[] localCopy; // work on a local copy
            lock (mSubscribedSicovams)
            {
                localCopy = mSubscribedSicovams.Values.ToArray();
            }

            Logger.Info(methodName + " : loading close prices for {0} sicovams..", localCopy.Count());

            DateTime start = DateTime.Now;

            Logger.Info(methodName + " : loaded close prices for {0} sicovams in {1} (hh:mi:ss.ms)", localCopy.Count(), DateTime.Now - start);
        }

        private void ShowAndLogError(string error)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Engine.Instance.Log.Error(methodName + " : " + error);
            MessageBox.Show(error, "MANAST", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public double GetDelta(string instrumentSicovam, string underlyingSicovam)
        {
            return 0;
        }

        public double GetGamma(string instrumentSicovam, string underlyingSicovam)
        {
            return 0;
        }
    }
}
