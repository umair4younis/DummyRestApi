using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS
{
    public partial class OpusWeightUpdateProcessor
    {
        private async Task<OpusOperationResult> ExecuteSafelyAsync(
            Func<Task> operation,
            string operationName,
            string friendlyErrorMessage)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return OpusOperationResult.Success();
            }
            catch (Exception ex)
            {
                string fallbackFriendlyMessage = ex is InvalidOperationException && !string.IsNullOrWhiteSpace(ex.Message)
                    ? ex.Message
                    : string.IsNullOrWhiteSpace(friendlyErrorMessage)
                    ? DefaultFriendlyErrorMessage
                    : friendlyErrorMessage;

                Engine.Instance.Log.Error("[" + operationName + "] Failed: " + ex.ToString());
                OpusOperationResult failure = OpusOperationResult.Failure(fallbackFriendlyMessage, ex.Message);
                OpusMessageTrailContext.PrefixCompletedBeforeTrail(failure);
                return failure;
            }
        }

        public Task<OpusOperationResult<ValidationResultOpus>> TryValidateAssetCompositionIdAsync()
        {
            return ExecuteSafelyAsync(
                async () => await ValidateAssetCompositionIdAsync().ConfigureAwait(false),
                "TryValidateAssetCompositionIdAsync",
                "Unable to validate OPUS asset composition right now.");
        }

        public Task<OpusOperationResult<List<ComponentInfo>>> TryValidateAndCollectBbgUuidsAsync()
        {
            return ExecuteSafelyAsync(
                async () => await ValidateAndCollectBbgUuidsAsync().ConfigureAwait(false),
                "TryValidateAndCollectBbgUuidsAsync",
                "Unable to validate Bloomberg tickers right now.");
        }

        public Task<OpusOperationResult<Dictionary<string, List<AssetNode>>>> TryFetchBbgBatchAsync(List<string> batch)
        {
            return ExecuteSafelyAsync(
                async () => await FetchBbgBatchAsync(batch).ConfigureAwait(false),
                "TryFetchBbgBatchAsync",
                "Unable to load Bloomberg batch data right now.");
        }

        public OpusOperationResult<string> TryBuildBbgFilterQuery(List<string> bbgTickers)
        {
            return ExecuteSafely(
                () => BuildBbgFilterQuery(bbgTickers),
                "TryBuildBbgFilterQuery",
                "Unable to build Bloomberg filter query right now.");
        }

        public Task<OpusOperationResult> TrySendWeightUpdatePayloadPatchAsync(string parentUuid, List<ComponentInfo> components)
        {
            return ExecuteSafelyAsync(
                async () => await SendWeightUpdatePayloadPatchAsync(parentUuid, components).ConfigureAwait(false),
                "TrySendWeightUpdatePayloadPatchAsync",
                "Unable to send OPUS weight update right now.");
        }

        public Task<OpusOperationResult<string>> TryExecuteAsync()
        {
            return ExecuteSafelyAsync(
                async () => await ExecuteAsync().ConfigureAwait(false),
                "TryExecuteAsync",
                "Unable to complete OPUS processing. Please verify input data and try again.");
        }

        public Task<OpusOperationResult<TotalReturnSwapResponse>> TryGetTotalReturnSwapAsync(string swapId)
        {
            return ExecuteSafelyAsync(
                async () => await GetTotalReturnSwapAsync(swapId).ConfigureAwait(false),
                "TryGetTotalReturnSwapAsync",
                "Unable to retrieve swap details right now.");
        }

        public Task<OpusOperationResult<string>> TryCreateTotalReturnSwapAsync(object payload)
        {
            return ExecuteSafelyAsync(
                async () => await CreateTotalReturnSwapAsync(payload).ConfigureAwait(false),
                "TryCreateTotalReturnSwapAsync",
                "Unable to create swap right now.");
        }

        public Task<OpusOperationResult> TryUpdateTotalReturnSwapAsync(string swapId, object patchPayload)
        {
            return ExecuteSafelyAsync(
                async () => await UpdateTotalReturnSwapAsync(swapId, patchPayload).ConfigureAwait(false),
                "TryUpdateTotalReturnSwapAsync",
                "Unable to update swap right now.");
        }

        public Task<OpusOperationResult> TryDeleteTotalReturnSwapAsync(string swapId)
        {
            return ExecuteSafelyAsync(
                async () => await DeleteTotalReturnSwapAsync(swapId).ConfigureAwait(false),
                "TryDeleteTotalReturnSwapAsync",
                "Unable to delete swap right now.");
        }

        public Task<OpusOperationResult<OpusApiResponse<QuoteGetResource>>> TryGetAssetQuoteAsync(string swapId, string marketplaceId)
        {
            return ExecuteSafelyAsync(
                async () => await GetAssetQuoteAsync(swapId, marketplaceId).ConfigureAwait(false),
                "TryGetAssetQuoteAsync",
                "Unable to retrieve asset quotes right now.");
        }

        public Task<OpusOperationResult<OpusApiResponse<QuotePostResource>>> TryAddAssetQuoteToHomeMarketplaceAsync(string swapId, AssetQuote quote)
        {
            return ExecuteSafelyAsync(
                async () => await AddAssetQuoteToHomeMarketplaceAsync(swapId, quote).ConfigureAwait(false),
                "TryAddAssetQuoteToHomeMarketplaceAsync",
                "Unable to add asset quote right now.");
        }

        public Task<OpusOperationResult<OpusApiResponse<AssetQuote>>> TryCreateSwapQuoteAsync(string swapId, AssetQuote quote, string marketplaceId = "home")
        {
            return ExecuteSafelyAsync(
                async () => await CreateSwapQuoteAsync(swapId, quote, marketplaceId).ConfigureAwait(false),
                "TryCreateSwapQuoteAsync",
                "Unable to create swap quote right now.");
        }

        public Task<OpusOperationResult<OpusApiResponse<QuoteGetResource>>> TryGetSwapQuotesAsync(string swapId, string marketplaceId = "home")
        {
            return ExecuteSafelyAsync(
                async () => await GetSwapQuotesAsync(swapId, marketplaceId).ConfigureAwait(false),
                "TryGetSwapQuotesAsync",
                "Unable to load swap quotes right now.");
        }

        public Task<OpusOperationResult<OpusApiResponse<QuotePatchResource>>> TryUpdateAssetQuoteAsync(string swapId, string marketplaceId, string quoteId, AssetQuotePatch patch)
        {
            return ExecuteSafelyAsync(
                async () => await UpdateAssetQuoteAsync(swapId, marketplaceId, quoteId, patch).ConfigureAwait(false),
                "TryUpdateAssetQuoteAsync",
                "Unable to update swap quote right now.");
        }

        public Task<OpusOperationResult> TryDeleteAssetQuoteWithResponseAsync(string swapId, string marketplaceId, string quoteId)
        {
            return ExecuteSafelyAsync(
                async () => await DeleteAssetQuoteWithResponseAsync(swapId, marketplaceId, quoteId).ConfigureAwait(false),
                "TryDeleteAssetQuoteWithResponseAsync",
                "Unable to delete swap quote right now.");
        }

        public Task<OpusOperationResult> TrySendWeightUpdatePayloadPostAsync(string parentUuid, List<ComponentInfo> components)
        {
            return ExecuteSafelyAsync(
                async () => await SendWeightUpdatePayloadPostAsync(parentUuid, components).ConfigureAwait(false),
                "TrySendWeightUpdatePayloadPostAsync",
                "Unable to send OPUS weight update right now.");
        }

        public Task<OpusOperationResult> TryUpdateSwapAssetAtMarketplacesAsync(string swapId, SwapPatch patch)
        {
            return ExecuteSafelyAsync(
                async () => await UpdateSwapAssetAtMarketplacesAsync(swapId, patch).ConfigureAwait(false),
                "TryUpdateSwapAssetAtMarketplacesAsync",
                "Unable to update swap asset details right now.");
        }

        public Task<OpusOperationResult> TryUpdateSwapNominalAsync(string swapId, SwapNominalPatch patch)
        {
            return ExecuteSafelyAsync(
                async () => await UpdateSwapNominalAsync(swapId, patch).ConfigureAwait(false),
                "TryUpdateSwapNominalAsync",
                "Unable to update swap nominal right now.");
        }

        public Task<OpusOperationResult<SwapDeltaFetchResponse>> TryFetchSwapDeltaAsync(SwapDeltaFetchRequest request)
        {
            return ExecuteSafelyAsync(
                async () => await FetchSwapDeltaAsync(request).ConfigureAwait(false),
                "TryFetchSwapDeltaAsync",
                "Unable to fetch swap delta right now.");
        }

        public Task<OpusOperationResult<OpusApiResponse<SwapDeltaUpdateResponse>>> TryUpdateSwapDeltaAsync(string swapId, SwapDeltaUpdate deltaUpdate)
        {
            return ExecuteSafelyAsync(
                async () => await UpdateSwapDeltaAsync(swapId, deltaUpdate).ConfigureAwait(false),
                "TryUpdateSwapDeltaAsync",
                "Unable to update swap delta right now.");
        }

        public Task<OpusOperationResult<SwapValidationResult>> TryValidateSwapAsync(string swapId, bool requireRecentQuote = true, decimal? minNotional = null)
        {
            return ExecuteSafelyAsync(
                async () => await ValidateSwapAsync(swapId, requireRecentQuote, minNotional).ConfigureAwait(false),
                "TryValidateSwapAsync",
                "Unable to validate swap right now.");
        }

        public Task<OpusOperationResult> TryExecuteAsync_CircuitBreaker()
        {
            return ExecuteSafelyAsync(
                async () => await ExecuteAsync_CircuitBreaker().ConfigureAwait(false),
                "TryExecuteAsync_CircuitBreaker",
                "Unable to complete OPUS processing right now.");
        }
    }
}
