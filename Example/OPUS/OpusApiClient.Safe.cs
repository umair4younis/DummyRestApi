using System.Net.Http;
using System.Threading.Tasks;
using System;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Utilities;


namespace Puma.MDE.OPUS
{
    public partial class OpusApiClient
    {
        private async Task<OpusOperationResult> ExecuteSafelyAsync(Func<Task> operation, string operationName, string friendlyErrorMessage)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return OpusOperationResult.Success();
            }
            catch (Exception ex)
            {
                string message = string.IsNullOrWhiteSpace(friendlyErrorMessage) ? DefaultFriendlyErrorMessage : friendlyErrorMessage;
                Engine.Instance.Log.Error("[" + operationName + "] Failed: " + ex.ToString());
                OpusOperationResult failure = OpusOperationResult.Failure(message, ex.Message);
                OpusMessageTrailContext.PrefixCompletedBeforeTrail(failure);
                return failure;
            }
        }

        public Task<OpusOperationResult<Tuple<HttpResponseMessage, string>>> TryGetWithResponseAsync(string endpoint)
        {
            return ExecuteSafelyAsync(() => GetWithResponseAsync(endpoint), "TryGetWithResponseAsync", "Unable to load OPUS response right now.");
        }


        public Task<OpusOperationResult> TryPostAsync(string endpoint, object data)
        {
            return ExecuteSafelyAsync(() => PostAsync(endpoint, data), "TryPostAsync", "Unable to send data to OPUS right now.");
        }


        public Task<OpusOperationResult<Tuple<HttpResponseMessage, string>>> TryPostWithResponseAsync(string endpoint, object data)
        {
            return ExecuteSafelyAsync(() => PostWithResponseAsync(endpoint, data), "TryPostWithResponseAsync", "Unable to send data to OPUS right now.");
        }


        public Task<OpusOperationResult> TryPatchAsync(string endpoint, object patchPayload, bool encodeUrl = true, int timeoutMs = 0)
        {
            return ExecuteSafelyAsync(() => PatchAsync(endpoint, patchPayload, encodeUrl, timeoutMs), "TryPatchAsync", "Unable to update OPUS data right now.");
        }


        public Task<OpusOperationResult> TryPatchAsync(string endpoint, object patchPayload, string parentAssetId)
        {
            return ExecuteSafelyAsync(() => PatchAsync(endpoint, patchPayload, parentAssetId), "TryPatchAsyncWithParent", "Unable to update OPUS data right now.");
        }


        public Task<OpusOperationResult<Tuple<HttpResponseMessage, string>>> TryPatchWithResponseAsync(string endpoint, object patchPayload)
        {
            return ExecuteSafelyAsync(() => PatchWithResponseAsync(endpoint, patchPayload), "TryPatchWithResponseAsync", "Unable to update OPUS data right now.");
        }


        public Task<OpusOperationResult> TryPutAsync(string endpoint, object data)
        {
            return ExecuteSafelyAsync(() => PutAsync(endpoint, data), "TryPutAsync", "Unable to update OPUS data right now.");
        }


        public Task<OpusOperationResult<Tuple<HttpResponseMessage, string>>> TryPutWithResponseAsync(string endpoint, object data)
        {
            return ExecuteSafelyAsync(() => PutWithResponseAsync(endpoint, data), "TryPutWithResponseAsync", "Unable to update OPUS data right now.");
        }


        public Task<OpusOperationResult> TryDeleteAsync(string endpoint)
        {
            return ExecuteSafelyAsync(() => DeleteAsync(endpoint), "TryDeleteAsync", "Unable to delete OPUS data right now.");
        }


        public Task<OpusOperationResult<Tuple<HttpResponseMessage, string>>> TryDeleteWithResponseAsync(string endpoint)
        {
            return ExecuteSafelyAsync(() => DeleteWithResponseAsync(endpoint), "TryDeleteWithResponseAsync", "Unable to delete OPUS data right now.");
        }


        public Task<OpusOperationResult<OpusApiResponse<AssetQuote>>> TryAddAssetQuoteToHomeMarketplaceAsync(string endpoint, string swapId, AssetQuote quote, string marketplaceId = "home")
        {
            return ExecuteSafelyAsync(() => AddAssetQuoteToHomeMarketplaceAsync(endpoint, swapId, quote, marketplaceId), "TryAddAssetQuoteToHomeMarketplaceAsync", "Unable to add asset quote right now.");
        }


        public Task<OpusOperationResult<OpusApiResponse<QuoteGetResource>>> TryGetSwapQuotesAsync(string endpoint, string swapId, string marketplaceId)
        {
            return ExecuteSafelyAsync(() => GetSwapQuotesAsync(endpoint, swapId, marketplaceId), "TryGetSwapQuotesAsync", "Unable to get swap quotes right now.");
        }


        public Task<OpusOperationResult> TryPatchSwapAsync(string endpoint, string swapId, object patchPayload)
        {
            return ExecuteSafelyAsync(() => PatchSwapAsync(endpoint, swapId, patchPayload), "TryPatchSwapAsync", "Unable to update swap right now.");
        }


        public Task<OpusOperationResult<OpusApiResponse<SwapDeltaUpdateResponse>>> TryUpdateSwapDeltaAsync(string endpoint, string swapId, SwapDeltaUpdate deltaUpdate)
        {
            return ExecuteSafelyAsync(() => UpdateSwapDeltaAsync(endpoint, swapId, deltaUpdate), "TryUpdateSwapDeltaAsync", "Unable to update swap delta right now.");
        }

    }
}
