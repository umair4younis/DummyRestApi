using Example.OPUS.Tests;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;


namespace Example.OPUS
{
    public class OpusHttpClientHandler
    {
        private readonly OpusConfiguration _opusConfiguration;
        public readonly HttpClientHandler _opusHttpClientHandler;

        public OpusHttpClientHandler(OpusConfiguration opusConfiguration)
        {
            _opusConfiguration = opusConfiguration ?? throw LogAndCreateNullException(
                nameof(opusConfiguration), "OpusConfiguration is required but was null");

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // Fallback for older runtimes that don't know Tls12 enum member
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            }

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 50;

            var proxy = new WebProxy(_opusConfiguration.ProxyUrl, BypassOnLocal: true)
            {
                UseDefaultCredentials = true
            };

            // ────────────────────────────────────────────────────────────────
            // NEW: Skip certificate loading for tests
            // ────────────────────────────────────────────────────────────────
            if (_opusConfiguration is FakeOpusConfiguration fakeConfig &&
                fakeConfig.SkipCertificateLoadingForTests)
            {
                Engine.Instance.Log.Info("[TEST] Skipping client certificate loading");
                _opusHttpClientHandler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = proxy,
                    PreAuthenticate = true,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return;   // ← important: skip real cert loading
            }

            // ────────────────────────────────────────────────────────────────
            // Resolve certificate path (handles both relative and absolute)
            // ────────────────────────────────────────────────────────────────
            string certPath = _opusConfiguration.ClientCertPath;

            if (!string.IsNullOrWhiteSpace(certPath))
            {
                // If it's already absolute → keep it
                // If relative → combine with application base directory
                if (!Path.IsPathRooted(certPath))
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    certPath = Path.Combine(baseDir, certPath);
                }

                // Normalize (removes .. / \ etc.)
                certPath = Path.GetFullPath(certPath);
            }
            else
            {
                string exceptionMessage = "Client certificate path is empty or null";

                Engine.Instance.Log.Error(exceptionMessage);
                throw new ArgumentException(exceptionMessage, nameof(_opusConfiguration.ClientCertPath));
            }

            Engine.Instance.Log.Info($"Resolved certificate path: {certPath}");

            X509Certificate2 clientCert;
            try
            {
                clientCert = new X509Certificate2(
                    certPath,
                    _opusConfiguration.ClientCertPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
                );
            }
            catch (Exception ex)
            {
                string exceptionMessage =
                    $"Failed to load client certificate from '{certPath}'. " +
                    $"Check path, file existence, permissions and password. " +
                    $"Inner: {ex.Message}";

                Engine.Instance.Log.Error(exceptionMessage);
                throw new InvalidOperationException(exceptionMessage, ex);
            }

            _opusHttpClientHandler = new HttpClientHandler
            {
                UseProxy = true,
                Proxy = proxy,
                PreAuthenticate = true,
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            _opusHttpClientHandler.ClientCertificates.Add(clientCert);
        }

        private ArgumentNullException LogAndCreateNullException(string paramName, string message)
        {
            Engine.Instance.Log.Error("{Message} (Parameter: {Parameter})", message, paramName);
            return new ArgumentNullException(paramName, message);
        }
    }
}