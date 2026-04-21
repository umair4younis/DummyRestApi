using Example.OPUS.OrderImport;
using Puma.MDE.OPUS.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSftpOrderFileDownloader
    {
        public DownloadedOrderFile DownloadLatestFile(OpusSftpOrderImportConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Engine.Instance.Log.Info(string.Format(
                "[OpusSftpOrderFileDownloader] Download request started. Host={0}, Port={1}, RemoteDirectorySet={2}, RemoteFilePathSet={3}, Pattern={4}",
                configuration.Host,
                configuration.Port,
                !string.IsNullOrWhiteSpace(configuration.RemoteDirectory),
                !string.IsNullOrWhiteSpace(configuration.RemoteFilePath),
                configuration.FilePattern));

            RetryPolicy retryPolicy = configuration.RetryPolicy ?? new RetryPolicy();
            if (retryPolicy.IsRetryable == null)
                retryPolicy.IsRetryable = IsRetryable;

            return OpusOrderImportRetryExecutor.ImportRetryExecute(
                () => DownloadLatestFileOnce(configuration),
                "Download OPUS SFTP order file",
                retryPolicy);
        }

        private static DownloadedOrderFile DownloadLatestFileOnce(OpusSftpOrderImportConfiguration configuration)
        {
            ConnectionInfo connectionInfo = BuildConnectionInfo(configuration);

            using (SftpClient client = new SftpClient(connectionInfo))
            {
                Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Connecting to OPUS SFTP endpoint.");
                client.Connect();
                if (!client.IsConnected)
                    throw new SshConnectionException("Could not connect to the OPUS SFTP endpoint.");

                Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Connected to OPUS SFTP endpoint.");

                ISftpFile remoteFile = ResolveRemoteFile(client, configuration);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Engine.Instance.Log.Info("[OpusSftpOrderFileDownloader] Downloading file: " + remoteFile.FullName);
                    client.DownloadFile(remoteFile.FullName, memoryStream);

                    Engine.Instance.Log.Info(string.Format(
                        "[OpusSftpOrderFileDownloader] Download complete. FileName={0}, Bytes={1}, LastWriteTimeUtc={2}",
                        remoteFile.Name,
                        memoryStream.Length,
                        remoteFile.LastWriteTimeUtc.ToString("o")));

                    return new DownloadedOrderFile
                    {
                        FileName = remoteFile.Name,
                        RemotePath = remoteFile.FullName,
                        Content = memoryStream.ToArray(),
                        LastWriteTimeUtc = remoteFile.LastWriteTimeUtc
                    };
                }
            }
        }

        private static ConnectionInfo BuildConnectionInfo(OpusSftpOrderImportConfiguration configuration)
        {
            List<AuthenticationMethod> authenticationMethods = new List<AuthenticationMethod>();

            if (!string.IsNullOrWhiteSpace(configuration.Password))
            {
                Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Password authentication is configured.");
                authenticationMethods.Add(new PasswordAuthenticationMethod(configuration.Username, configuration.Password));
            }

            if (!string.IsNullOrWhiteSpace(configuration.PrivateKeyPath))
            {
                Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Private key authentication is configured.");
                string privateKeyPath = ResolvePath(configuration.PrivateKeyPath);
                PrivateKeyFile privateKey = string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(privateKeyPath)
                    : new PrivateKeyFile(privateKeyPath, configuration.PrivateKeyPassphrase);

                authenticationMethods.Add(new PrivateKeyAuthenticationMethod(configuration.Username, privateKey));
            }

            if (authenticationMethods.Count == 0)
                throw new InvalidOperationException("No valid authentication method was configured for the OPUS SFTP connection.");

            Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Connection info built with " + authenticationMethods.Count + " authentication method(s).");

            return new ConnectionInfo(configuration.Host, configuration.Port, configuration.Username, authenticationMethods.ToArray());
        }

        private static ISftpFile ResolveRemoteFile(SftpClient client, OpusSftpOrderImportConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.RemoteFilePath))
            {
                Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Using explicit remote file path: " + configuration.RemoteFilePath);
                if (!client.Exists(configuration.RemoteFilePath))
                    throw new FileNotFoundException("Configured OPUS SFTP file was not found.", configuration.RemoteFilePath);

                return client.Get(configuration.RemoteFilePath);
            }

            Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Selecting latest file from directory using pattern matching.");
            IEnumerable<ISftpFile> candidates = client
                .ListDirectory(configuration.RemoteDirectory)
                .Where(file => !file.IsDirectory && !file.IsSymbolicLink && WildcardMatch(file.Name, configuration.FilePattern));

            ISftpFile latest = candidates
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name)
                .FirstOrDefault();

            if (latest == null)
                throw new FileNotFoundException("No OPUS order file matched the configured file pattern.", configuration.FilePattern);

            Engine.Instance.Log.Info("[OpusSftpOrderFileDownloader] Latest matching file selected: " + latest.FullName);

            return latest;
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
            return ex is SshConnectionException
                || ex is SshOperationTimeoutException
                || ex is SocketException
                || ex is IOException;
        }
    }
}