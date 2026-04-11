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
