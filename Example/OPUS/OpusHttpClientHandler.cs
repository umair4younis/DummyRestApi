using Puma.MDE.OPUS.Tests;
using Puma.MDE.OPUS.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;


namespace Puma.MDE.OPUS
{
    public class OpusHttpClientHandler
    {
        private readonly OpusConfiguration _opusConfiguration;
        public readonly HttpClientHandler _opusHttpClientHandler;

        public OpusHttpClientHandler(OpusConfiguration opusConfiguration)
        {
            _opusConfiguration = opusConfiguration ?? throw LogAndCreateNullException(
                nameof(opusConfiguration), "OpusConfiguration is required but was null");

            Engine.Instance.Log.Info("[OpusHttpClientHandler] Initializing HTTP client handler");

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                Engine.Instance.Log.Debug("[OpusHttpClientHandler] TLS 1.2 enabled");
            }
            catch
            {
                // Fallback for older runtimes that don't know Tls12 enum member
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                Engine.Instance.Log.Debug("[OpusHttpClientHandler] TLS 1.2 enabled via fallback (3072)");
            }

            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 50;
            Engine.Instance.Log.Debug("[OpusHttpClientHandler] ServicePointManager configured: Expect100Continue=false, MaxConnections=50");

            var proxy = new WebProxy(_opusConfiguration.ProxyUrl, BypassOnLocal: true)
            {
                UseDefaultCredentials = true
            };
            Engine.Instance.Log.Info($"[OpusHttpClientHandler] Proxy configured: {_opusConfiguration.ProxyUrl}");

            // ────────────────────────────────────────────────────────────────
            // NEW: Skip certificate loading for tests
            // ────────────────────────────────────────────────────────────────
            if (_opusConfiguration is FakeOpusConfiguration fakeConfig &&
                fakeConfig.SkipCertificateLoadingForTests)
            {
                Engine.Instance.Log.Info("[OpusHttpClientHandler] TEST MODE - Skipping client certificate loading");
                _opusHttpClientHandler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = proxy,
                    PreAuthenticate = true,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                Engine.Instance.Log.Info("[OpusHttpClientHandler] TEST MODE - Handler created without client cert");
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
                    Engine.Instance.Log.Debug($"[OpusHttpClientHandler] Converted relative cert path to absolute: {certPath}");
                }

                // Normalize (removes .. / \ etc.)
                certPath = Path.GetFullPath(certPath);
                Engine.Instance.Log.Debug($"[OpusHttpClientHandler] Normalized certificate path: {certPath}");
            }
            else
            {
                string exceptionMessage = "Client certificate path is empty or null";

                Engine.Instance.Log.Error($"[OpusHttpClientHandler] {exceptionMessage}");
                throw new ArgumentException(exceptionMessage, nameof(_opusConfiguration.ClientCertPath));
            }

            Engine.Instance.Log.Info($"[OpusHttpClientHandler] Loading certificate from: {certPath}");

            X509Certificate2 clientCert;
            try
            {
                if (!File.Exists(certPath))
                {
                    Engine.Instance.Log.Error($"[OpusHttpClientHandler] Certificate file does not exist: {certPath}");
                    throw new FileNotFoundException($"Certificate file not found: {certPath}");
                }

                clientCert = new X509Certificate2(
                    certPath,
                    _opusConfiguration.ClientCertPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
                );
                Engine.Instance.Log.Info($"[OpusHttpClientHandler] Client certificate loaded successfully: Subject={clientCert.SubjectName.Name}, Thumbprint={clientCert.Thumbprint}");
            }
            catch (Exception ex)
            {
                string exceptionMessage =
                    $"Failed to load client certificate from '{certPath}'. " +
                    $"Check path, file existence, permissions and password. " +
                    $"Inner: {ex.Message}";

                Engine.Instance.Log.Error($"[OpusHttpClientHandler] {exceptionMessage}");
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
            Engine.Instance.Log.Info("[OpusHttpClientHandler] HttpClientHandler created with proxy, preauth, and client certificate");
        }

        public static OpusOperationResult<OpusHttpClientHandler> TryCreate(OpusConfiguration opusConfiguration)
        {
            try
            {
                return OpusOperationResult<OpusHttpClientHandler>.SuccessWithData(new OpusHttpClientHandler(opusConfiguration));
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error("[OpusHttpClientHandler.TryCreate] Failed: " + ex.ToString());
                return OpusOperationResult<OpusHttpClientHandler>.FailureWithData(
                    "Unable to initialize OPUS HTTP handler.",
                    ex.Message);
            }
        }

        private ArgumentNullException LogAndCreateNullException(string paramName, string message)
        {
            Engine.Instance.Log.Error("{Message} (Parameter: {Parameter})", message, paramName);
            return new ArgumentNullException(paramName, message);
        }
    }
}