using Puma.MDE.Data;
using Puma.MDE.OPUS.Models;
using System;
using System.Configuration;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSftpOrderImportConfiguration
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string RemoteDirectory { get; set; }

        public string RemoteFilePath { get; set; }

        public string FilePattern { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string PrivateKeyPath { get; set; }

        public string PrivateKeyPassphrase { get; set; }

        public string SheetName { get; set; }

        public int HeaderRowIndex { get; set; }

        public TypeManastOrder DefaultOrderType { get; set; }

        public TypeManastUpdatePoolUpon DefaultUpdatePoolUpon { get; set; }

        public string DefaultCurrency { get; set; }

        public string DefaultInstrumentTypeName { get; set; }

        public double DefaultFxRate { get; set; }

        public double DefaultContractSize { get; set; }

        public RetryPolicy RetryPolicy { get; set; }

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
                PrivateKeyPath = AppSettings.Get("Opus.Sftp.PrivateKeyPath"),
                PrivateKeyPassphrase = AppSettings.Get("Opus.Sftp.PrivateKeyPassphrase"),
                SheetName = AppSettings.Get("Opus.OrderImport.SheetName"),
                HeaderRowIndex = Math.Max(1, AppSettings.GetInt("Opus.OrderImport.HeaderRowIndex", 1)),
                DefaultCurrency = AppSettings.Get("Opus.OrderImport.DefaultCurrency"),
                DefaultInstrumentTypeName = AppSettings.Get("Opus.OrderImport.DefaultInstrumentTypeName", "Stock"),
                DefaultFxRate = AppSettings.GetAs("Opus.OrderImport.DefaultFxRate", ParseDouble, 1.0),
                DefaultContractSize = AppSettings.GetAs("Opus.OrderImport.DefaultContractSize", ParseDouble, 1.0),
                RetryPolicy = new RetryPolicy
                {
                    MaxRetries = Math.Max(1, AppSettings.GetInt("Opus.OrderImport.Retry.MaxRetries", 3)),
                    BaseDelayMs = Math.Max(0, AppSettings.GetInt("Opus.OrderImport.Retry.BaseDelayMs", 1000)),
                    BackoffFactor = AppSettings.GetAs("Opus.OrderImport.Retry.BackoffFactor", ParseDouble, 2.0),
                    JitterMaxFactor = AppSettings.GetAs("Opus.OrderImport.Retry.JitterMaxFactor", ParseDouble, 0.5)
                }
            };

            configuration.DefaultOrderType = ParseEnum(AppSettings.Get("Opus.OrderImport.Type"), TypeManastOrder.Transaction);
            configuration.DefaultUpdatePoolUpon = ParseEnum(AppSettings.Get("Opus.OrderImport.UpdatePoolUpon"), TypeManastUpdatePoolUpon.eDoNotUpdate);

            Engine.Instance.Log.Info(string.Format(
                "[OpusSftpOrderImportConfiguration] Loaded config. Host={0}, Port={1}, RemoteDirectorySet={2}, RemoteFilePathSet={3}, FilePattern={4}, SheetName={5}, HeaderRowIndex={6}, AuthModes={7}",
                configuration.Host,
                configuration.Port,
                !string.IsNullOrWhiteSpace(configuration.RemoteDirectory),
                !string.IsNullOrWhiteSpace(configuration.RemoteFilePath),
                configuration.FilePattern,
                string.IsNullOrWhiteSpace(configuration.SheetName) ? "<default>" : configuration.SheetName,
                configuration.HeaderRowIndex,
                BuildAuthModesLabel(configuration)));

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

            Engine.Instance.Log.Debug("[OpusSftpOrderImportConfiguration] Configuration validation passed.");
        }

        private static string BuildAuthModesLabel(OpusSftpOrderImportConfiguration configuration)
        {
            bool hasPassword = !string.IsNullOrWhiteSpace(configuration.Password);
            bool hasPrivateKey = !string.IsNullOrWhiteSpace(configuration.PrivateKeyPath);

            if (hasPassword && hasPrivateKey)
                return "password+privateKey";

            if (hasPassword)
                return "password";

            if (hasPrivateKey)
                return "privateKey";

            return "none";
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            TEnum parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : fallback;
        }

        private static double ParseDouble(string value)
        {
            double result;
            if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
                throw new FormatException("Unable to parse double value '" + value + "'.");
            return result;
        }
    }
}