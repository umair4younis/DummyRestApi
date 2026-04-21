using Example.OPUS.OrderImport;
using Puma.MDE.Data;
using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OpusSwapOrderImportService
    {
        private readonly OpusSftpOrderImportConfiguration _configuration;
        private readonly Func<OpusSftpOrderImportConfiguration, DownloadedOrderFile> _fileDownloader;
        private readonly Func<byte[], string, SwapAccount, OpusSftpOrderImportConfiguration, SwapRawOrder> _fileParser;
        private readonly ISwapOrderImportGateway _importGateway;

        public OpusSwapOrderImportService(
            OpusSftpOrderImportConfiguration configuration = null,
            Func<OpusSftpOrderImportConfiguration, DownloadedOrderFile> fileDownloader = null,
            Func<byte[], string, SwapAccount, OpusSftpOrderImportConfiguration, SwapRawOrder> fileParser = null,
            ISwapOrderImportGateway importGateway = null)
        {
            _configuration = configuration ?? OpusSftpOrderImportConfiguration.FromAppSettings();
            _fileDownloader = fileDownloader ?? new OpusSftpOrderFileDownloader().DownloadLatestFile;
            _fileParser = fileParser ?? new OpusSwapOrderFileParser().Parse;
            _importGateway = importGateway ?? new OrderManagerImportGateway();

            Engine.Instance.Log.Debug("[OpusSwapOrderImportService] Import service initialized.");
        }

        public void GetFullUnwindOrder(SwapAccount selectedAccount, out SwapRawOrder fullUnwind)
        {
            if (selectedAccount == null)
                throw new ArgumentNullException(nameof(selectedAccount));

            Engine.Instance.Log.Info("[OpusSwapOrderImportService] Building full unwind order for account: " + selectedAccount.AccountName);

            DownloadedOrderFile downloadedFile = _fileDownloader(_configuration);
            if (downloadedFile == null)
                throw new InvalidOperationException("No OPUS SFTP order file was downloaded.");

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderImportService] Downloaded order file received. FileName={0}, RemotePath={1}, SizeBytes={2}",
                downloadedFile.FileName,
                downloadedFile.RemotePath,
                downloadedFile.Content == null ? 0 : downloadedFile.Content.Length));

            fullUnwind = _fileParser(downloadedFile.Content, downloadedFile.FileName, selectedAccount, _configuration);
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
            _importGateway.ImportOrder(fullUnwind, out importedOrder, out errors);

            Engine.Instance.Log.Info(string.Format(
                "[OpusSwapOrderImportService] Import full unwind workflow completed. ImportedOrderSet={0}, Errors={1}",
                importedOrder != null,
                errors == null ? 0 : errors.Count));
        }
    }
}