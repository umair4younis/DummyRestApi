using Example.OPUS.OrderImport;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSftpOrderFileDownloader
    {
        internal static Func<string> SftpExecutableResolver { get; set; } = ResolveSftpExecutablePath;

        public DownloadedOrderFile DownloadLatestFile(OpusSftpOrderImportConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            ValidateStartupCompatibility(configuration);

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

        public static void ValidateStartupCompatibility(OpusSftpOrderImportConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Engine.Instance.Log.Info("[OpusSftpOrderFileDownloader] Validating startup compatibility for OpenSSH-based OPUS SFTP import.");

            if (string.IsNullOrWhiteSpace(configuration.PrivateKeyPath))
            {
                string message =
                    "OPUS SFTP startup validation failed. The current no-NuGet implementation requires key-based OpenSSH authentication, but Opus.Sftp.PrivateKeyPath is not configured. Password-only SFTP is no longer supported in this implementation. Configure Opus.Sftp.PrivateKeyPath and ensure the key can be used by OpenSSH sftp.exe, or restore an SSH library-based implementation.";
                Engine.Instance.Log.Error("[OpusSftpOrderFileDownloader] " + message);
                throw new ConfigurationErrorsException(message);
            }

            string privateKeyPath = ResolvePath(configuration.PrivateKeyPath);
            if (!File.Exists(privateKeyPath))
            {
                string message = "OPUS SFTP startup validation failed. The configured private key file was not found: '" + privateKeyPath + "'. Update Opus.Sftp.PrivateKeyPath to a readable key file before starting the import flow.";
                Engine.Instance.Log.Error("[OpusSftpOrderFileDownloader] " + message);
                throw new ConfigurationErrorsException(message);
            }

            string sftpExecutablePath = SftpExecutableResolver();
            if (string.IsNullOrWhiteSpace(sftpExecutablePath) || !File.Exists(sftpExecutablePath))
            {
                string message =
                    "OPUS SFTP startup validation failed. OpenSSH sftp.exe could not be found. This no-NuGet implementation depends on the Windows OpenSSH Client. Install the OpenSSH Client feature and make sure sftp.exe is available on PATH, then restart the application before running the OPUS import.";
                Engine.Instance.Log.Error("[OpusSftpOrderFileDownloader] " + message);
                throw new ConfigurationErrorsException(message);
            }

            if (!string.IsNullOrWhiteSpace(configuration.Password))
                Engine.Instance.Log.Warn("[OpusSftpOrderFileDownloader] Password authentication is configured but will be ignored. OpenSSH key-based authentication will be used.");

            Engine.Instance.Log.Info("[OpusSftpOrderFileDownloader] Startup compatibility validation passed. Using sftp executable: " + sftpExecutablePath);
        }

        private static DownloadedOrderFile DownloadLatestFileOnce(OpusSftpOrderImportConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.PrivateKeyPassphrase))
                Engine.Instance.Log.Warn("[OpusSftpOrderFileDownloader] PrivateKeyPassphrase is configured. Ensure the key is unlocked via ssh-agent because sftp.exe does not accept passphrase arguments.");

            string remotePath = ResolveRemotePath(configuration);
            string localTempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + Path.GetExtension(remotePath));

            try
            {
                Engine.Instance.Log.Info("[OpusSftpOrderFileDownloader] Downloading file: " + remotePath);
                ExecuteSftpBatch(configuration, new[] { BuildGetCommand(remotePath, localTempFile) }, "download file");

                byte[] content = File.ReadAllBytes(localTempFile);
                DateTime? lastWriteTimeUtc = null;
                if (File.Exists(localTempFile))
                    lastWriteTimeUtc = File.GetLastWriteTimeUtc(localTempFile);

                Engine.Instance.Log.Info(string.Format(
                    "[OpusSftpOrderFileDownloader] Download complete. FileName={0}, Bytes={1}",
                    Path.GetFileName(remotePath),
                    content.Length));

                return new DownloadedOrderFile
                {
                    FileName = Path.GetFileName(remotePath),
                    RemotePath = remotePath,
                    Content = content,
                    LastWriteTimeUtc = lastWriteTimeUtc
                };
            }
            finally
            {
                TryDeleteFile(localTempFile);
            }
        }

        private static string ResolveRemotePath(OpusSftpOrderImportConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.RemoteFilePath))
            {
                Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Using explicit remote file path: " + configuration.RemoteFilePath);
                return configuration.RemoteFilePath;
            }

            Engine.Instance.Log.Debug("[OpusSftpOrderFileDownloader] Selecting latest file from directory using pattern matching.");
            string remotePattern = CombineRemotePath(configuration.RemoteDirectory, configuration.FilePattern);
            string[] output = ExecuteSftpBatch(
                configuration,
                new[] { "ls -1t \"" + remotePattern + "\"" },
                "list remote files");

            List<string> listedPaths = output
                .Select(line => (line ?? string.Empty).Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.StartsWith("sftp>", StringComparison.OrdinalIgnoreCase))
                .Where(line => line.IndexOf("Connecting to", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(line => line.IndexOf("connected", StringComparison.OrdinalIgnoreCase) < 0)
                .ToList();

            string latest = listedPaths.FirstOrDefault(path => WildcardMatch(Path.GetFileName(path), configuration.FilePattern));
            if (string.IsNullOrWhiteSpace(latest))
                throw new FileNotFoundException("No OPUS order file matched the configured file pattern.", configuration.FilePattern);

            Engine.Instance.Log.Info("[OpusSftpOrderFileDownloader] Latest matching file selected: " + latest);
            return latest;
        }

        private static string[] ExecuteSftpBatch(OpusSftpOrderImportConfiguration configuration, IEnumerable<string> commands, string operation)
        {
            string privateKeyPath = ResolvePath(configuration.PrivateKeyPath);
            if (!File.Exists(privateKeyPath))
                throw new FileNotFoundException("Configured private key file was not found.", privateKeyPath);

            string batchPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".sftp");
            try
            {
                File.WriteAllLines(batchPath, commands);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = SftpExecutableResolver(),
                    Arguments = string.Format(
                        "-P {0} -b \"{1}\" -i \"{2}\" -o BatchMode=yes -o StrictHostKeyChecking=accept-new {3}@{4}",
                        configuration.Port,
                        batchPath,
                        privateKeyPath,
                        configuration.Username,
                        configuration.Host),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Failed to start sftp process.");

                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string standardError = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new IOException(string.Format(
                            "sftp command failed while trying to {0}. ExitCode={1}. Error={2}",
                            operation,
                            process.ExitCode,
                            string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError));
                    }

                    string mergedOutput = (standardOutput ?? string.Empty) + Environment.NewLine + (standardError ?? string.Empty);
                    return mergedOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                }
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException("OpenSSH sftp.exe was not found on PATH. Install OpenSSH Client feature on Windows.", ex);
            }
            finally
            {
                TryDeleteFile(batchPath);
            }
        }

        private static string BuildGetCommand(string remotePath, string localPath)
        {
            return string.Format("get \"{0}\" \"{1}\"", remotePath, localPath);
        }

        private static string ResolveSftpExecutablePath()
        {
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string systemPath = string.IsNullOrWhiteSpace(windir) ? null : Path.Combine(windir, "System32", "OpenSSH", "sftp.exe");
            if (!string.IsNullOrWhiteSpace(systemPath) && File.Exists(systemPath))
                return systemPath;

            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] pathSegments = pathValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string segment in pathSegments)
            {
                string candidate = Path.Combine(segment.Trim().Trim('"'), "sftp.exe");
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string CombineRemotePath(string remoteDirectory, string filePattern)
        {
            string directory = (remoteDirectory ?? string.Empty).TrimEnd('/');
            string pattern = string.IsNullOrWhiteSpace(filePattern) ? "*" : filePattern;
            return directory + "/" + pattern;
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
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
                || ex is Win32Exception
                || ex is SocketException
                || ex is TimeoutException;
        }
    }
}