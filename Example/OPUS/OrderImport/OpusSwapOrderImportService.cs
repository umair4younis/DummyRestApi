using Puma.MDE.Data;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;
using System.IO;
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

            DownloadedOrderFile downloadedFile = ExecuteWithCircuitBreaker(
                () => _fileDownloader(_configuration),
                "Download OPUS SFTP order file");
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

        private T ExecuteWithCircuitBreaker<T>(Func<T> operation, string operationName)
        {
            try
            {
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
    }
}