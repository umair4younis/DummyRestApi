using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System.Linq;
using Puma.MDE.OPUS.Utilities;

namespace Puma.MDE.OPUS
{
    public partial class OpusApiClient
    {

        /// <summary>
        /// Creates a new quote for a swap in the home marketplace.
        /// </summary>
        public virtual async Task<OpusApiResponse<AssetQuote>> AddAssetQuoteToHomeMarketplaceAsync(string endpoint, string swapId, AssetQuote quote, string marketplaceId = "home")
        {
            if (string.IsNullOrWhiteSpace(swapId)) throw new ArgumentException("Swap ID required", nameof(swapId));
            if (quote == null) throw new ArgumentNullException(nameof(quote));

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);
                string fullUrl = BuildFullUrl(endpoint);

                string json = JsonSerializerSettingsProvider.Serialize(quote);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(fullUrl, content).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string message = $"POST quote failed: {response.StatusCode} - {body}";
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                var result = JsonSerializerSettingsProvider.Deserialize<OpusApiResponse<AssetQuote>>(body);
                return result;
            }).ConfigureAwait(false);
        }


        /// <summary>
        /// Fetches all quotes for a swap in a given marketplace.
        /// </summary>
        public virtual async Task<OpusApiResponse<QuoteGetResource>> GetSwapQuotesAsync(string endpoint, string swapId, string marketplaceId)
        {
            if (string.IsNullOrWhiteSpace(swapId)) throw new ArgumentException("Swap ID required", nameof(swapId));
            if (string.IsNullOrWhiteSpace(marketplaceId)) throw new ArgumentException("Marketplace ID required", nameof(marketplaceId));

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);
                string fullUrl = BuildFullUrl(endpoint);

                var response = await _httpClient.GetAsync(fullUrl).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string message = $"GET quotes failed: {response.StatusCode} - {body}";
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                return JsonSerializerSettingsProvider.Deserialize<OpusApiResponse<QuoteGetResource>>(body);
            }).ConfigureAwait(false);
        }


        /// <summary>
        /// Safest PatchSwapAsync - sends payload and only checks HTTP status.
        /// Does NOT deserialize the response body (most PATCHes return only 200 OK).
        /// </summary>
        public virtual async Task PatchSwapAsync(string endpoint, string swapId, object patchPayload)
        {
            if (string.IsNullOrWhiteSpace(swapId))
                throw new ArgumentException("Swap ID required", nameof(swapId));

            if (patchPayload == null)
                throw new ArgumentNullException(nameof(patchPayload));

            await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);
                string fullUrl = BuildFullUrl(endpoint);

                HttpResponseMessage response = await _httpClient.PatchAsync(fullUrl, patchPayload).ConfigureAwait(false);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string message = $"PATCH swap failed for {swapId}: Status={response.StatusCode}, Body={body ?? "empty"}";
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                Engine.Instance.Log.Info($"PATCH swap succeeded for swap {swapId}");
            }).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates delta using the unicredit-swap-service endpoint.
        /// </summary>
        public virtual async Task<OpusApiResponse<SwapDeltaUpdateResponse>> UpdateSwapDeltaAsync(string endpoint, string swapId, SwapDeltaUpdate deltaUpdate)
        {
            if (string.IsNullOrWhiteSpace(swapId))
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            if (deltaUpdate == null || deltaUpdate.Members == null || !deltaUpdate.Members.Any())
                throw new ArgumentException("Valid members delta required");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);
                string fullUrl = BuildFullUrl(endpoint);

                string json = JsonSerializerSettingsProvider.Serialize(deltaUpdate);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(fullUrl, content).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string message = $"PUT delta update failed for swap {swapId}: {response.StatusCode} - {body}";
                    Engine.Instance.Log.Error(message);

                    switch ((int)response.StatusCode)
                    {
                        case 400:
                        case 422:
                            throw new ApiValidationException("Validation failed", body);
                        case 401:
                        case 403:
                            throw new ApiAuthorizationException("Authorization failed - check permissions or token", body);
                        case 404:
                            throw new ApiNotFoundException("Resource not found", swapId, body);
                        case 429:
                            throw new ApiRateLimitException("Rate limit exceeded", body);
                        default:
                            if ((int)response.StatusCode >= 500)
                                throw new ApiServerException($"Server error {response.StatusCode}", body);
                            throw new HttpRequestException(message);
                    }
                }

                try
                {
                    var result = JsonSerializerSettingsProvider.Deserialize<OpusApiResponse<SwapDeltaUpdateResponse>>(body);
                    return result ?? new OpusApiResponse<SwapDeltaUpdateResponse> { Resource = new SwapDeltaUpdateResponse { Status = "success" } };
                }
                catch (JsonException jex)
                {
                    Engine.Instance.Log.Error($"Failed to deserialize delta update response: {jex.Message}\nBody: {body}");
                    throw new ApiResponseException("Invalid response format from server", body, jex);
                }
            }).ConfigureAwait(false);
        }

    }
}
