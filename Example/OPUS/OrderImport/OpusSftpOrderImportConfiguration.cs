using Puma.MDE.Data;
using Puma.MDE.OPUS.Models;
using System;
using System.Configuration;
using System.IO;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSftpOrderImportConfiguration
    {
        private const string OrderImportPrefix = "Opus.OrderImport";

        public string Host { get; set; }

        public int Port { get; set; }

        public string RemoteDirectory { get; set; }

        public string RemoteFilePath { get; set; }

        public string FilePattern { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Otp { get; set; }

        public bool ExpectKeyboardInteractiveAuth { get; set; }

        public string PrivateKeyPath { get; set; }

        public string PrivateKeyPassphrase { get; set; }

        public string LocalFallbackDirectory { get; set; }

        public string SheetName { get; set; }

        public int HeaderRowIndex { get; set; }

        public TypeManastOrder DefaultOrderType { get; set; }

        public TypeManastUpdatePoolUpon DefaultUpdatePoolUpon { get; set; }

        public string DefaultCurrency { get; set; }

        public string DefaultInstrumentTypeName { get; set; }

        public double DefaultFxRate { get; set; }

        public double DefaultContractSize { get; set; }

        public RetryPolicy RetryPolicy { get; set; }

        public int CircuitBreakerFailureThreshold { get; set; }

        public int CircuitBreakerBreakSeconds { get; set; }

        public int CircuitBreakerRetries { get; set; }

        public OpusSftpOrderImportConfiguration()
        {
            Port = 22;
            FilePattern = "SwapRawOrder*.csv";
            HeaderRowIndex = 1;
            DefaultOrderType = TypeManastOrder.Transaction;
            DefaultUpdatePoolUpon = TypeManastUpdatePoolUpon.eDoNotUpdate;
            DefaultInstrumentTypeName = "Stock";
            DefaultFxRate = 1.0;
            DefaultContractSize = 1.0;
            RetryPolicy = new RetryPolicy();
            CircuitBreakerFailureThreshold = 3;
            CircuitBreakerBreakSeconds = 30;
            CircuitBreakerRetries = 0;
            ExpectKeyboardInteractiveAuth = false;
            LocalFallbackDirectory = Path.Combine("OPUS", "OrderImportFallback");
        }

        public static OpusSftpOrderImportConfiguration FromAppSettings()
        {
            Engine.Instance.Log.Info("[OpusSftpOrderImportConfiguration] Loading OPUS SFTP import configuration from app settings.");

            OpusSftpOrderImportConfiguration configuration = new OpusSftpOrderImportConfiguration
            {
                Host = AppSettings.GetRequired("Opus.Sftp.Host"),
                Port = AppSettings.GetInt("Opus.Sftp.Port", 22),
                RemoteDirectory = AppSettings.Get("Opus.Sftp.RemoteDirectory"),
                RemoteFilePath = AppSettings.Get("Opus.Sftp.RemoteFilePath"),
                FilePattern = AppSettings.Get("Opus.Sftp.FilePattern", "SwapRawOrder*.csv"),
                Username = AppSettings.GetRequired("Opus.Sftp.Username"),
                Password = AppSettings.Get("Opus.Sftp.Password"),
                Otp = AppSettings.Get("Opus.Sftp.Otp"),
                ExpectKeyboardInteractiveAuth = AppSettings.GetAs("Opus.Sftp.ExpectKeyboardInteractiveAuth", bool.Parse, false),
                PrivateKeyPath = AppSettings.Get("Opus.Sftp.PrivateKeyPath"),
                PrivateKeyPassphrase = AppSettings.Get("Opus.Sftp.PrivateKeyPassphrase"),
                LocalFallbackDirectory = AppSettings.Get("Opus.OrderImport.LocalFallbackDirectory", Path.Combine("OPUS", "OrderImportFallback")),
                SheetName = AppSettings.Get("Opus.OrderImport.SheetName"),
                HeaderRowIndex = Math.Max(1, AppSettings.GetInt("Opus.OrderImport.HeaderRowIndex", 1)),
                DefaultCurrency = AppSettings.Get("Opus.OrderImport.DefaultCurrency"),
                DefaultInstrumentTypeName = AppSettings.Get("Opus.OrderImport.DefaultInstrumentTypeName", "Stock"),
                DefaultFxRate = AppSettings.GetAs("Opus.OrderImport.DefaultFxRate", ParseDouble, 1.0),
                DefaultContractSize = AppSettings.GetAs("Opus.OrderImport.DefaultContractSize", ParseDouble, 1.0),
                RetryPolicy = new RetryPolicy
                {
                    MaxRetries = Math.Max(1, GetOrderImportInt("MaxRetries", "Retry.MaxRetries", 3)),
                    BaseDelayMs = Math.Max(0, GetOrderImportInt("BaseDelayMs", "Retry.BaseDelayMs", 1000)),
                    BackoffFactor = GetOrderImportDouble("BackoffFactor", "Retry.BackoffFactor", 2.0),
                    JitterMaxFactor = GetOrderImportDouble("JitterMaxFactor", "Retry.JitterMaxFactor", 0.5)
                },
                CircuitBreakerFailureThreshold = Math.Max(1, GetOrderImportInt("FailureThreshold", "CircuitBreaker.FailureThreshold", 3)),
                CircuitBreakerBreakSeconds = Math.Max(1, GetOrderImportInt("BreakSeconds", "CircuitBreaker.BreakSeconds", 30)),
                CircuitBreakerRetries = Math.Max(0, GetOrderImportInt("CircuitBreakerRetries", "CircuitBreaker.Retries", 0))
            };

            configuration.DefaultOrderType = ParseEnum(AppSettings.Get("Opus.OrderImport.Type"), TypeManastOrder.Transaction);
            configuration.DefaultUpdatePoolUpon = ParseEnum(AppSettings.Get("Opus.OrderImport.UpdatePoolUpon"), TypeManastUpdatePoolUpon.eDoNotUpdate);

            Engine.Instance.Log.Info(string.Format(
                "[OpusSftpOrderImportConfiguration] Loaded config. Host={0}, Port={1}, RemoteDirectorySet={2}, RemoteFilePathSet={3}, FilePattern={4}, SheetName={5}, HeaderRowIndex={6}, LocalFallbackDirectory={7}, AuthModes={8}, Retry(Max={9},BaseDelayMs={10},Backoff={11},Jitter={12}), CBThreshold={13}, CBBreakSeconds={14}, CBRetries={15}",
                configuration.Host,
                configuration.Port,
                !string.IsNullOrWhiteSpace(configuration.RemoteDirectory),
                !string.IsNullOrWhiteSpace(configuration.RemoteFilePath),
                configuration.FilePattern,
                string.IsNullOrWhiteSpace(configuration.SheetName) ? "<default>" : configuration.SheetName,
                configuration.HeaderRowIndex,
                configuration.LocalFallbackDirectory,
                BuildAuthModesLabel(configuration),
                configuration.RetryPolicy.MaxRetries,
                configuration.RetryPolicy.BaseDelayMs,
                configuration.RetryPolicy.BackoffFactor,
                configuration.RetryPolicy.JitterMaxFactor,
                configuration.CircuitBreakerFailureThreshold,
                configuration.CircuitBreakerBreakSeconds,
                configuration.CircuitBreakerRetries));

            configuration.Validate();
            return configuration;
        }

        public void Validate()
        {
            Engine.Instance.Log.Debug("[OpusSftpOrderImportConfiguration] Validating OPUS SFTP import configuration.");

            if (string.IsNullOrWhiteSpace(Host))
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: Opus.Sftp.Host is missing.");
                throw new ConfigurationErrorsException("Opus.Sftp.Host is missing.");
            }

            if (Port <= 0)
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: Opus.Sftp.Port must be greater than zero.");
                throw new ConfigurationErrorsException("Opus.Sftp.Port must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: Opus.Sftp.Username is missing.");
                throw new ConfigurationErrorsException("Opus.Sftp.Username is missing.");
            }

            if (string.IsNullOrWhiteSpace(RemoteDirectory) && string.IsNullOrWhiteSpace(RemoteFilePath))
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: both RemoteDirectory and RemoteFilePath are empty.");
                throw new ConfigurationErrorsException("Either Opus.Sftp.RemoteDirectory or Opus.Sftp.RemoteFilePath must be configured.");
            }

            if (string.IsNullOrWhiteSpace(Password) && string.IsNullOrWhiteSpace(PrivateKeyPath))
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: neither password nor private key authentication is configured.");
                throw new ConfigurationErrorsException("Either Opus.Sftp.Password or Opus.Sftp.PrivateKeyPath must be configured.");
            }

            if (CircuitBreakerFailureThreshold < 1)
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: Opus.OrderImport.FailureThreshold must be >= 1.");
                throw new ConfigurationErrorsException("Opus.OrderImport.FailureThreshold must be >= 1.");
            }

            if (CircuitBreakerBreakSeconds < 1)
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: Opus.OrderImport.BreakSeconds must be >= 1.");
                throw new ConfigurationErrorsException("Opus.OrderImport.BreakSeconds must be >= 1.");
            }

            if (CircuitBreakerRetries < 0)
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Validation failed: Opus.OrderImport.CircuitBreakerRetries must be >= 0.");
                throw new ConfigurationErrorsException("Opus.OrderImport.CircuitBreakerRetries must be >= 0.");
            }

            Engine.Instance.Log.Debug("[OpusSftpOrderImportConfiguration] Configuration validation passed.");
        }

        private static string BuildAuthModesLabel(OpusSftpOrderImportConfiguration configuration)
        {
            bool hasPassword = !string.IsNullOrWhiteSpace(configuration.Password);
            bool hasOtp = !string.IsNullOrWhiteSpace(configuration.Otp);
            bool hasPrivateKey = !string.IsNullOrWhiteSpace(configuration.PrivateKeyPath);

            if (hasPassword && hasPrivateKey)
                return hasOtp ? "password+otp+privateKey" : "password+privateKey";

            if (hasPassword)
                return hasOtp ? "password+otp" : "password";

            if (hasPrivateKey)
                return hasOtp ? "privateKey+otp" : "privateKey";

            if (hasOtp)
                return "otp";

            return "none";
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Engine.Instance.Log.Debug(string.Format(
                    "[OpusSftpOrderImportConfiguration] Enum value not provided for {0}. Using fallback={1}.",
                    typeof(TEnum).Name,
                    fallback));
                return fallback;
            }

            TEnum parsed;
            if (Enum.TryParse(value, true, out parsed))
                return parsed;

            Engine.Instance.Log.Warn(string.Format(
                "[OpusSftpOrderImportConfiguration] Unable to parse enum value '{0}' for {1}. Using fallback={2}.",
                value,
                typeof(TEnum).Name,
                fallback));
            return fallback;
        }

        private static int GetOrderImportInt(string simplifiedKeySuffix, string legacyKeySuffix, int fallback)
        {
            string simplifiedKey = OrderImportPrefix + "." + simplifiedKeySuffix;
            string legacyKey = OrderImportPrefix + "." + legacyKeySuffix;

            bool hasSimplified = ConfigurationManager.AppSettings[simplifiedKey] != null;
            bool hasLegacy = ConfigurationManager.AppSettings[legacyKey] != null;

            int value = AppSettings.GetInt(simplifiedKey, AppSettings.GetInt(legacyKey, fallback));
            string source = hasSimplified ? simplifiedKey : (hasLegacy ? legacyKey : "default");

            Engine.Instance.Log.Debug(string.Format(
                "[OpusSftpOrderImportConfiguration] Resolved int setting {0}/{1} from {2} with value={3}.",
                simplifiedKey,
                legacyKey,
                source,
                value));

            return value;
        }

        private static double GetOrderImportDouble(string simplifiedKeySuffix, string legacyKeySuffix, double fallback)
        {
            string simplifiedKey = OrderImportPrefix + "." + simplifiedKeySuffix;
            string legacyKey = OrderImportPrefix + "." + legacyKeySuffix;

            bool hasSimplified = ConfigurationManager.AppSettings[simplifiedKey] != null;
            bool hasLegacy = ConfigurationManager.AppSettings[legacyKey] != null;

            double value = AppSettings.GetAs(simplifiedKey, ParseDouble, AppSettings.GetAs(legacyKey, ParseDouble, fallback));
            string source = hasSimplified ? simplifiedKey : (hasLegacy ? legacyKey : "default");

            Engine.Instance.Log.Debug(string.Format(
                "[OpusSftpOrderImportConfiguration] Resolved double setting {0}/{1} from {2} with value={3}.",
                simplifiedKey,
                legacyKey,
                source,
                value));

            return value;
        }

        private static double ParseDouble(string value)
        {
            double result;
            if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            {
                Engine.Instance.Log.Error("[OpusSftpOrderImportConfiguration] Failed to parse double value: " + value);
                throw new FormatException("Unable to parse double value '" + value + "'.");
            }
            return result;
        }
    }
}