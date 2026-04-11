using Newtonsoft.Json;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Puma.MDE.OPUS.Tests
{
    public partial class FakeOpusApiClient
    {
        public override Task<OpusApiResponse<QuoteGetResource>> GetSwapQuotesAsync(string endpoint, string swapId, string marketplaceId)
        {
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_getSwapQuotesThrowException != null) throw _getSwapQuotesThrowException;

            if (_getSwapQuotesResult != null)
            {
                return Task.FromResult(_getSwapQuotesResult);
            }

            return Task.FromResult(new OpusApiResponse<QuoteGetResource>
            {
                Resource = new QuoteGetResource
                {
                    Quotes = new List<AssetQuote>
                    {
                        new AssetQuote
                        {
                            Time = DateTime.UtcNow,
                            Value = new AmountValue { Quantity = 100000m }
                        }
                    }
                }
            });
        }

        public override async Task<OpusApiResponse<AssetQuote>> AddAssetQuoteToHomeMarketplaceAsync(string endpoint, string swapId, AssetQuote quote, string marketplaceId = "home")
        {
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            var tuple = await PostWithResponseAsync(endpoint, quote).ConfigureAwait(false);
            if (!tuple.Item1.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"POST quote failed: {tuple.Item1.StatusCode} - {tuple.Item2}");
            }

            if (!string.IsNullOrWhiteSpace(tuple.Item2))
            {
                try
                {
                    var typed = JsonConvert.DeserializeObject<OpusApiResponse<AssetQuote>>(tuple.Item2);
                    if (typed != null) return typed;
                }
                catch { }
            }

            return new OpusApiResponse<AssetQuote> { Resource = quote };
        }

        public override async Task<OpusApiResponse<SwapDeltaUpdateResponse>> UpdateSwapDeltaAsync(string endpoint, string swapId, SwapDeltaUpdate deltaUpdate)
        {
            UpdateSwapDeltaAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            var tuple = await PutWithResponseAsync(endpoint, deltaUpdate).ConfigureAwait(false);

            if (!tuple.Item1.IsSuccessStatusCode)
            {
                string message = $"PUT delta update failed for swap {swapId}: {tuple.Item1.StatusCode} - {tuple.Item2}";

                switch ((int)tuple.Item1.StatusCode)
                {
                    case 400:
                    case 422:
                        throw new ApiValidationException("Validation failed", tuple.Item2);
                    case 401:
                    case 403:
                        throw new ApiAuthorizationException("Authorization failed - check permissions or token", tuple.Item2);
                    case 404:
                        throw new ApiNotFoundException("Resource not found", swapId, tuple.Item2);
                    case 429:
                        throw new ApiRateLimitException("Rate limit exceeded", tuple.Item2);
                    default:
                        if ((int)tuple.Item1.StatusCode >= 500)
                            throw new ApiServerException($"Server error {tuple.Item1.StatusCode}", tuple.Item2);
                        throw new HttpRequestException(message);
                }
            }

            try
            {
                var result = JsonConvert.DeserializeObject<OpusApiResponse<SwapDeltaUpdateResponse>>(tuple.Item2);
                return result ?? new OpusApiResponse<SwapDeltaUpdateResponse> { Resource = new SwapDeltaUpdateResponse { Status = "success" } };
            }
            catch (JsonException jex)
            {
                throw new ApiResponseException("Invalid response format from server", tuple.Item2, jex);
            }
        }

        public void SetGetWithResponseResult(HttpResponseMessage response)
        {
            string body = response.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            try
            {
                _getSwapQuotesResult = JsonConvert.DeserializeObject<OpusApiResponse<QuoteGetResource>>(body);
            }
            catch
            {
                _getSwapQuotesResult = null;
            }
        }
    }
}
