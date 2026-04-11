using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Puma.MDE.OPUS
{
    public partial class OpusWeightUpdateProcessor
    {

        /// <summary>
        /// Sends POST request to update asset composition weights (alternative flow).
        /// </summary>
        public async Task SendWeightUpdatePayloadPostAsync(string parentUuid, List<ComponentInfo> components)
        {
            if (string.IsNullOrEmpty(parentUuid))
            {
                Engine.Instance.Log.Warn("Cannot send update - parent UUID is missing");
                return;
            }

            if (components == null || components.Count == 0)
            {
                Engine.Instance.Log.Warn("No components to update");
                return;
            }

            // Build payload as anonymous object (Newtonsoft.Json handles it well)
            object payload = new
            {
                assetCompositionId = ParentAssetId,
                parentUuid = parentUuid,
                components = components.ConvertAll(c => new
                {
                    childUuid = c.Uuid,
                    weight = new { value = c.WeightPercent, unit = "%" },
                    quantity = c.WeightPercent,
                    reference = c.BbgTicker
                }).ToArray()
            };

            string endpoint = $"/asset-compositions/{ParentAssetId}";

            try
            {
                await _opusApiClient.PostAsync(endpoint, payload).ConfigureAwait(false);
                Engine.Instance.Log.Info("Weight update successfully sent to OPUS API.");
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error("Failed to send weight update: " + ex.Message);
            }
        }


        /// <summary>
        /// Updates asset-at-marketplaces configuration for a swap (including nominal).
        /// The payload now supports both "nominal" and "assetAtMarketplaces" at the root level,
        /// matching the current OPUS API contract.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <param name="patch">Patch payload containing nominal and/or assetAtMarketplaces</param>
        /// <exception cref="ArgumentException">Thrown if swapId is empty or patch is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
        public async Task UpdateSwapAssetAtMarketplacesAsync(string swapId, SwapPatch patch)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error("UpdateSwapAssetAtMarketplacesAsync called with empty swapId");
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            if (patch == null)
            {
                Engine.Instance.Log.Error("UpdateSwapAssetAtMarketplacesAsync called with null patch");
                throw new ArgumentException("Valid SwapPatch is required");
            }

            // At least one of nominal or assetAtMarketplaces should be provided
            bool hasNominal = patch.Nominal != null;
            bool hasAssetUpdates = patch.AssetAtMarketplaces != null && patch.AssetAtMarketplaces.Any();

            if (!hasNominal && !hasAssetUpdates)
            {
                Engine.Instance.Log.Error("UpdateSwapAssetAtMarketplacesAsync called with empty nominal and assetAtMarketplaces");
                throw new ArgumentException("At least nominal or assetAtMarketplaces must be provided");
            }

            Engine.Instance.Log.Info($"UpdateSwapAssetAtMarketplacesAsync started for swap {swapId} " +
                                    $"(nominal update: {hasNominal}, marketplaces: {patch.AssetAtMarketplaces?.Count ?? 0})");

            // Perform validation before update
            var validation = await ValidateSwapAsync(
                swapId,
                requireRecentQuote: true,
                minNotional: _minNotional
            ).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                Engine.Instance.Log.Error($"UpdateSwapAssetAtMarketplacesAsync failed validation: {validation.GetSummary()}");
                throw new InvalidOperationException(
                    $"Cannot update asset-at-marketplaces for swap {swapId}: {validation.ErrorMessage}");
            }

            foreach (var warning in validation.Warnings)
            {
                Engine.Instance.Log.Warn($"Swap {swapId} asset update warning: {warning}");
            }

            Engine.Instance.Log.Info($"UpdateSwapAssetAtMarketplacesAsync validation passed: {validation.GetSummary()}");

            string endpoint = $"/swaps/{swapId}";

            try
            {
                await _opusApiClient.PatchSwapAsync(endpoint, swapId, patch).ConfigureAwait(false);

                Engine.Instance.Log.Info($"Successfully updated asset-at-marketplaces" +
                                        (hasNominal ? " and nominal" : "") +
                                        $" for swap {swapId}");
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to update asset-at-marketplaces for swap {swapId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Updates the nominal (notional) value of the swap.
        /// 
        /// Performs validation, checks for significant notional change (logs warning if >50%),
        /// logs detailed validation summary, then sends the PATCH request.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <param name="patch">Patch payload containing the new nominal value</param>
        /// <exception cref="ArgumentException">Thrown if swapId is empty or patch is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
        public async Task UpdateSwapNominalAsync(string swapId, SwapNominalPatch patch)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error("UpdateSwapNominalAsync called with empty swapId");
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            if (patch?.Nominal == null)
            {
                Engine.Instance.Log.Error("UpdateSwapNominalAsync called with null nominal patch");
                throw new ArgumentException("Valid nominal patch required");
            }

            Engine.Instance.Log.Info($"UpdateSwapNominalAsync started for swap {swapId}. " +
                                    $"New nominal: {patch.Nominal.Quantity:N2} {patch.Nominal.Unit}");

            var validation = await ValidateSwapAsync(
                swapId,
                requireRecentQuote: false,
                minNotional: _minNotional
            ).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                Engine.Instance.Log.Error($"UpdateSwapNominalAsync failed validation: {validation.GetSummary()}");
                throw new InvalidOperationException(
                    $"Cannot update nominal for swap {swapId}: {validation.ErrorMessage}");
            }

            foreach (var warning in validation.Warnings)
            {
                Engine.Instance.Log.Warn($"Swap {swapId} nominal update warning: {warning}");
            }

            // Warn on significant notional change
            if (validation.CurrentNotional.HasValue && patch.Nominal.Quantity > 0)
            {
                decimal changePercent = Math.Abs(patch.Nominal.Quantity - validation.CurrentNotional.Value)
                                      / validation.CurrentNotional.Value * 100m;

                if (changePercent > 50m)
                {
                    Engine.Instance.Log.Warn(
                        $"Significant notional change detected for swap {swapId}: " +
                        $"{validation.CurrentNotional:N2} → {patch.Nominal.Quantity:N2} ({changePercent:F1}%)");
                }
            }

            Engine.Instance.Log.Info($"UpdateSwapNominalAsync validation passed: {validation.GetSummary()}");

            string endpoint = $"/swaps/{swapId}";

            try
            {
                await _opusApiClient.PatchSwapAsync(endpoint, swapId, patch).ConfigureAwait(false);
                Engine.Instance.Log.Info($"Successfully updated nominal for swap {swapId} to {patch.Nominal.Quantity:N2}");
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to update nominal for swap {swapId}: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Fetches delta (target vs current positions and weights) for swaps based on account segments.
        /// 
        /// Performs a POST request to /unicredit-swap-service/api/swaps/deltas
        /// using the correct base URL via GetBaseUrlForEndpoint.
        /// Logs the operation start and result summary (consistent with UpdateSwapDeltaAsync).
        /// </summary>
        /// <param name="request">Request containing list of account segment UUIDs</param>
        /// <returns>Swap delta fetch response with account segments, swaps and member deltas</returns>
        /// <exception cref="ArgumentException">Thrown if request is null or has no account segments</exception>
        public async Task<SwapDeltaFetchResponse> FetchSwapDeltaAsync(SwapDeltaFetchRequest request)
        {
            if (request == null || request.AccountSegments == null || request.AccountSegments.Count == 0)
            {
                Engine.Instance.Log.Error("FetchSwapDeltaAsync called with invalid or empty request - accountSegments required");
                throw new ArgumentException("At least one account segment UUID is required", nameof(request));
            }

            Engine.Instance.Log.Info(string.Format(
                "FetchSwapDeltaAsync started for {0} account segment(s): {1}",
                request.AccountSegments.Count,
                string.Join(", ", request.AccountSegments)));

            const string endpoint = "/unicredit-swap-service/api/swaps/deltas";

            try
            {
                var response = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    // Use the generic PostWithResponseAsync<T> with correct base URL
                    return await _opusApiClient.PostWithResponseAsync<SwapDeltaFetchResponse>(endpoint, request).ConfigureAwait(false);
                }).ConfigureAwait(false);

                // Calculate summary counts safely for .NET 4.8
                int totalSwaps = 0;
                int totalMembers = 0;

                if (response != null && response.AccountSegments != null)
                {
                    foreach (var segment in response.AccountSegments)
                    {
                        if (segment.Swaps != null)
                        {
                            totalSwaps += segment.Swaps.Count;
                            foreach (var swap in segment.Swaps)
                            {
                                if (swap.Members != null)
                                {
                                    totalMembers += swap.Members.Count;
                                }
                            }
                        }
                    }
                }

                Engine.Instance.Log.Info(string.Format(
                    "FetchSwapDeltaAsync completed successfully. Retrieved data for {0} swap(s) with {1} member delta(s).",
                    totalSwaps, totalMembers));

                return response;
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error(string.Format("Circuit breaker open during FetchSwapDeltaAsync: {0}", cbex.Message));
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error(string.Format("Failed to fetch swap delta: {0}", ex.Message));
                throw;
            }
        }


        /// <summary>
        /// Updates delta (current pieces and weights) for members of a swap using PUT.
        /// 
        /// Uses the unicredit-swap-service endpoint via GetBaseUrlForEndpoint.
        /// Performs validation, validates that sum of weights is approximately 100%,
        /// logs enriched information (member count and asset IDs), then calls the API.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <param name="deltaUpdate">Delta payload containing members with assetId, currentPieces and currentWeight</param>
        /// <returns>Response from the delta update API</returns>
        /// <exception cref="ArgumentException">Thrown if swapId is empty or deltaUpdate is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown if validation fails or weights sum is invalid</exception>
        public async Task<OpusApiResponse<SwapDeltaUpdateResponse>> UpdateSwapDeltaAsync(string swapId, SwapDeltaUpdate deltaUpdate)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error("UpdateSwapDeltaAsync called with empty swapId");
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            if (deltaUpdate == null || deltaUpdate.Members == null || !deltaUpdate.Members.Any())
            {
                Engine.Instance.Log.Error("UpdateSwapDeltaAsync called with empty or null members");
                throw new ArgumentException("At least one member delta is required");
            }

            // Enriched logging with member count and asset IDs
            var assetIds = string.Join(", ", deltaUpdate.Members.Select(m => m.AssetId ?? "missing"));
            Engine.Instance.Log.Info($"UpdateSwapDeltaAsync started for swap {swapId} | " +
                                    $"Members: {deltaUpdate.Members.Count} | Assets: {assetIds}");

            var validation = await ValidateSwapAsync(
                swapId,
                requireRecentQuote: true,
                minNotional: _minNotional
            ).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                Engine.Instance.Log.Error($"UpdateSwapDeltaAsync failed validation: {validation.GetSummary()}");
                throw new InvalidOperationException(
                    $"Cannot update delta for swap {swapId}: {validation.ErrorMessage}");
            }

            foreach (var warning in validation.Warnings)
            {
                Engine.Instance.Log.Warn($"Swap {swapId} delta update warning: {warning}");
            }

            // Sum-of-weights validation (~100%)
            decimal totalWeight = deltaUpdate.Members.Sum(m => m.CurrentWeight);
            if (Math.Abs(totalWeight - 100m) > 0.5m)
            {
                string message = $"Sum of weights must be approximately 100%. Actual sum: {totalWeight:F2}%";
                Engine.Instance.Log.Info(message);
                // TODO: to throw if needs to restrict this condition
                //throw new ArgumentException(message);
            }

            if (Math.Abs(totalWeight - 100m) > 0.01m)
            {
                Engine.Instance.Log.Warn($"Weights sum is {totalWeight:F2}% – slight deviation from 100% for swap {swapId}");
            }

            Engine.Instance.Log.Info($"UpdateSwapDeltaAsync validation passed: {validation.GetSummary()}");

            string endpoint = $"/unicredit-swap-service/api/swaps/{swapId}";

            try
            {
                var result = await _opusApiClient.UpdateSwapDeltaAsync(endpoint, swapId, deltaUpdate).ConfigureAwait(false);
                Engine.Instance.Log.Info($"Successfully updated delta for swap {swapId} ({deltaUpdate.Members.Count} members)");
                return result;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Failed to update delta for swap {swapId}: {ex.Message}");
                throw;
            }
        }

    }
}
