using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSftpOrderFileDownloader
    {
        private const string LogPrefix = "[OpusSftpOrderFileDownloader] ";

        public DownloadedOrderFile DownloadLatestFile(OpusSftpOrderImportConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            ValidateStartupCompatibility(configuration);

            Engine.Instance.Log.Info(string.Format(
                LogPrefix + "Download request started. Host={0}, Port={1}, RemoteDirectorySet={2}, RemoteFilePathSet={3}, Pattern={4}, AuthMode={5}",
                configuration.Host,
                configuration.Port,
                !string.IsNullOrWhiteSpace(configuration.RemoteDirectory),
                !string.IsNullOrWhiteSpace(configuration.RemoteFilePath),
                configuration.FilePattern,
                BuildAuthModeLabel(configuration)));

            RetryPolicy retryPolicy = configuration.RetryPolicy ?? new RetryPolicy();
            if (retryPolicy.IsRetryable == null)
                retryPolicy.IsRetryable = IsRetryable;

            OpusCircuitBreaker sftpCircuitBreaker = CreateOrderImportCircuitBreaker(configuration);

            try
            {
                return sftpCircuitBreaker
                    .ExecuteAsync(() => Task.FromResult(
                        OpusOrderImportRetryExecutor.ImportRetryExecute(
                            () => DownloadLatestFileOnce(configuration),
                            "Download OPUS SFTP order file",
                            retryPolicy)))
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CircuitBreakerOpenException ex)
            {
                Engine.Instance.Log.Error(LogPrefix + "SFTP download blocked by circuit breaker.", ex);
                throw new InvalidOperationException("OPUS SFTP download is temporarily unavailable because the SFTP circuit breaker is open.", ex);
            }
        }

        public static void ValidateStartupCompatibility(OpusSftpOrderImportConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Engine.Instance.Log.Info(LogPrefix + "Validating startup compatibility for SSH.NET-based OPUS SFTP import.");

            bool hasPassword = !string.IsNullOrWhiteSpace(configuration.Password);
            bool hasOtp = !string.IsNullOrWhiteSpace(configuration.Otp);
            bool hasPrivateKey = !string.IsNullOrWhiteSpace(configuration.PrivateKeyPath);

            if (!hasPassword && !hasPrivateKey)
            {
                string message =
                    "OPUS SFTP startup validation failed. Either Opus.Sftp.Password or Opus.Sftp.PrivateKeyPath must be configured for SSH.NET authentication.";
                Engine.Instance.Log.Error(LogPrefix + message);
                throw new ConfigurationErrorsException(message);
            }

            if (!hasPrivateKey && !string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase))
            {
                string message =
                    "OPUS SFTP startup validation failed. Opus.Sftp.PrivateKeyPassphrase is configured but Opus.Sftp.PrivateKeyPath is empty. Configure both values together for encrypted private-key authentication.";
                Engine.Instance.Log.Error(LogPrefix + message);
                throw new ConfigurationErrorsException(message);
            }

            if (hasPrivateKey)
            {
                string privateKeyPath = ResolvePath(configuration.PrivateKeyPath);
                if (!File.Exists(privateKeyPath))
                {
                    string message = "OPUS SFTP startup validation failed. The configured private key file was not found: '" + privateKeyPath + "'. Update Opus.Sftp.PrivateKeyPath to a readable key file before starting the import flow.";
                    Engine.Instance.Log.Error(LogPrefix + message);
                    throw new ConfigurationErrorsException(message);
                }

                Engine.Instance.Log.Info(LogPrefix + "Private-key authentication is configured and key file exists: " + privateKeyPath);
            }

            if (hasPassword && hasPrivateKey)
                Engine.Instance.Log.Info(LogPrefix + "Both password and private-key authentication are configured. SSH.NET will attempt multi-method authentication.");
            else if (hasPassword)
                Engine.Instance.Log.Info(LogPrefix + "Password-based authentication is configured.");
            else
                Engine.Instance.Log.Info(LogPrefix + "Private-key authentication is configured.");

            if (hasOtp)
                Engine.Instance.Log.Info(LogPrefix + "Dedicated OTP/PAS credential is configured for keyboard-interactive prompts.");

            string otpWarning = GetOtpUsageWarningIfAny(configuration);
            if (!string.IsNullOrWhiteSpace(otpWarning))
                Engine.Instance.Log.Warn(LogPrefix + otpWarning);

            Engine.Instance.Log.Info(LogPrefix + "Startup compatibility validation passed.");
        }

        private static DownloadedOrderFile DownloadLatestFileOnce(OpusSftpOrderImportConfiguration configuration)
        {
            using (SftpClient client = CreateSftpClient(configuration))
            {
                Engine.Instance.Log.Info(LogPrefix + "Connecting to SFTP host: " + configuration.Host + ":" + configuration.Port);
                client.Connect();

                if (!client.IsConnected)
                    throw new IOException("SSH.NET did not establish an SFTP connection.");

                Engine.Instance.Log.Info(LogPrefix + "SFTP connection established.");

                SftpFile remoteFile = ResolveRemoteFile(client, configuration);
                Engine.Instance.Log.Info(LogPrefix + "Downloading file: " + remoteFile.FullName);

                byte[] content;
                using (MemoryStream stream = new MemoryStream())
                {
                    client.DownloadFile(remoteFile.FullName, stream);
                    content = stream.ToArray();
                }

                DateTime? lastWriteTimeUtc = null;
                if (remoteFile.Attributes != null)
                    lastWriteTimeUtc = remoteFile.Attributes.LastWriteTime.ToUniversalTime();

                Engine.Instance.Log.Info(string.Format(
                    LogPrefix + "Download complete. FileName={0}, Bytes={1}, LastWriteTimeUtc={2}",
                    remoteFile.Name,
                    content.Length,
                    lastWriteTimeUtc.HasValue ? lastWriteTimeUtc.Value.ToString("O") : "<unknown>"));

                return new DownloadedOrderFile
                {
                    FileName = remoteFile.Name,
                    RemotePath = remoteFile.FullName,
                    Content = content,
                    LastWriteTimeUtc = lastWriteTimeUtc
                };
            }
        }

        private static SftpFile ResolveRemoteFile(SftpClient client, OpusSftpOrderImportConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.RemoteFilePath))
            {
                Engine.Instance.Log.Debug(LogPrefix + "Using explicit remote file path: " + configuration.RemoteFilePath);
                if (!client.Exists(configuration.RemoteFilePath))
                    throw new FileNotFoundException("Configured remote file path was not found on SFTP server.", configuration.RemoteFilePath);

                return client.Get(configuration.RemoteFilePath);
            }

            Engine.Instance.Log.Debug(LogPrefix + "Selecting latest file from directory using pattern matching.");
            string remoteDirectory = configuration.RemoteDirectory;
            IEnumerable<SftpFile> candidates = client
                .ListDirectory(remoteDirectory)
                .Where(file => file != null)
                .Where(file => !file.IsDirectory)
                .Where(file => !file.IsSymbolicLink)
                .Where(file => WildcardMatch(file.Name, configuration.FilePattern))
                .OrderByDescending(file => file.Attributes == null ? DateTime.MinValue : file.Attributes.LastWriteTime.ToUniversalTime())
                .ToList();

            SftpFile latest = candidates.FirstOrDefault();
            if (latest == null)
                throw new FileNotFoundException("No OPUS order file matched the configured file pattern in remote directory.", configuration.FilePattern);

            Engine.Instance.Log.Info(LogPrefix + "Latest matching file selected: " + latest.FullName);
            return latest;
        }

        private static SftpClient CreateSftpClient(OpusSftpOrderImportConfiguration configuration)
        {
            List<AuthenticationMethod> methods = new List<AuthenticationMethod>();
            string username = configuration.Username;
            bool hasPassword = !string.IsNullOrWhiteSpace(configuration.Password);
            bool hasOtp = !string.IsNullOrWhiteSpace(configuration.Otp);
            bool hasPrivateKey = !string.IsNullOrWhiteSpace(configuration.PrivateKeyPath);

            if (hasPrivateKey)
            {
                string privateKeyPath = ResolvePath(configuration.PrivateKeyPath);
                PrivateKeyFile privateKeyFile = string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(privateKeyPath)
                    : new PrivateKeyFile(privateKeyPath, configuration.PrivateKeyPassphrase);

                methods.Add(new PrivateKeyAuthenticationMethod(username, privateKeyFile));

                if (!string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase))
                    Engine.Instance.Log.Debug(LogPrefix + "Using encrypted private key authentication (private key + passphrase).");
                else
                    Engine.Instance.Log.Debug(LogPrefix + "Using private key authentication.");
            }

            if (hasPassword)
            {
                methods.Add(new PasswordAuthenticationMethod(username, configuration.Password));
                Engine.Instance.Log.Debug(LogPrefix + "Password authentication method enabled.");
            }

            if (hasPassword || hasOtp)
            {

                KeyboardInteractiveAuthenticationMethod keyboardInteractive = new KeyboardInteractiveAuthenticationMethod(username);
                keyboardInteractive.AuthenticationPrompt += (sender, args) =>
                {
                    foreach (AuthenticationPrompt prompt in args.Prompts)
                    {
                        string request = prompt.Request ?? string.Empty;
                        prompt.Response = ResolveInteractivePromptResponse(request, configuration.Password, configuration.Otp);
                    }
                };
                methods.Add(keyboardInteractive);

                Engine.Instance.Log.Debug(LogPrefix + "Keyboard-interactive authentication enabled with independent password/OTP prompt matching.");
            }

            ConnectionInfo connectionInfo = new ConnectionInfo(
                configuration.Host,
                configuration.Port,
                username,
                methods.ToArray())
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            return new SftpClient(connectionInfo);
        }

        private static string BuildAuthModeLabel(OpusSftpOrderImportConfiguration configuration)
        {
            bool hasPassword = !string.IsNullOrWhiteSpace(configuration.Password);
            bool hasOtp = !string.IsNullOrWhiteSpace(configuration.Otp);
            bool hasPrivateKey = !string.IsNullOrWhiteSpace(configuration.PrivateKeyPath);

            if (hasPassword && hasPrivateKey)
            {
                if (string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase))
                    return hasOtp ? "privateKey+password+otp" : "privateKey+password";

                return hasOtp ? "privateKey(passphrase)+password+otp" : "privateKey(passphrase)+password";
            }

            if (hasPrivateKey)
            {
                if (string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase))
                    return hasOtp ? "privateKey+otp" : "privateKey";

                return hasOtp ? "privateKey(passphrase)+otp" : "privateKey(passphrase)";
            }

            if (hasPassword)
                return hasOtp ? "password+otp" : "password";

            if (hasOtp)
                return "otp";

            return "none";
        }

        private static string ResolveInteractivePromptResponse(string promptRequest, string password, string otp)
        {
            string request = (promptRequest ?? string.Empty).ToLowerInvariant();
            bool looksLikeOtpPrompt = request.IndexOf("otp", StringComparison.Ordinal) >= 0
                || request.IndexOf("one-time", StringComparison.Ordinal) >= 0
                || request.IndexOf("token", StringComparison.Ordinal) >= 0
                || request.IndexOf("passcode", StringComparison.Ordinal) >= 0
                || Regex.IsMatch(request, "\\bpas\\b", RegexOptions.CultureInvariant)
                || request.IndexOf("pin", StringComparison.Ordinal) >= 0;

            if (looksLikeOtpPrompt)
                return string.IsNullOrWhiteSpace(otp) ? password : otp;

            bool looksLikePasswordPrompt = request.IndexOf("password", StringComparison.Ordinal) >= 0;
            if (looksLikePasswordPrompt)
                return string.IsNullOrWhiteSpace(password) ? otp : password;

            return string.IsNullOrWhiteSpace(password) ? otp : password;
        }

        private static string GetOtpUsageWarningIfAny(OpusSftpOrderImportConfiguration configuration)
        {
            if (configuration == null)
                return null;

            bool hasOtp = !string.IsNullOrWhiteSpace(configuration.Otp);
            if (!hasOtp)
                return null;

            if (configuration.ExpectKeyboardInteractiveAuth)
                return null;

            return "Opus.Sftp.Otp is configured, but Opus.Sftp.ExpectKeyboardInteractiveAuth is false. If the target server does not request keyboard-interactive prompts, OTP/PAS will be unused.";
        }

        private static OpusCircuitBreaker CreateOrderImportCircuitBreaker(OpusSftpOrderImportConfiguration configuration)
        {
            return new OpusCircuitBreaker(
                configuration.CircuitBreakerFailureThreshold,
                configuration.CircuitBreakerBreakSeconds,
                configuration.CircuitBreakerRetries);
        }

        private static bool WildcardMatch(string input, string pattern)
        {
            string effectivePattern = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;
            string regexPattern = "^" + Regex.Escape(effectivePattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(input ?? string.Empty, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        private static bool IsRetryable(Exception ex)
        {
            return ex is InvalidOperationException
                || ex is IOException
                || ex is SocketException
                || ex is SshConnectionException
                || ex is SshException
                || ex is SftpPermissionDeniedException
                || ex is TimeoutException;
        }
    }
}