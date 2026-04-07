using System;
using System.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
using NLog;


namespace Puma.MDE
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("520C0EBE-7BCA-431f-9AD6-85A92DD11B65")]
    [ComVisible(true)]
    public sealed class Engine
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
            }

            static public String Get(String key)
            {
                KeyValueConfigurationElement elem = config.AppSettings.Settings[key];
                if (elem == null)
                    throw new Exception("setting " + key + " not found");
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

        Logger log = LogManager.GetLogger("Example");
        static readonly Engine instance = new Engine();

        private Engine()
        {
            Today = DateTime.Today;

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
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

        [ComVisible(false)]
        public Logger Log
        {
            get
            {
                return log;
            }
        }


        public void InfoException(string message, Exception ex)
        {
#if SOPHIS_7
            Log.Info(ex, message);
#else
            Log.InfoException(message, ex);
#endif
        }


        public void ErrorException(string message, Exception ex)
        {
#if SOPHIS_7
            Log.Error(ex, message);
#else
            Log.ErrorException(message, ex);
#endif
        }

        public void WarnException(string message, Exception ex)
        {
#if SOPHIS_7
            Log.Warn(ex, message);
#else
            Log.WarnException(message, ex);
#endif
        }

        public void FatalException(string message, Exception ex)
        {
#if SOPHIS_7
            Log.Fatal(ex, message);
#else
            Log.FatalException(message, ex);
#endif
        }

        public void DebugException(string message, Exception ex)
        {
#if SOPHIS_7
            Log.Debug(ex, message);
#else
            Log.DebugException(message, ex);
#endif
        }

        public void LogError(string message, Exception ex)
        {
#if SOPHIS_7
            Log.Error(ex, message);
#else
            Log.Error(message, ex);
#endif
        }



        public string GetHistomvtsName()
        {
#if SOPHIS_7
            return "JOIN_POSITION_HISTOMVTS";
#else
            return "HISTOMVTS";
#endif
        }

        public DateTime Today { get; set; }

        static System.Reflection.Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            System.Reflection.Assembly a = null;

            string serializationAssemblyPartialName = "PumaMDE";
            string serializationAssemblyPartialNameWinUI = "PumaMDEWinUI";
            string assemblyPartialNameHibernate = "nhibernate,";

            if (args.Name.IndexOf(assemblyPartialNameHibernate, StringComparison.InvariantCultureIgnoreCase) != -1)
                return System.Reflection.Assembly.GetAssembly(typeof(NHibernate.ISession));

            if (args.Name.IndexOf(serializationAssemblyPartialName, StringComparison.InvariantCultureIgnoreCase) != -1 &&
               args.Name.IndexOf(serializationAssemblyPartialNameWinUI, StringComparison.InvariantCultureIgnoreCase) == -1)
            {
                return System.Reflection.Assembly.GetExecutingAssembly();
            }

            return a;
        }

        [ComVisible(false)]
        public void LogDebugArray(string name, double[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                log.Debug(String.Format("{0}[{1}]={2}", name, i, arr[i]));
        }
        [ComVisible(false)]
        public void LogDebugArray(string name, int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                log.Debug(String.Format("{0}[{1}]={2}", name, i, arr[i]));
        }
        [ComVisible(false)]
        public void LogDebugArray(string name, long[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                log.Debug(String.Format("{0}[{1}]={2}", name, i, arr[i]));
        }

        [ComVisible(false)]
        public void LogDebugValue(string name, object val)
        {
            log.Debug(String.Format("{0}={1}", name, val));
        }

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
