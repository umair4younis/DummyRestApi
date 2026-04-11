using Newtonsoft.Json;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Puma.MDE.OPUS
{
    public partial class OpusWeightUpdateProcessor
    {

        /// <summary>
        /// Retrieves a specific quote for an asset in a marketplace, wrapped in circuit breaker.
        /// Endpoint: GET /swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes
        /// </summary>
        /// <param name="swapId">The swap identifier (UUID)</param>
        /// <param name="marketplaceId">The marketplace identifier (UUID)</param>
        /// <returns>The OpusApiResponse object of type QuoteGetResource or null if not found</returns>
        public async Task<OpusApiResponse<QuoteGetResource>> GetAssetQuoteAsync(string swapId, string marketplaceId)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }
            if (string.IsNullOrWhiteSpace(marketplaceId))
            {
                Engine.Instance.Log.Error(string.Format("Marketplace ID is required: {0}", marketplaceId));
                throw new ArgumentException("Marketplace ID is required", nameof(marketplaceId));
            }

            string endpoint = $"/swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes";

            try
            {
                var response = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    return await _opusApiClient.GetAsync<OpusApiResponse<QuoteGetResource>>(endpoint).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info($"Quotes retrieved for swap {swapId}, marketplace {marketplaceId}.");
                return response;
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error($"Circuit open - GET quotes aborted: {cbex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to get quotes for swap {swapId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Adds a new quote to the asset's home marketplace, wrapped in circuit breaker.
        /// Endpoint: POST /swaps/{swapId}/asset-at-marketplaces/home/quotes
        /// </summary>
        /// <param name="swapId">The swap identifier (UUID)</param>
        /// <param name="quote">The quote to create</param>
        /// <returns>The OpusApiResponse object of type QuotePostResource or null if not found</returns>
        public async Task<OpusApiResponse<QuotePostResource>> AddAssetQuoteToHomeMarketplaceAsync(string swapId, AssetQuote quote)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }
            if (quote == null)
            {
                Engine.Instance.Log.Error(string.Format("Quote is required: {0}", quote));
                throw new ArgumentNullException(nameof(quote));
            }

            string endpoint = $"/swaps/{swapId}/asset-at-marketplaces/home/quotes";

            try
            {
                var response = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    var result = await _opusApiClient.PostWithResponseAsync(endpoint, quote).ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<OpusApiResponse<QuotePostResource>>(result.Item2);
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info($"Quote added to home marketplace for swap {swapId}.");
                return response;
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error($"Circuit open - POST quote aborted: {cbex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to add quote for swap {swapId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Updates an existing quote for an asset in a marketplace.
        /// Endpoint: PATCH /swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes/{quoteId}
        /// </summary>
        /// <param name="swapId">The swap identifier (UUID)</param>
        /// <param name="marketplaceId">The marketplace identifier (UUID)</param>
        /// <param name="quoteId">The quote identifier to update</param>
        /// <param name="patch">Partial quote update data (only changed fields)</param>
        /// <returns>The OpusApiResponse object of type QuotePatchResource or null if not found</returns>
        public async Task<OpusApiResponse<QuotePatchResource>> UpdateAssetQuoteAsync(string swapId, string marketplaceId, string quoteId, AssetQuotePatch patch)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }
            if (string.IsNullOrWhiteSpace(marketplaceId))
            {
                Engine.Instance.Log.Error(string.Format("Marketplace ID is required: {0}", marketplaceId));
                throw new ArgumentException("Marketplace ID is required", nameof(marketplaceId));
            }
            if (string.IsNullOrWhiteSpace(quoteId))
            {
                Engine.Instance.Log.Error(string.Format("Quote ID is required: {0}", quoteId));
                throw new ArgumentException(nameof(quoteId));
            }
            if (patch == null)
            {
                Engine.Instance.Log.Error(patch);
                throw new ArgumentNullException(nameof(patch));
            }

            string endpoint = $"/swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes/{quoteId}";

            try
            {
                var response = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    var result = await _opusApiClient.PatchWithResponseAsync(endpoint, patch).ConfigureAwait(false); // assuming you add this method
                    return JsonConvert.DeserializeObject<OpusApiResponse<QuotePatchResource>>(result.Item2);
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info(
                    $"Quote {quoteId} updated successfully for swap {swapId} in marketplace {marketplaceId}.");
                return response;
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error($"Circuit open - PATCH quote aborted: {cbex.Message}");
                throw;
            }
            catch (ApiValidationException vex)
            {
                Engine.Instance.Log.Error($"Validation error updating quote {quoteId}: {vex.Message}");
                if (!string.IsNullOrEmpty(vex.ResponseBody))
                    Engine.Instance.Log.Error($"Response body: {vex.ResponseBody}");
                throw;
            }
            catch (ApiNotFoundException nfex)
            {
                Engine.Instance.Log.Error($"Quote or resource not found: {nfex.Message}");
                throw;
            }
            catch (ApiRateLimitException rlex)
            {
                Engine.Instance.Log.Warn($"Rate limit hit while updating quote {quoteId}: {rlex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to update quote {quoteId} for swap {swapId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Deletes an existing quote and returns response details.
        /// Endpoint: DELETE /swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes/{quoteId}
        /// </summary>
        public async Task DeleteAssetQuoteWithResponseAsync(string swapId, string marketplaceId, string quoteId)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }
            if (string.IsNullOrWhiteSpace(marketplaceId))
            {
                Engine.Instance.Log.Error(string.Format("Marketplace ID is required: {0}", marketplaceId));
                throw new ArgumentException("Marketplace ID is required", nameof(marketplaceId));
            }
            if (string.IsNullOrWhiteSpace(quoteId))
            {
                Engine.Instance.Log.Error(string.Format("Quote ID is required: {0}", quoteId));
                throw new ArgumentException(nameof(quoteId));
            }

            string endpoint = $"/swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes/{quoteId}";

            try
            {
                var result = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    return await _opusApiClient.DeleteWithResponseAsync(endpoint).ConfigureAwait(false);
                }).ConfigureAwait(false);

                HttpResponseMessage response = result.Item1;
                string body = result.Item2;

                Engine.Instance.Log.Info($"Quote {quoteId} deleted. Status: {response.StatusCode}");

                if (!string.IsNullOrWhiteSpace(body))
                {
                    Engine.Instance.Log.Info($"DELETE response body: {body}");
                }
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error($"Circuit open - DELETE quote aborted: {cbex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to delete quote {quoteId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Creates a new Mark-to-Market (MtM) quote for the swap in its home marketplace.
        /// 
        /// This method first performs validation using ValidateSwapAsync (existence, status, optional quote freshness),
        /// logs a detailed validation summary, logs any warnings, then calls the underlying API.
        /// 
        /// Throws InvalidOperationException if validation fails.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <param name="quote">The quote data to create (time, value, etc.)</param>
        /// <param name="marketplaceId">Marketplace identifier (defaults to "home")</param>
        /// <returns>The created quote wrapped in OpusApiResponse&lt;AssetQuote&gt;</returns>
        /// <exception cref="ArgumentException">Thrown if swapId is null or empty</exception>
        /// <exception cref="ArgumentNullException">Thrown if quote is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if swap validation fails</exception>
        public async Task<OpusApiResponse<AssetQuote>> CreateSwapQuoteAsync(string swapId, AssetQuote quote, string marketplaceId = "home")
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }
            if (quote == null)
            {
                Engine.Instance.Log.Error(string.Format("Quote is required: {0}", quote));
                throw new ArgumentNullException(nameof(quote));
            }

            Engine.Instance.Log.Info($"CreateSwapQuoteAsync started for swap {swapId}");

            var validation = await ValidateSwapAsync(swapId, requireRecentQuote: false, minNotional: _minNotional).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                Engine.Instance.Log.Error($"CreateSwapQuoteAsync failed validation: {validation.GetSummary()}");
                throw new InvalidOperationException($"Cannot create quote for swap {swapId}: {validation.ErrorMessage}");
            }

            foreach (var warning in validation.Warnings)
            {
                Engine.Instance.Log.Warn($"Swap {swapId} quote creation warning: {warning}");
            }

            string endpoint = $"/swaps/{swapId}/asset-at-marketplaces/home/quotes";

            try
            {
                var result = await _opusApiClient.AddAssetQuoteToHomeMarketplaceAsync(endpoint, swapId, quote, marketplaceId).ConfigureAwait(false);

                Engine.Instance.Log.Info($"Quote successfully created for swap {swapId}. New quote time: {quote.Time:yyyy-MM-dd HH:mm}");
                return result;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to create quote for swap {swapId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Retrieves all quotes for a swap in the specified marketplace.
        /// 
        /// Performs validation first, logs detailed validation summary and any warnings,
        /// then fetches quotes via the API.
        /// 
        /// Throws InvalidOperationException if validation fails.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <param name="marketplaceId">Marketplace identifier (defaults to "home")</param>
        /// <returns>Quotes wrapped in OpusApiResponse&lt;QuoteGetResource&gt;</returns>
        /// <exception cref="ArgumentException">Thrown if swapId is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown if swap validation fails</exception>
        public async Task<OpusApiResponse<QuoteGetResource>> GetSwapQuotesAsync(string swapId, string marketplaceId = "home")
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error("GetSwapQuotesAsync called with empty swapId");
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            Engine.Instance.Log.Info($"GetSwapQuotesAsync started for swap {swapId}, marketplace: {marketplaceId}");

            var validation = await ValidateSwapAsync(swapId, requireRecentQuote: false, minNotional: _minNotional).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                Engine.Instance.Log.Error($"GetSwapQuotesAsync failed validation: {validation.GetSummary()}");
                throw new InvalidOperationException($"Cannot retrieve quotes for swap {swapId}: {validation.ErrorMessage}");
            }

            string endpoint = $"/swaps/{swapId}/asset-at-marketplaces/{marketplaceId}/quotes";

            try
            {
                var result = await _opusApiClient.GetSwapQuotesAsync(endpoint, swapId, marketplaceId).ConfigureAwait(false);
                Engine.Instance.Log.Info($"Successfully retrieved {result?.Resource?.Quotes?.Count ?? 0} quotes for swap {swapId}");
                return result;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to get quotes for swap {swapId}: {ex.Message}");
                throw;
            }
        }

    }
}
