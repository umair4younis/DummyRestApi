using NHibernate;
using NLog;
using Puma.MDE.Common.Configuration;
using Puma.MDE.Data;
using Puma.MDE.Pricing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Puma.MDE
{
    public class SqlStatementInterceptor : EmptyInterceptor
    {
        public Logger Log { get; }

        public SqlStatementInterceptor(Logger log)
        {
            Log = log;
        }

        public override NHibernate.SqlCommand.SqlString OnPrepareStatement(NHibernate.SqlCommand.SqlString sql)
        {
            Log.Debug($"Executing SQL command: {sql}");
            return sql;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("83DE0DD2-E780-4bdc-B4FB-E3D51DCF80B4")]
    [ComVisible(true)]

    public sealed class Starter
    {
        public Engine Engine
        {
            get
            {
                return Engine.Instance;
            }
        }
        public object WellKnownStrategiesFactory
        {
            get
            {
                return null;
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("520C0EBE-7BCA-431f-9AD6-85A92DD11B65")]
    [ComVisible(true)]
    public sealed partial class Engine
    {
        public ILogger Logger => Log;  // expose the real logger

        [ComVisible(false)]
        internal class Configuration
        {
            static System.Configuration.Configuration config;

            static Configuration()
            {
                Uri uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                config = ConfigurationManager.OpenExeConfiguration(uri.LocalPath);


                //var location = Path.GetDirectoryName(typeof(Configuration).Assembly.Location);
                //if (location == null)
                //    return;


                //var files = Directory.GetFiles(string.Format(@"{0}\Application.Configuration\", location), "*.Config");

                //foreach (var file in files)
                //{
                //    var currentFile = file;
                //    var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = currentFile };
                //    var configLoaded = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                //    if (config == null)
                //    {
                //        config = configLoaded;
                //    }
                //    else
                //    {
                //        var enumer = configLoaded.AppSettings.Settings.GetEnumerator();
                //        while (enumer.MoveNext())
                //        {
                //            var current = enumer.Current as KeyValueConfigurationElement;
                //            if (current != null)
                //                config.AppSettings.Settings.Add(current.Key, current.Value);
                //        }
                //    }
                //}
            }

            static public String Get(String key)
            {
                KeyValueConfigurationElement elem = config.AppSettings.Settings[key];
                if (elem == null)
                    throw new PumaMDEException("setting " + key + " not found");
                return elem.Value;
            }

            static public KeyValueConfigurationCollection Settings
            {
                get
                {
                    return config.AppSettings.Settings;
                }
            }
        }


        [ComVisible(false)]
        internal class StandardConfigProvider : IConfig
        {
            [DebuggerStepThrough]
            public string Get(string key)
            {
                var retVal = key;
                if (retVal != null)
                    return retVal;

                return Engine.Configuration.Settings[key].Value;
            }
            public int Count()
            {
                return Engine.Configuration.Settings.Count;
            }
            public string Item(int i)
            {
                var key = Engine.Configuration.Settings.AllKeys[i];
                return Get(key);
            }
            public string Key(int i)
            {
                return Engine.Configuration.Settings.AllKeys[i];
            }
            public bool IsDefined(string key)
            {
                return Engine.Configuration.Settings.AllKeys.Contains<string>(key);
            }

            // used by Swap functionality to set values per run per db select
            public void Set(string key, string value)
            {
                Engine.Configuration.Settings.Add(key, value);
            }

            public List<string> GetCollection(string prefix)
            {
                return Engine.Configuration.Settings.AllKeys.Where(configKey => configKey.StartsWith(prefix)).ToList();
            }
        }


        public IConfig Config = new StandardConfigProvider();

        static public T CreateObject<T>(string classOrProgID, params object[] args)
        {
            Type type = Type.GetType(classOrProgID);
            if (type == null)
            {
                type = Type.GetTypeFromProgID(classOrProgID);
            }
            if (type == null)
            {
                throw new PumaMDEException("class " + classOrProgID + " not found");
            }

            return (T)Activator.CreateInstance(type, args);
        }

        static public T CreateObject<T>(string classOrProgID, System.Reflection.BindingFlags flags, params object[] args)
        {
            Type type = Type.GetType(classOrProgID);
            if (type == null)
            {
                type = Type.GetTypeFromProgID(classOrProgID);
            }
            if (type == null)
            {
                throw new PumaMDEException("class " + classOrProgID + " not found");
            }

            return (T)Activator.CreateInstance(type, flags, null, args, null);
        }


        static readonly Engine instance = new Engine();

        NHibernate.Cfg.Configuration cfg;

        ISession _session = null;
        ISessionFactory factory = null;


        User connectedUser;
        Dictionary<string, IMarketData> markets;
        IMarketData market;
        IHistoricalVolatilityProvider historicalVolProvider;
        Dictionary<string, string[]> exchangeTradeStatus;
        Dictionary<string, string> kxSpecialFxRics;
        Dictionary<string, string[]> oisDiscountCirves;
        Dictionary<int, int> ONDriftCmpCurveFamilyIDs;
        string fxFwdDiscountFamilyCurveName;

        Logger log = LogManager.GetLogger("puma.mde");

        private Engine()
        {
            ForceRTChannelConnection = false;
            ConnectionWithMarketData = true;
            ConnectionWithKX = true;

            AppDomain.CurrentDomain.AssemblyResolve += null;
        }
        ~Engine()
        {
            Disconnect();
        }

        [ComVisible(false)]
        public static Engine Instance
        {
            get
            {
                return instance;
            }
        }

        public string Application
        {
            get
            {
                try
                {
                    return Assembly.GetEntryAssembly().GetName().Name;
                }
                catch
                {
                    return "UNKNOWN";
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return getFactory() != null;
            }
        }

        [ComVisible(false)]
        public Logger Log
        {
            get
            {
                return log;
            }
        }

        public string GetHistomvtsName()
        {
#if SOPHIS_7
            return "JOIN_POSITION_HISTOMVTS";
#else
            return "HISTOMVTS";
#endif
        }


        [ComVisible(false)]
        public ISessionFactory getFactory()
        {
            return factory;
        }

        public DateTime Today { get; set; }

        public void Disconnect()
        {
            lock (this)
            {
                Factory = null;

                if (_session != null)
                {
                    Log.Info("disconnecting");
                    try
                    {
                        _session.Close();
                    }
                    catch
                    {
                    }
                    _session = null;
                }

                cfg = null;
                if (factory != null)
                {
                    factory.Close();
                    factory = null;
                }
                connectedUser = null;
            }
        }

        [DebuggerStepThrough]
        public void Connect(String username)
        {
            lock (this)
            {
                try
                {
                    Log.Info("connecting as {0}", username);

                    RegisterAmericanPricer();
                    RegisterEuropeanPricer();


                    if (factory != null && connectedUser != null && connectedUser.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase))
                        return;

                    Disconnect();

                    Log.Info("loading nhibernate configuration");
                    cfg = new NHibernate.Cfg.Configuration();
                    IDictionary<string, string> props = new Dictionary<string, string>();

                    Uri uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                    string mainDirectory = System.IO.Directory.GetParent(uri.LocalPath).FullName;
                    cfg.Configure(mainDirectory + "\\hibernate.cfg.xml");

                    if (GetConfig<bool>("enable_sql_logging", false))
                    {
                        cfg.SetInterceptor(new SqlStatementInterceptor(Log));
                    }

                    props.Add("connection.connection_string",
                        String.Format("User ID={0};Password={1};Data Source={2}",
                            Config.Get("hibernate_oracle_user"),
                           Config.Get("hibernate_oracle_password"),
                            Config.Get("hibernate_oracle_database")
                            )
                        );



                    cfg.AddProperties(props);

                    Log.Info("opening session");

                    factory = cfg.BuildSessionFactory();
                    Factory = new DataFactory();

                    Log.Info("checking credentials");

                    User user = Factory.GetUser(username);
                    if (user == null)
                    {
                        throw new PumaMDEException("user not found");
                    }
                    connectedUser = user;

                    Log.Info("Successfully connected to db {0}@{1} via hibernate", Config.Get("hibernate_oracle_user"), Config.Get("hibernate_oracle_database"));

                    if (ConnectionWithKX)
                    {
                        RegisterMarketDataProviders();
                        RegisterQuoteProviders();
                        RegisterHistoricalVolatilityProvider();
                        ReadExchangeStatus();
                        ReadKxSpecialFxRics();
                        ReadOisDiscountCurves();
                        ReadONDriftCompoundingCurveFamilies();
                        ReadFxFwdDiscountCurves();
                        RegisterKxDataStorageConnection();
                        RegisterCalendarProviders();
                    }

                    string liqUser = null, liqPassword = null, liqOracle = null;
                    try
                    {
                        liqUser = Config.Get("liq_hibernate_oracle_user");
                        liqPassword = Config.Get("liq_hibernate_oracle_password");
                        liqOracle = Config.Get("liq_hibernate_oracle_database");
                    }
                    catch
                    {
                    }

                    if (
                        !String.IsNullOrEmpty(liqUser) &&
                        !String.IsNullOrEmpty(liqPassword) &&
                        !String.IsNullOrEmpty(liqOracle))
                    {
                    }
                    else
                    {
                        Log.Info("not liquidator database credentials provided, not liquidator database connection will be established");
                    }


                    Log.Info("Up & Running");

                }
                catch (Exception e)
                {
                    Disconnect();
                    throw e;
                }
            }
        }

        [ComVisible(false)]
        public KxStorageConnection KxStorageConnection { get; set; }

        [ComVisible(false)]
        private void RegisterKxDataStorageConnection()
        {
            if (Config.IsDefined("kx_port_storage"))
            {
                Log.Info("Registering kx data storage connection");
                KxStorageConnection = null;
            }
        }

        public IMarketData MarketData
        {
            get
            {
                return market;
            }
            set
            {
                market = value;
            }
        }

        public IHistoricalVolatilityProvider HistoricalVolatilityProvider
        {
            get
            {
                return historicalVolProvider;
            }
            set
            {
                historicalVolProvider = value;
            }
        }

        [ComVisible(false)]
        public Dictionary<string, string[]> ExchangeTradeStatus
        {
            get
            {
                return exchangeTradeStatus;
            }
            set
            {
                exchangeTradeStatus = value;
            }
        }

        [ComVisible(false)]
        public Dictionary<string, string> KxSpecialFxRics
        {
            get
            {
                return kxSpecialFxRics;
            }
            set
            {
                kxSpecialFxRics = value;
            }
        }

        [ComVisible(false)]
        public Dictionary<string, string[]> OisDiscountCurves
        {
            get
            {
                return oisDiscountCirves;
            }
            set
            {
                oisDiscountCirves = value;
            }
        }

        [ComVisible(false)]
        public Dictionary<int, int> ONDriftCompoundingCurves
        {
            get
            {
                return ONDriftCmpCurveFamilyIDs;
            }
            set
            {
                ONDriftCmpCurveFamilyIDs = value;
            }
        }

        [ComVisible(false)]
        public string FxFwdDiscountFamilyCurveName
        {
            get
            {
                return fxFwdDiscountFamilyCurveName;
            }
            set
            {
                fxFwdDiscountFamilyCurveName = value;
            }
        }

        public IPricer AmericanPricer
        {
            get
            {
                return null;
            }
            set
            {
                value = null;
            }
        }

        public IPricer EuropeanPricer
        {
            get
            {
                return null;
            }
            set
            {
                value = null;
            }
        }

        public IPricer GetPricer(String exercise)
        {
            exercise = exercise.ToLowerInvariant();
            if (exercise[0] == 'e')
                return EuropeanPricer;
            if (exercise[0] == 'a')
                return AmericanPricer;

            throw
                new PumaMDEException("invalid pricer name " + exercise);
        }

        [ComVisible(false)]
        public void RegisterMarketDataProvider(String provider, String classname)
        {
            if (ConnectionWithMarketData)
            {
                Log.Info("registering market data provider {0} for source {1}", classname, provider);
            }
            else
            {
                Log.Info("not registering market data provider");
            }
        }

        [ComVisible(false)]
        public void RegisterMarketDataProviders()
        {
            bool exists = true;
            markets = null;
            int i = 1;
            while (exists)
            {
                string key = "market_data_provider_" + i;
                exists = Config.IsDefined(key);
                if (exists)
                {
                    string provider = Config.Get("market_data_provider_" + i);
                    RegisterMarketDataProvider(provider, Config.Get("mdp_class_" + provider));
                }
                i++;
            }
            if (markets.Count > 0)
            {
                if (Config.IsDefined("default_market_data_provider"))
                {
                    market = markets[Config.Get("default_market_data_provider")];
                }
            }
        }

        [ComVisible(false)]
        public void RegisterAmericanPricer(String classname)
        {
            Log.Info("registering american pricer {0}", classname);
        }

        [ComVisible(false)]
        public void RegisterEuropeanPricer(String classname)
        {
            Log.Info("registering european pricer {0}", classname);
        }

        [ComVisible(false)]
        public void RegisterAmericanPricer()
        {
            string classname = Config.Get("american_pricer");
            RegisterAmericanPricer(classname);
        }

        [ComVisible(false)]
        public void RegisterEuropeanPricer()
        {
            string classname = Config.Get("european_pricer");
            RegisterEuropeanPricer(classname);
        }
        [ComVisible(false)]
        public void RegisterHistoricalVolatilityProvider()
        {
            string classname = Config.Get("historical_volatility_provider");
            RegisterHistoricalVolatilityProvider(classname);
        }
        [ComVisible(false)]
        public void RegisterHistoricalVolatilityProvider(string classname)
        {
            Log.Info("Registering Historical volatility provider");
            historicalVolProvider = CreateObject<IHistoricalVolatilityProvider>(classname);
        }
        [ComVisible(false)]
        public void ReadExchangeStatus()
        {
            string statusTemplate = "trade_flag";
            exchangeTradeStatus = new Dictionary<string, string[]>();
            for (int i = 0; i < Engine.Instance.Config.Count(); i++)
            {
                string key = Engine.Instance.Config.Key(i);
                if (key.Length > statusTemplate.Length && key.Substring(0, statusTemplate.Length) == statusTemplate)
                {
                    exchangeTradeStatus.Add(key.Split('_')[2], Config.Get(key).Split(','));
                }
            }
        }

        [ComVisible(false)]
        public void ReadKxSpecialFxRics()
        {
            string statusTemplate = "kx_fx_ric";
            kxSpecialFxRics = new Dictionary<string, string>();
            for (int i = 0; i < Engine.Instance.Config.Count(); i++)
            {
                string key = Engine.Instance.Config.Key(i);
                if (key.Length > statusTemplate.Length && key.Substring(0, statusTemplate.Length) == statusTemplate)
                {
                    kxSpecialFxRics.Add(key.Split('_')[3], Config.Get(key));
                }
            }
        }

        [ComVisible(false)]
        public void ReadOisDiscountCurves()
        {
            string statusTemplate = "ois_discounting_curve";
            oisDiscountCirves = new Dictionary<string, string[]>();
            for (int i = 0; i < Engine.Instance.Config.Count(); i++)
            {
                string key = Engine.Instance.Config.Key(i);
                if (key.Length > statusTemplate.Length && key.Substring(0, statusTemplate.Length) == statusTemplate)
                {
                    oisDiscountCirves.Add(key.Split('_')[3], Config.Get(key).Split(','));
                }
            }
        }

        [ComVisible(false)]
        public void ReadONDriftCompoundingCurveFamilies()
        {
            string statusTemplate = "on_drift_compounding_family_name";
            ONDriftCmpCurveFamilyIDs = new Dictionary<int, int>();
            for (int i = 0; i < Engine.Instance.Config.Count(); i++)
            {
                string key = Engine.Instance.Config.Key(i);
                if (key.Length > statusTemplate.Length && key.Substring(0, statusTemplate.Length) == statusTemplate)    //e.g. on_drift_compounding_family_name_EUR 
                {
                    int currencyCode = Engine.Instance.Factory.GetCurrencyCodeForName(key.Split('_')[5]);
                    int familyCode = Engine.Instance.Factory.GetYieldCurveFamilyCodeForName(Config.Get(key), currencyCode);
                    ONDriftCmpCurveFamilyIDs.Add(currencyCode, familyCode);
                }
            }
        }

        [ComVisible(false)]
        public void ReadFxFwdDiscountCurves()
        {
            string statusTemplate = "fx_fwd_discounting_curve_family_name";
            for (int i = 0; i < Engine.Instance.Config.Count(); i++)
            {
                string key = Engine.Instance.Config.Key(i);
                if (key.Length == statusTemplate.Length && key.Substring(0, statusTemplate.Length) == statusTemplate)
                {
                    fxFwdDiscountFamilyCurveName = Config.Get(key);
                }
            }
        }

        [ComVisible(false)]
        public ISession Session
        {
            get
            {
                if (_session == null)
                {
                    _session = factory.OpenSession();
                }
                return _session;
            }
        }

        public void DisposeSession()
        {
            lock (this)
            {
                lock (Engine.Instance.Factory.thisLock)
                {
                    if (_session != null)
                    {
                        try
                        {
                            _session.Close();
                        }
                        catch
                        {
                        }
                        _session = null;
                    }
                }
            }
        }

        public DataFactory Factory { get; set; }

        [ComVisible(false)]
        public IDictionary<string, IQuoteProvider> QuoteProviders { get; set; }

        [ComVisible(false)]
        public IDictionary<string, IQuoteProvider> QuoteProvidersWithoutBrokers
        {
            get
            {
                IDictionary<string, IQuoteProvider> retval = new Dictionary<string, IQuoteProvider>();
                foreach (var item in QuoteProviders.Where(x => x.Value.Name != "mysql" && x.Value.Name != "kx_cscreen"))
                    retval.Add(item.Key, item.Value);

                return retval;
            }
        }


        public QuoteProviders Sources
        {
            get
            {
                return new QuoteProviders(QuoteProviders.Values.ToList());
            }
            set
            {
                QuoteProviders = new Dictionary<string, IQuoteProvider>();
            }
        }

        [ComVisible(false)]
        public void RegisterQuoteProviders()
        {
            QuoteProviders = new Dictionary<string, IQuoteProvider>();


            IQuoteProvider provider;

            if (Config.IsDefined("use_kx_for_cscreen"))
            {
                if (Convert.ToBoolean(Config.Get("use_kx_for_cscreen")))
                {
                    Log.Info("Cscreen Quotes retrival via kx activated via config. No Mysql will be used anymore!");
                    Log.Info("registering kx-cscreen provider");
                    QuoteProviders.Add("", null);
                }
                else
                {
                    Log.Info("registering mysql provider");
                    provider = null;
                    QuoteProviders.Add(provider.Name, provider);
                }
            }
            else
            {
                Log.Info("registering mysql provider");
                provider = null;
                QuoteProviders.Add(provider.Name, provider);
            }

            Log.Info("registering kx-listed provider");
            provider = null;
            QuoteProviders.Add(provider.Name, provider);

            Log.Info("registering kx-manual provider");
            provider = null;
            QuoteProviders.Add(provider.Name, provider);

            Log.Info("registering auto spreadff provider");
            provider = null;
            QuoteProviders.Add(provider.Name, provider);
        }

        public void AddQuoteProvider(IQuoteProvider provider)
        {
            QuoteProviders.Add(provider.Name, provider);
        }

        public VolsurfaceProcessing.Processor GetVolsurfaceProcessor(Underlying und, VolsurfaceModel defaultmodel)
        {
            return new VolsurfaceProcessing.Processor(und, defaultmodel);
        }

        public VolsurfaceProcessing.Processor GetVolsurfaceProcessor(Underlying und)
        {
            return new VolsurfaceProcessing.Processor(und);
        }

        public VolsurfaceModelling.IVolsurfaceModel GetVolsurfaceComputer(Volsurface surface)
        {
            VolsurfaceModelling.IVolsurfaceModel retval =
                VolsurfaceModelling.VolsurfaceModel.CreateInstance(surface.VolsurfaceModel);

            retval.Volsurface = surface;
            return retval;
        }

        public VolsurfaceModelling.IVolsurfaceModel GetVolsurfaceComputer(VolsurfaceModel description)
        {
            VolsurfaceModelling.IVolsurfaceModel retval =
                VolsurfaceModelling.VolsurfaceModel.CreateInstance(description);

            return retval;
        }

        public void Save([MarshalAs(UnmanagedType.IDispatch)] Object obj)
        {
            Session.Save(obj);
        }

        public void SaveOrUpdate([MarshalAs(UnmanagedType.IDispatch)] Object obj)
        {
            Session.SaveOrUpdate(obj);
        }

        public void Delete([MarshalAs(UnmanagedType.IDispatch)] Object obj)
        {
            Session.Delete(obj);
        }

        public void Flush()
        {
            Session.Flush();
        }

        public void Refresh([MarshalAs(UnmanagedType.IDispatch)] Object obj)
        {
            Session.Refresh(obj);
        }

        public void Evict([MarshalAs(UnmanagedType.IDispatch)] Object obj)
        {
            Session.Evict(obj);
        }

        public Object Reload([MarshalAs(UnmanagedType.IDispatch)] Object obj)
        {
            Object retval = null;
            try
            {
                if (obj == null)
                    return retval;

                Type type = obj.GetType();

                int id = (int)Session.GetIdentifier(obj);

                Session.Evict(obj);
                retval = Session.Load(type, id);
            }
            catch (Exception)
            {
                retval = obj;
            }
            return retval;
        }

        public bool ForceRTChannelConnection
        {
            get;
            set;
        }

        public bool ConnectionWithMarketData
        {
            get; set;
        }

        public bool ConnectionWithKX
        {
            get;
            set;
        }

        [ComVisible(false)]
        public T GetConfig<T>(string name)
        {
            return GetConfig<T>(name, default(T));
        }

        [ComVisible(false)]
        public T GetConfig<T>(string name, T defaultSetting)
        {
            if (!Config.IsDefined(name))
            {
                return defaultSetting;
            }

            try
            {
                var retVal = (T)Convert.ChangeType(Config.Get(name), typeof(T));
                if (typeof(T) == typeof(string) && retVal == null)
                    return defaultSetting;
                return retVal;
            }
            catch
            {
                return defaultSetting;
            }
        }

        public object PermissionsValidator
        {
            get;
            internal set;
        }

        [ComVisible(false)]
        public void SqlInsert(string sql)
        {
            lock (Engine.Instance.Factory.thisLock)
            {
                Session.CreateSQLQuery(sql).ExecuteUpdate();
            }
        }

        [ComVisible(false)]
        public void ChangeDefaultMarketDataProvider(string providerClassName)
        {
            RegisterMarketDataProvider("Manual_Changed", providerClassName);
            market = markets["Manual_Changed"];
        }

        //
        [ComVisible(false)]
        public void RegisterCalendarProviders()
        {
            bool exists = true;
            int i = 1;
            while (exists)
            {
                string key = "calendar_provider_" + i;
                exists = Config.IsDefined(key);
                if (exists)
                {
                    string calendar = Config.Get("calendar_provider_" + i);
                    try
                    {
                        RegisterCalendarProvider(calendar, Config.Get("cal_class_" + calendar));
                    }
                    catch
                    {
                    }
                }
                i++;
            }
        }


        [ComVisible(false)]
        public void RegisterCalendarProvider(String calendar, String classname)
        {
            try
            {
                Log.Info("registering Calendar provider {0} for source {1}", classname, calendar);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public void LogInfo(string info)
        {
            Log.Info(info);
        }
    }

    public class QuoteProviders
    {
        private List<IQuoteProvider> quoteProviders;

        public QuoteProviders(List<IQuoteProvider> quoteProviders)
        {
            this.quoteProviders = quoteProviders;
        }
    }

    public interface IQuoteProvider
    {
        string Name { get; set; }
    }

    public interface IMarketData
    {
    }

    public class KxStorageConnection
    {
    }

    public interface IHistoricalVolatilityProvider
    {
    }

    [ComVisible(false)]
    public class NLogConfigGuard : IDisposable
    {
        public void Dispose()
        {
#if SOPHIS_7
            try
            {
                LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

                LogManager.ReconfigExistingLoggers();
            }
            catch (Exception ex)
            {
                Engine.Instance.ErrorException("error during reload of nlog config", ex);
            }
#endif
        }
    }



}
