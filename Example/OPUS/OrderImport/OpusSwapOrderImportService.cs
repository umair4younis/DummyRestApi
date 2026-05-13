using Puma.MDE.Data;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSwapOrderImportService
    {
        private readonly OpusSftpOrderImportConfiguration _configuration;
        private readonly Func<OpusSftpOrderImportConfiguration, DownloadedOrderFile> _fileDownloader;
        private readonly Func<byte[], string, SwapAccount, OpusSftpOrderImportConfiguration, SwapRawOrder> _fileParser;
        private readonly ISwapOrderImportGateway _importGateway;
        private readonly RetryPolicy _retryPolicy;
        private readonly OpusCircuitBreaker _orderImportCircuitBreaker;

        public OpusSwapOrderImportService(
            OpusSftpOrderImportConfiguration configuration = null,
            Func<OpusSftpOrderImportConfiguration, DownloadedOrderFile> fileDownloader = null,
            Func<byte[], string, SwapAccount, OpusSftpOrderImportConfiguration, SwapRawOrder> fileParser = null,
            ISwapOrderImportGateway importGateway = null,
            OpusCircuitBreaker orderImportCircuitBreaker = null)
        {
            _configuration = configuration ?? OpusSftpOrderImportConfiguration.FromAppSettings();
            _retryPolicy = _configuration.RetryPolicy ?? new RetryPolicy();
            if (_retryPolicy.IsRetryable == null)
                _retryPolicy.IsRetryable = IsRetryable;

            _orderImportCircuitBreaker = orderImportCircuitBreaker ?? new OpusCircuitBreaker(
                _configuration.CircuitBreakerFailureThreshold,
                _configuration.CircuitBreakerBreakSeconds,
                _configuration.CircuitBreakerRetries);

            if (fileDownloader == null)
            {
                Engine.Instance.Log.Info("[OpusSwapOrderImportService] Running startup validation for default SFTP downloader.");
                OpusSftpOrderFileDownloader.ValidateStartupCompatibility(_configuration);
            }

            _fileDownloader = fileDownloader ?? new OpusSftpOrderFileDownloader().DownloadLatestFile;
            _fileParser = fileParser ?? new OpusSwapOrderFileParser().Parse;
            _importGateway = importGateway ?? new OrderManagerImportGateway();

            Engine.Instance.Log.Debug(string.Format(
                "[OpusSwapOrderImportService] Import service initialized. RetryMax={0}, CircuitBreakerType={1}, CBThreshold={2}, CBBreakSeconds={3}, CBRetries={4}",
                _retryPolicy.MaxRetries,
                _orderImportCircuitBreaker.GetType().Name,
                _configuration.CircuitBreakerFailureThreshold,
                _configuration.CircuitBreakerBreakSeconds,
                _configuration.CircuitBreakerRetries));
        }

        public void GetFullUnwindOrder(SwapAccount selectedAccount, out SwapRawOrder fullUnwind)
        {
            if (selectedAccount == null)
                throw new ArgumentNullException(nameof(selectedAccount));

            Engine.Instance.Log.Info("[OpusSwapOrderImportService] Building full unwind order for account: " + selectedAccount.AccountName);

            DownloadedOrderFile downloadedFile = DownloadOrderFileWithFallback();
            if (downloadedFile == null)
                throw new InvalidOperationException("No OPUS SFTP order file was downloaded.");

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderImportService] Downloaded order file received. FileName={0}, RemotePath={1}, SizeBytes={2}",
                downloadedFile.FileName,
                downloadedFile.RemotePath,
                downloadedFile.Content == null ? 0 : downloadedFile.Content.Length));

            fullUnwind = ExecuteWithRetryAndCircuitBreaker(
                () => _fileParser(downloadedFile.Content, downloadedFile.FileName, selectedAccount, _configuration),
                "Parse OPUS order file");
            if (fullUnwind == null)
                throw new InvalidOperationException("The OPUS SFTP order file could not be parsed into a SwapRawOrder.");

            if (string.IsNullOrWhiteSpace(fullUnwind.AccountName))
            {
                Engine.Instance.Log.Warn("[OpusSwapOrderImportService] Parsed order had no account name; using selected account.");
                fullUnwind.AccountName = selectedAccount.AccountName;
            }

            if (fullUnwind.Type == TypeManastOrder.None)
            {
                Engine.Instance.Log.Warn("[OpusSwapOrderImportService] Parsed order type was None; applying default configured type.");
                fullUnwind.Type = _configuration.DefaultOrderType;
            }

            fullUnwind.UpdatePoolUpon = _configuration.DefaultUpdatePoolUpon;

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderImportService] Parsed full unwind order from '{0}' with {1} trade rows.",
                downloadedFile.RemotePath ?? downloadedFile.FileName,
                fullUnwind.Trades.Count));
        }

        public void getFullUnwindOrder(SwapAccount selectedAccount, out SwapRawOrder fullUnwind)
        {
            GetFullUnwindOrder(selectedAccount, out fullUnwind);
        }

        public void ImportFullUnwindOrder(
            SwapAccount selectedAccount,
            out SwapRawOrder fullUnwind,
            out SwapOrder importedOrder,
            out List<OrderManager.Error> errors)
        {
            Engine.Instance.Log.Info("[OpusSwapOrderImportService] Import full unwind workflow started.");
            GetFullUnwindOrder(selectedAccount, out fullUnwind);
            SwapRawOrder orderToImport = fullUnwind;

            SwapOrder localImportedOrder = null;
            List<OrderManager.Error> localErrors = null;
            ExecuteWithRetryAndCircuitBreaker(
                () =>
                {
                    _importGateway.ImportOrder(orderToImport, out localImportedOrder, out localErrors);
                    return true;
                },
                "Import OPUS full unwind order");

            importedOrder = localImportedOrder;
            errors = localErrors;

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderImportService] Import full unwind workflow completed. ImportedOrderSet={0}, Errors={1}",
                importedOrder != null,
                errors == null ? 0 : errors.Count));
        }

        private DownloadedOrderFile DownloadOrderFileWithFallback()
        {
            Engine.Instance.Log.Info("[OpusSwapOrderImportService] Starting order-file acquisition with SFTP primary and local fallback strategy.");
            Exception sftpFailure = null;

            try
            {
                Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Attempting primary SFTP order-file download.");
                DownloadedOrderFile sftpFile = ExecuteWithCircuitBreaker(
                    () => _fileDownloader(_configuration),
                    "Download OPUS SFTP order file");

                if (sftpFile != null && sftpFile.Content != null && sftpFile.Content.Length > 0)
                {
                    Engine.Instance.Log.Info(string.Format(
                        "[OpusSwapOrderImportService] Primary SFTP download succeeded. FileName={0}, RemotePath={1}, SizeBytes={2}",
                        sftpFile.FileName,
                        sftpFile.RemotePath,
                        sftpFile.Content.Length));
                    return sftpFile;
                }

                Engine.Instance.Log.Warn("[OpusSwapOrderImportService] SFTP download returned no file content. Falling back to local OPUS order-import folder.");
            }
            catch (Exception ex)
            {
                if (IsTemporarilyUnavailableFailure(ex))
                {
                    Engine.Instance.Log.Warn("[OpusSwapOrderImportService] SFTP primary path is temporarily unavailable due to circuit breaker state. Skipping local fallback and rethrowing.");
                    throw;
                }

                sftpFailure = ex;
                Engine.Instance.Log.Warn("[OpusSwapOrderImportService] SFTP download failed. Falling back to local OPUS order-import folder. Reason: " + ex.Message);
            }

            try
            {
                Engine.Instance.Log.Info("[OpusSwapOrderImportService] Attempting local fallback file load.");
                DownloadedOrderFile fallbackFile = LoadLatestLocalFallbackFile();
                Engine.Instance.Log.Info(string.Format(
                    "[OpusSwapOrderImportService] Local fallback load succeeded. FileName={0}, FilePath={1}, SizeBytes={2}",
                    fallbackFile.FileName,
                    fallbackFile.RemotePath,
                    fallbackFile.Content == null ? 0 : fallbackFile.Content.Length));
                return fallbackFile;
            }
            catch (Exception fallbackEx)
            {
                if (sftpFailure == null)
                    throw;

                Engine.Instance.Log.Error("[OpusSwapOrderImportService] Both SFTP primary and local fallback acquisition failed.", fallbackEx);

                throw new InvalidOperationException(
                    "OPUS order import could not obtain a source file. SFTP download failed and local fallback file resolution also failed.",
                    new AggregateException(sftpFailure, fallbackEx));
            }
        }

        private DownloadedOrderFile LoadLatestLocalFallbackFile()
        {
            string configuredDirectory = string.IsNullOrWhiteSpace(_configuration.LocalFallbackDirectory)
                ? Path.Combine("OPUS", "OrderImportFallback")
                : _configuration.LocalFallbackDirectory;
            Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Local fallback directory from configuration: " + configuredDirectory);

            string localFallbackDirectory = ResolvePath(configuredDirectory);
            Directory.CreateDirectory(localFallbackDirectory);

            Engine.Instance.Log.Info("[OpusSwapOrderImportService] Using local fallback directory for order import: " + localFallbackDirectory);

            string fallbackFilePath = ResolveFallbackFilePath(localFallbackDirectory);
            Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Reading local fallback file: " + fallbackFilePath);
            byte[] content = File.ReadAllBytes(fallbackFilePath);

            if (content == null || content.Length == 0)
                throw new InvalidOperationException("Local fallback file is empty: " + fallbackFilePath);

            FileInfo fileInfo = new FileInfo(fallbackFilePath);
            Engine.Instance.Log.Debug(string.Format(
                "[OpusSwapOrderImportService] Local fallback file metadata. LastWriteTimeUtc={0:O}, SizeBytes={1}",
                fileInfo.LastWriteTimeUtc,
                fileInfo.Length));

            return new DownloadedOrderFile
            {
                FileName = fileInfo.Name,
                RemotePath = fileInfo.FullName,
                Content = content,
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc
            };
        }

        private string ResolveFallbackFilePath(string localFallbackDirectory)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.RemoteFilePath))
            {
                string explicitName = Path.GetFileName(_configuration.RemoteFilePath);
                string explicitPath = Path.Combine(localFallbackDirectory, explicitName);
                Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Local fallback explicit candidate path: " + explicitPath);

                if (File.Exists(explicitPath))
                {
                    Engine.Instance.Log.Info("[OpusSwapOrderImportService] Local fallback selected explicit file: " + explicitPath);
                    return explicitPath;
                }

                Engine.Instance.Log.Warn("[OpusSwapOrderImportService] Local fallback explicit file not found. Falling back to pattern selection. Path: " + explicitPath);
            }

            string effectivePattern = string.IsNullOrWhiteSpace(_configuration.FilePattern)
                ? "*"
                : _configuration.FilePattern;

            Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Local fallback pattern selection using pattern: " + effectivePattern);

            string[] candidates = Directory
                .GetFiles(localFallbackDirectory, effectivePattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
                .ToArray();

            Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Local fallback pattern candidate count: " + candidates.Length);

            if (candidates.Length == 0)
            {
                throw new FileNotFoundException(
                    "No local fallback order file matched the configured pattern.",
                    effectivePattern);
            }

            string selected = candidates[0];
            Engine.Instance.Log.Info("[OpusSwapOrderImportService] Local fallback selected latest matching file: " + selected);
            return selected;
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (Path.IsPathRooted(path))
            {
                Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Resolved absolute local fallback path: " + path);
                return path;
            }

            string resolvedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Resolved relative local fallback path to: " + resolvedPath);
            return resolvedPath;
        }

        private T ExecuteWithCircuitBreaker<T>(Func<T> operation, string operationName)
        {
            try
            {
                Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Executing circuit-breaker protected operation: " + operationName);
                return _orderImportCircuitBreaker.ExecuteAsync(
                    () => Task.FromResult(operation())).GetAwaiter().GetResult();
            }
            catch (CircuitBreakerOpenException ex)
            {
                string message = string.Format("{0} is temporarily unavailable because the order-import circuit breaker is open.", operationName);
                Engine.Instance.Log.Error("[OpusSwapOrderImportService] " + message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        private T ExecuteWithRetryAndCircuitBreaker<T>(Func<T> operation, string operationName)
        {
            try
            {
                Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Executing retry + circuit-breaker protected operation: " + operationName);
                return _orderImportCircuitBreaker.ExecuteAsync(
                    () => Task.FromResult(
                        OpusOrderImportRetryExecutor.ImportRetryExecute(operation, operationName, _retryPolicy)))
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CircuitBreakerOpenException ex)
            {
                string message = string.Format("{0} is temporarily unavailable because the order-import circuit breaker is open.", operationName);
                Engine.Instance.Log.Error("[OpusSwapOrderImportService] " + message, ex);
                throw new InvalidOperationException(message, ex);
            }
        }

        private static bool IsRetryable(Exception ex)
        {
            return ex is InvalidOperationException
                || ex is IOException
                || ex is SocketException
                || ex is TimeoutException;
        }

        private static bool IsTemporarilyUnavailableFailure(Exception ex)
        {
            if (ex == null)
                return false;

            if (ex is CircuitBreakerOpenException)
                return true;

            if (ex is InvalidOperationException &&
                ex.Message != null &&
                ex.Message.IndexOf("temporarily unavailable", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (ex.InnerException != null)
                return IsTemporarilyUnavailableFailure(ex.InnerException);

            return false;
        }
    }
}