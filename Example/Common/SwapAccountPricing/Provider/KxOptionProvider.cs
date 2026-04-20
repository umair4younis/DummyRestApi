using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Puma.MDE.Data;
using System.Threading;
using NLog;

namespace Puma.MDE.SwapAccountPricing.Provider
{
    public class KxOptionProvider : IPriceProvider
    {
        public static string NAME = "KxOption";

        // real-time prices
        private Dictionary<string, SubscribedInstrument> mSubscribedRICs = new Dictionary<string, SubscribedInstrument>();
        private Thread mThreadRealTime;
        private volatile bool mReadRealTimePrices = true;
        private ManualResetEvent mSuspendRTEvent = new ManualResetEvent(true); // true -> thread will NOT wait on first call to "WaitOne"
        private ManualResetEvent mSleepRealTimeEvent = new ManualResetEvent(false); // false -> thread will wait on first call to "WaitOne"
        private Dictionary<string, RealTimePrices> mRealTimePrices = new Dictionary<string, RealTimePrices>(); // (ric, real-time prices)
        private int priceRefreshIntervalSeconds = 0; // price refresh frequency

        private string mConnection1Host = "";
        private string mConnection1PortString = "";
        private string mConnection2Host = "";
        private string mConnection2PortString = "";
        private string mUser = "";

        private Logger Logger { get { return Engine.Instance.Log; } }

        public string GetConnectionInfo()
        {
            string info = string.Format("Host:{0}, Port:{1}, User:{2}", mConnection1Host, mConnection1PortString, mUser);
            return info;
        }

        public KxOptionProvider(string source)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName + " : initialising KxOptionProvider..");
            
            Logger.Info(methodName + " : Host1: {0}, Port1: {1}, Host2: {2}, Port2: {3}, User: {4}, Password: {5}",
                mConnection1Host, mConnection1PortString, mConnection2Host, mConnection2PortString, mUser, "");

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
            Logger.Info(methodName + " : price refresh interval: {0}", priceRefreshIntervalSeconds);

            if (failed.All(x => x))
            {
                ShowAndLogError("Couldn't obtain connection to Kx!");
                return;
            }

            // real-time prices
            mThreadRealTime = new Thread(ReadRealTimePrices);
            mThreadRealTime.IsBackground = true;

            Logger.Info(methodName + " : KxOptionProvider initialised");
        }

        public void Start()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            mThreadRealTime.Start();
            Logger.Info(methodName + " : started KxOptionProvider");
        }

        public void Stop()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            Logger.Info(methodName + " : stopping KxOptionProvider..");

            Logger.Info(methodName + " : stopping thread 'ReadRealTimePrices'..");
            //mThreadRealTime.Abort(); // do not call "Abort" since throws "System.Threading.ThreadAbortException" at the point where thread is waiting at "WaitOne"
            mReadRealTimePrices = false;
            mSleepRealTimeEvent.Set();
        }

        public void Suspend()
        // we do not use the deprecated "Thread.Suspend", but implement our own using a ManualResetEvent
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName + " : suspended KxOptionProvider");
            mSuspendRTEvent.Reset();
        }

        public void Resume()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Info(methodName + " : resumed KxOptionProvider");
            mSuspendRTEvent.Set();
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
                        return GetRealTimePrice(ir, priceRequest);
                    }
                case TypeManastPriceField.eClose:
                case TypeManastPriceField.eFixing1:
                case TypeManastPriceField.eNAV:
                    break; // to do : redirect this call to "KxProvider::GetPrice"
                default:
                    break;
            }

            return new Price(double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);
        }

        private Price GetRealTimePrice(InstrumentRowSnapshot ir, SPriceRequest priceRequest)
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

            return prices.mPrices[priceRequest.mField - TypeManastPriceField.eRealTimeLast];
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

        public Price GetFXRate(string ccy1, string ccy2, bool useWMFixing, SPriceRequest priceRequest, bool wait)
        {
            return new Price(Double.NaN, DateTime.Now, priceRequest, TypeManastPriceField.eNoPriceField);
        }

        public double GetAccrued(DateTime calcDate, string sicovam)
        {
            return 0.0;
        }

        public double GetBdCpnValue(DateTime calcDate, string sicovam)
        {
            return 0.0;
        }

        public bool GetCpnNext(DateTime calcDate, string sicovam, out DateTime exDate, out DateTime payDate)
        {
            exDate = DateTime.MinValue;
            payDate = DateTime.MinValue;
            return false;
        }

        public string ProviderName()
        {
            return "KxOption";
        }

        public void Dispose()
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
        }

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

                        int received = 0;
                        foreach (string ric in rics)
                        {
                            // read mid from Kx
                            double? mid = ReadData(".mde.getLastMid", ric);
                            if (mid.HasValue)
                            {
                                received++;
                                SaveRealTimePrice(ric, TypeManastPriceField.eRealTimeLast, mid.Value);
                                SaveRealTimePrice(ric, TypeManastPriceField.eRealTimeAsk, mid.Value);
                                SaveRealTimePrice(ric, TypeManastPriceField.eRealTimeBid, mid.Value);
                            }
                        }

                        DateTime end = DateTime.Now;

                        Logger.Info(methodName + " : read prices from Kx in {0} (hh:mi:ss.ms). Subscribed: {1}, received: {2}.", (end - start), rics.Length, received);
                    }
                }
                catch (Exception e)
                {
                    Logger.Info(e);
                    mReadRealTimePrices = false; // stop loop, to avoid getting same error over and over, causing log file to explode
                    Logger.Error("stopped " + methodName);
                }
            }

            Logger.Info(methodName + " : stopped thread");
        }

        private double? ReadData(string function, string ric)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            object result = null;
            return 0;
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
                prices.mPrices[(int)field] = new Price(value, DateTime.Now, null, field);
            }
        }

        public void ReloadClosings()
        {
        }

        private void ShowAndLogError(string error)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;
            Logger.Error(methodName + " : " + error);
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



        static public string getRicForListedOptionFromKx(string underlying, double strike, DateTime maturity, string putCall)
        {
            string foundRic = "";

            try
            {
                foundRic = "";
            }
            catch (Exception ex)
            {
                foundRic = "";
            }

            return foundRic;
        }

        // TODO aner
        // migration & usage of code still in progress
        //static public void getTimeSeries(string foundRic)
        //{
        //    try
        //    {
        //        // a dedicated Kx user to avoid "Access Denied" when calling below Kx functions
        //        string host = "mdbkxp1.tsy.fm.hypovereinsbank.de";
        //        int port = 8057;
        //        string user = "mdeuser";
        //        string pw = "PumaMDEpass";

        //        KdbConnector kxOpt = new KdbConnector(host, port, user, pw);
        //        kxOpt.connect();


        //        // get time series over period of time [Now - 1 day, Now]
        //        DateTime endDate = DateTime.Now;
        //        DateTime startDate = endDate.AddDays(-1);
        //        DateTime ConvertedStartDate = startDate.ToUniversalTime();
        //        DateTime ConvertedEndDate = endDate.ToUniversalTime();
        //        TimeStamp start = new TimeStamp((new TimeSpanQ((ConvertedStartDate.Date.Ticks + ConvertedStartDate.TimeOfDay.Ticks) * 100)).getAsQTimeStamp());
        //        TimeStamp end = new TimeStamp((new TimeSpanQ((ConvertedEndDate.Date.Ticks + ConvertedEndDate.TimeOfDay.Ticks) * 100)).getAsQTimeStamp());

        //        string[] rics = new string[1];
        //        rics[0] = foundRic;
        //        Flip optQuotes = (Flip)(kxOpt.syncQuery(".mde.getTimeseries", "quote", "", rics, start, end));

        //        string[] cols = optQuotes.getColumnNames();

        //        Dictionary<String, int> columns = new Dictionary<string, int>();
        //        for (int i = 0; i < cols.Length; i++)
        //        {
        //            columns.Add(cols[i], i);
        //        }

        //        string[] syms = (string[])optQuotes.getColumnValues()[columns["sym"]];
        //        TimeStamp[] dates = (TimeStamp[])optQuotes.getColumnValues()[columns["time"]];
        //        double[] settlements = (double[])optQuotes.getColumnValues()[columns["hstclose"]]; // this corresponds to field "PREV_CLOSE_VALUE_REALTIME" in BBG
        //        double[] bids = (double[])optQuotes.getColumnValues()[columns["bid"]];
        //        double[] asks = (double[])optQuotes.getColumnValues()[columns["ask"]];
        //        TimeStamp[] sttlementDates = (TimeStamp[])optQuotes.getColumnValues()[columns["hstclosedate"]];
        //    }
        //    catch (Exception ex)
        //    {
        //        Engine.Instance.LogInfo(ex.ToString());
        //    }
        //}

        //static public void getDividendsFromKx(string ric)
        //{
        //    try
        //    {
        //        string host = "mdbkxp1.tsy.fm.hypovereinsbank.de";
        //        int port = 5088;
        //        string user = "assetuser";
        //        string pw = "asset+pass";

        //        KdbConnector kx = new KdbConnector(host, port, user, pw);
        //        kx.connect();

        //        bool b = true;

        //        if (b)
        //        {
        //            string sym = "RT_" + ric;
        //            Flip divs = (Flip)(kx.syncQuery(".urdp.asset.getDividendsBySym", ric));

        //            string[] cols = divs.getColumnNames(); // time;sym;isin;financialYear;dividendType;reported;dayOfUpdate;timeOfUpdate;payDate;exDividendDay;currency;divEstimation;comment;announced;announceDate;recordDate;fullComment;dividendKey;divEstimationRaw;grossAmountPublished;announcementPublished;xddPublished;closeOfBooksPublished;payDatePublished
        //            Dictionary<String, int> columns = new Dictionary<string, int>();
        //            for (int i = 0; i < cols.Length; i++)
        //            {
        //                columns.Add(cols[i], i);
        //            }
        //            string cols_s = string.Join(";", cols); //

        //            TimeStamp[] times = (TimeStamp[])divs.getColumnValues()[columns["time"]];
        //            string[] syms = (string[])divs.getColumnValues()[columns["sym"]];
        //            string[] isins = (string[])divs.getColumnValues()[columns["isin"]];
        //            Date[] payDates = (Date[])divs.getColumnValues()[columns["payDate"]];
        //            Date[] exDividendDays = (Date[])divs.getColumnValues()[columns["exDividendDay"]];
        //            double[] divEstimations = (double[])divs.getColumnValues()[columns["divEstimation"]];
        //        }
        //        else
        //        {
        //            string function = ".urdp.asset.divsAll[]";
        //            Flip divs = (Flip)(kx.syncQuery(function));

        //            string[] cols = divs.getColumnNames(); // ISIN;FinYear;Type;Xdd;PayDate;DaddEstimation;BestEstimator;HVBEstimator;Currency;Reported
        //            Dictionary<String, int> columns = new Dictionary<string, int>();
        //            for (int i = 0; i < cols.Length; i++)
        //            {
        //                columns.Add(cols[i], i);
        //            }
        //            string cols_s = string.Join(";", cols);

        //            string[] isins = (string[])divs.getColumnValues()[columns["ISIN"]];
        //            string[] payDates = (string[])divs.getColumnValues()[columns["PayDate"]];
        //            string[] Xdds = (string[])divs.getColumnValues()[columns["Xdd"]];
        //            double[] DaddEstimations = (double[])divs.getColumnValues()[columns["DaddEstimation"]];
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Engine.Instance.LogInfo(ex.ToString());
        //    }
        //}

        //static public void listedOptionsInKx()
        ///*
        //    - demonstrate how to get last (mid) from Kx
        //    - demonstrate how to get historical time series to get closings?
        //    - insired by :
        //        "mainline\SophisExtensions\PuMa.MDE\classes\PumaMDE\Quotation\Source\Kx\AttributeProviderKx.cs"
        //        "mainline\SophisExtensions\PuMa.MDE\classes\PumaMDE\Quotation\Source\Kx\QuoteProviderKxListed.cs"
        //        "mainline\SophisExtensions\PuMa.MDE\classes\PumaMDE\MarketData\RisqueBridgeTweaked\MarketDataRisquebridgeTweaked.cs"
        //    - note : strike range of options present in Kx is configured in MDE in the underlying tab "Kx Listed Option Bounds" for S&P
        //    - note : this only gets BBG options, not Eurex. To get Eurex, one must call ".mde.getEurexOptions" - see function "public Dictionary<String, KxOption> getOptions(Puma.MDE.Data.Underlying und)" inside "AttributeProviderKx.cs"
        //*/
        //{
        //    string ric = ".SPX";

        //    // a dedicates Kx user to avoid "Access Denied" when calling below Kx functions
        //    string host = "mdbkxp1.tsy.fm.hypovereinsbank.de";
        //    int port = 8057;
        //    string user = "mdeuser";
        //    string pw = "PumaMDEpass";

        //    // the listed option we are interested in : "SXY 21/12/18 P 2300" (Sophis reference)
        //    double seekStrike = 2925; //  2300; //  ;
        //    DateTime seekDate = new DateTime(2018, 12, 21);
        //    string seekPutCall = "Put";
        //    string foundRic = "";

        //    try
        //    {
        //        KdbConnector kxOpt = new KdbConnector(host, port, user, pw);
        //        kxOpt.connect();

        //        // get all possible options on S&P, then loop and locate the one having strike, maturity and type that we seek
        //        {
        //            Flip options = (Flip)kxOpt.syncQuery(".mde.getOptions", ric, GetNow());
        //            string[] cols = options.getColumnNames();
        //            Dictionary<String, int> columns = new Dictionary<string, int>();

        //            for (int i = 0; i < cols.Length; i++)
        //            {
        //                columns.Add(cols[i], i);
        //            }

        //            string[] syms = (string[])options.getColumnValues()[columns["sym"]];
        //            double[] strikes = (double[])options.getColumnValues()[columns["strike"]];
        //            string[] putCalls = (string[])options.getColumnValues()[columns["putCall"]];
        //            string[] exercises = (string[])options.getColumnValues()[columns["exerciseType"]];
        //            Csharp2q.types.Date[] maturities = (Csharp2q.types.Date[])options.getColumnValues()[columns["maturity"]];

        //            for (int i = 0; i < strikes.Count(); i++)
        //            {
        //                double strike = strikes[i];
        //                if (strike != seekStrike)
        //                    continue;
        //                Csharp2q.types.Date mat = maturities[i];
        //                if (mat.getDate() == null)
        //                    continue;
        //                if (((DateTime)mat.getDate()).Date != seekDate.Date)
        //                    continue;
        //                string putCall = putCalls[i];
        //                if (putCall != seekPutCall)
        //                    continue;
        //                foundRic = syms[i];
        //                break;
        //            }
        //        }

        //        if (foundRic == "")
        //            return;

        //        // get last mid (not clear what it returns when market is closed)
        //        // Dennis sais CBOE (Chicago) has trading times 9:00-15:15 CET and 15:30-22:15 CET
        //        {
        //            Flip spot = (Flip)kxOpt.syncQuery(".mde.getLastMid", foundRic);

        //            string[] cols = spot.getColumnNames();
        //            Dictionary<String, int> columns = new Dictionary<string, int>();
        //            for (int i = 0; i < cols.Length; i++)
        //            {
        //                columns.Add(cols[i], i);
        //            }
        //            double[] price;
        //            TimeStamp[] time;

        //            price = (double[])spot.getColumnValues()[columns["mid"]];
        //            time = (TimeStamp[])spot.getColumnValues()[columns["time"]];
        //        }

        //        // get time series over period of time [Now - 1 day, Now]
        //        {
        //            DateTime endDate = DateTime.Now;
        //            DateTime startDate = endDate.AddDays(-1);
        //            DateTime ConvertedStartDate = startDate.ToUniversalTime();
        //            DateTime ConvertedEndDate = endDate.ToUniversalTime();
        //            TimeStamp start = new TimeStamp((new TimeSpanQ((ConvertedStartDate.Date.Ticks + ConvertedStartDate.TimeOfDay.Ticks) * 100)).getAsQTimeStamp());
        //            TimeStamp end = new TimeStamp((new TimeSpanQ((ConvertedEndDate.Date.Ticks + ConvertedEndDate.TimeOfDay.Ticks) * 100)).getAsQTimeStamp());

        //            string[] rics = new string[1];
        //            rics[0] = foundRic;
        //            Flip optQuotes = (Flip)(kxOpt.syncQuery(".mde.getTimeseries", "quote", "", rics, start, end));

        //            string[] cols = optQuotes.getColumnNames();

        //            Dictionary<String, int> columns = new Dictionary<string, int>();
        //            for (int i = 0; i < cols.Length; i++)
        //            {
        //                columns.Add(cols[i], i);
        //            }

        //            string[] syms = (string[])optQuotes.getColumnValues()[columns["sym"]];
        //            TimeStamp[] dates = (TimeStamp[])optQuotes.getColumnValues()[columns["time"]];
        //            double[] settlements = (double[])optQuotes.getColumnValues()[columns["hstclose"]]; // this corresponds to field "PREV_CLOSE_VALUE_REALTIME" in BBG
        //            double[] bids = (double[])optQuotes.getColumnValues()[columns["bid"]];
        //            double[] asks = (double[])optQuotes.getColumnValues()[columns["ask"]];
        //            TimeStamp[] sttlementDates = (TimeStamp[])optQuotes.getColumnValues()[columns["hstclosedate"]];
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Engine.Instance.LogInfo(ex.ToString());
        //    }
        //}
    }
}
