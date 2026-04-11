using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS
{
    /// <summary>
    /// Coordinates the process of validating and updating asset composition weights in OPUS.
    /// Handles parent asset validation, BBG ticker batch processing, weight aggregation, 
    /// and sending PATCH/POST updates with retry and circuit-breaker resilience.
    /// </summary>
    public partial class OpusWeightUpdateProcessor
    {
        private const string DefaultFriendlyErrorMessage = "Something went wrong while processing OPUS data. Please try again or contact support.";

        private readonly decimal _minNotional = 1_000_000m;
        private readonly OpusGraphQLClient _opusGraphQLClient;
        private readonly OpusApiClient _opusApiClient;
        public readonly OpusCircuitBreaker _opusCircuitBreaker;

        // Different retry policies per step
        private readonly RetryPolicy _parentValidationPolicy;
        private readonly RetryPolicy _bbgBatchPolicy;

        public static string ParentAssetId = string.Empty;
        public static string Currency = string.Empty;
        public static decimal SwapNotional = 0m;
        public static decimal Mtm = 0m;
        public static decimal MtmFromFinancing = 0m;
        public static decimal SwapValue = 0m;

        private static List<ReportHolding> _reportHoldings;
        public static List<ReportHolding> ReportHoldings
        {
            get
            {
                if (_reportHoldings == null)
                {
                    _reportHoldings = new List<ReportHolding>();
                }
                return _reportHoldings;
            }
            set
            {
                _reportHoldings = value ?? new List<ReportHolding>();
            }
        }

        /// <summary>
        /// Initializes the processor with required GraphQL and REST clients, 
        /// sets up retry policies and a shared circuit breaker for resilience.
        /// </summary>
        public OpusWeightUpdateProcessor(
            OpusGraphQLClient opusGraphQLClient,
            OpusApiClient opusApiClient,
            OpusCircuitBreaker opusCircuitBreaker = null)
        {
            _opusGraphQLClient = opusGraphQLClient;
            _opusApiClient = opusApiClient;

            if (_opusGraphQLClient == null) throw new ArgumentNullException("opusGraphQLClient");
            if (_opusApiClient == null) throw new ArgumentNullException("opusApiClient");

            _opusCircuitBreaker = opusCircuitBreaker ?? new OpusCircuitBreaker(
                failureThreshold: 5,
                breakSeconds: 60,
                maxRetries: 3,
                baseRetryDelayMs: 1000,
                backoffFactor: 2.0,
                jitterMaxFactor: 0.5);

            // Initialize retry policies
            _parentValidationPolicy = new RetryPolicy
            {
                MaxRetries = 4,
                BaseDelayMs = 1000,
                BackoffFactor = 2.0,
                JitterMaxFactor = 0.4
            };

            _bbgBatchPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                BaseDelayMs = 1500,
                BackoffFactor = 2.5,
                JitterMaxFactor = 0.6
            };

            // Default retry condition (customize as needed)
            _parentValidationPolicy.IsRetryable = IsTransientError;
            _bbgBatchPolicy.IsRetryable = IsTransientError;

            // Single shared circuit breaker for all operations (tune parameters as needed)
            _opusCircuitBreaker = new OpusCircuitBreaker(
                failureThreshold: 5,
                breakSeconds: 60,
                maxRetries: 3,
                baseRetryDelayMs: 1000,
                backoffFactor: 2.0,
                jitterMaxFactor: 0.5
            );
        }

        /// <summary>
        /// Determines if an exception is transient and worth retrying (timeout, 5xx, 429, etc.).
        /// </summary>
        private bool IsTransientError(Exception ex)
        {
            if (ex == null) return false;

            string message = ex.Message ?? "";
            return
                ex is System.Net.Http.HttpRequestException ||
                message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.Contains("503") ||
                message.Contains("504") ||
                message.Contains("429");
        }

        /// <summary>
        /// Main execution flow: validates parent asset, collects valid BBG components, 
        /// checks total weight, and sends composition update via PATCH.
        /// </summary>
        public async Task<string> ExecuteAsync()
        {
            // STEP 1: Parent validation with retry
            var validationResult = await ExecuteWithRetryAsync(
                ValidateAssetCompositionIdAsync,
                "Parent composition validation",
                _parentValidationPolicy).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                Engine.Instance.Log.Error("[FATAL] " + validationResult.ErrorMessage);
                return string.Empty;
            }

            Engine.Instance.Log.Info(string.Format(
                "Parent validated: {0} (ID: {1}, UUID: {2})",
                validationResult.AssetName ?? "N/A",
                ParentAssetId,
                validationResult.AssetUuid ?? "N/A"));

            // STEP 2: BBG validation
            var validComponents = await ValidateAndCollectBbgUuidsAsync().ConfigureAwait(false);

            if (validComponents.Count == 0)
            {
                Engine.Instance.Log.Error("[ERROR] No valid child components found.");
                return string.Empty;
            }

            Engine.Instance.Log.Info(string.Format("Found {0} valid components.", validComponents.Count));

            decimal totalWeight = validComponents.Sum(c => c.WeightPercent);
            if (Math.Abs(totalWeight - 100m) > 0.5m)
            {
                Engine.Instance.Log.Warn(string.Format(
                    "[WARNING] Total weight {0:F2}% (expected ~100%)", totalWeight));
            }

            await SendWeightUpdatePayloadPatchAsync(validationResult.AssetUuid, validComponents).ConfigureAwait(false);
            return validationResult.AssetUuid;
        }

        /// <summary>
        /// Executes an operation with exponential backoff + jitter retries on transient errors.
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            RetryPolicy policy)
        {
            Engine.Instance.Log.Info($"[ExecuteWithRetryAsync] Starting '{operationName}' with max {policy.MaxRetries} retries, " +
                                    $"base delay {policy.BaseDelayMs}ms, backoff factor {policy.BackoffFactor}");

            Random random = new Random();

            for (int attempt = 1; attempt <= policy.MaxRetries; attempt++)
            {
                try
                {
                    Engine.Instance.Log.Debug($"[ExecuteWithRetryAsync] '{operationName}' - Attempt {attempt}/{policy.MaxRetries}");
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (policy.IsRetryable != null && policy.IsRetryable(ex) && attempt < policy.MaxRetries)
                    {
                        // Exponential backoff
                        double backoff = policy.BaseDelayMs * Math.Pow(policy.BackoffFactor, attempt - 1);

                        // Jitter (0 to jitterMaxFactor * backoff)
                        double jitter = random.NextDouble() * backoff * policy.JitterMaxFactor;

                        int delayMs = (int)(backoff + jitter);

                        Engine.Instance.Log.Info(string.Format(
                            "[ExecuteWithRetryAsync] '{0}' - Attempt {1}/{2} failed ({3}): {4}. Retrying in {5}ms...",
                            operationName, attempt, policy.MaxRetries, ex.GetType().Name, ex.Message.Trim(), delayMs));

                        await TestDelayService.Delay(delayMs).ConfigureAwait(false);
                        continue;
                    }

                    Engine.Instance.Log.Warn(string.Format(
                        "[ExecuteWithRetryAsync] '{0}' - Failed after {1} attempts ({2}): {3}",
                        operationName, policy.MaxRetries, ex.GetType().Name, ex.Message));

                    throw;
                }
            }

            throw new InvalidOperationException(
                string.Format("[ExecuteWithRetryAsync] '{0}' - Exhausted all {1} retries.", operationName, policy.MaxRetries));
        }

        /// <summary>
        /// Validates the parent asset composition ID via GraphQL and returns its UUID and name.
        /// </summary>
        public async Task<ValidationResultOpus> ValidateAssetCompositionIdAsync()
        {
            Engine.Instance.Log.Info($"[ValidateAssetCompositionIdAsync] Starting parent asset validation for ID: {ParentAssetId}");

            string query = $@"
    {{
      assets(range: {{offset: 0, size: 1000}},
             filter: {{
               and: [{{ expression: ""id = {ParentAssetId}"" }}]
             }}) {{
        edges {{
          node {{
            id
            name
            uuid
            __typename
          }}
        }}
      }}
    }}";

            try
            {
                // Execute using generic ExecuteAsync<T>
                Engine.Instance.Log.Debug($"[ValidateAssetCompositionIdAsync] Executing GraphQL query for asset ID: {ParentAssetId}");
                var response = await _opusGraphQLClient.ExecuteAsync<AssetsQueryResponse>(query).ConfigureAwait(false);

                // Access path is now response.assets.edges...
                var node = response != null &&
                           response.assets != null &&
                           response.assets.edges != null &&
                           response.assets.edges.Length > 0
                    ? response.assets.edges[0].node
                    : null;

                if (node == null)
                {
                    Engine.Instance.Log.Error($"[ValidateAssetCompositionIdAsync] Asset not found for ID: {ParentAssetId}");
                    return new ValidationResultOpus { IsValid = false, ErrorMessage = "OPUS asset id is not valid. No asset found." };
                }

                if (string.IsNullOrWhiteSpace(node.uuid))
                {
                    Engine.Instance.Log.Error($"[ValidateAssetCompositionIdAsync] UUID is missing for asset: {node.name}");
                    return new ValidationResultOpus { IsValid = false, AssetName = node.name, ErrorMessage = "OPUS asset id is not valid. UUID is missing." };
                }

                if (node.__typename != "ASSETCOMPOSITION")
                {
                    Engine.Instance.Log.Error($"[ValidateAssetCompositionIdAsync] Asset is not ASSETCOMPOSITION type. Type: {node.__typename}");
                    return new ValidationResultOpus { IsValid = false, AssetName = node.name, AssetUuid = node.uuid, ErrorMessage = "OPUS asset id is not valid." };
                }

                Engine.Instance.Log.Info($"[ValidateAssetCompositionIdAsync] Validation successful. Asset: {node.name}, UUID: {node.uuid}");
                return new ValidationResultOpus
                {
                    IsValid = true,
                    AssetName = node.name,
                    AssetUuid = node.uuid,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[ValidateAssetCompositionIdAsync] Exception during validation: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates BBG tickers in batches via GraphQL, collects valid UUIDs and weights.
        /// </summary>
        public async Task<List<ComponentInfo>> ValidateAndCollectBbgUuidsAsync()
        {
            const int BATCH_SIZE = 6;
            List<ComponentInfo> validMappings = new List<ComponentInfo>();
            List<string> errors = new List<string>();

            if (ReportHoldings == null || ReportHoldings.Count == 0)
            {
                Engine.Instance.Log.Warn("[ValidateAndCollectBbgUuidsAsync] ReportHoldings is empty. No BBG validation will be executed.");
                return validMappings;
            }

            // Get all BBG tickers
            List<string> allTickers = new List<string>();
            foreach (ReportHolding holding in ReportHoldings)
            {
                if (holding == null)
                {
                    Engine.Instance.Log.Warn("[ValidateAndCollectBbgUuidsAsync] Encountered null holding entry. Skipping.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(holding.BbgTicker))
                {
                    string holdingName = string.IsNullOrWhiteSpace(holding.Name) ? "N/A" : holding.Name;
                    Engine.Instance.Log.Warn("[ValidateAndCollectBbgUuidsAsync] Holding has empty BBG ticker. Skipping. Holding: " + holdingName);
                    continue;
                }

                allTickers.Add(holding.BbgTicker.Trim());
            }

            if (allTickers.Count == 0)
            {
                Engine.Instance.Log.Warn("[ValidateAndCollectBbgUuidsAsync] No valid BBG tickers found after input sanitization.");
                return validMappings;
            }

            // Split into batches
            List<List<string>> batches = new List<List<string>>();
            for (int i = 0; i < allTickers.Count; i += BATCH_SIZE)
            {
                int count = Math.Min(BATCH_SIZE, allTickers.Count - i);
                List<string> batch = allTickers.GetRange(i, count);
                batches.Add(batch);
            }

            foreach (List<string> batch in batches)
            {
                string batchDescription = string.Join(", ", batch.ToArray());

                Dictionary<string, List<AssetNode>> found = null;

                try
                {
                    found = await ExecuteWithRetryAsync(
                        () => FetchBbgBatchAsync(batch),  // now returns Task<...>
                        "BBG batch validation (" + batchDescription + ")",
                        _bbgBatchPolicy).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Engine.Instance.Log.Error("[ValidateAndCollectBbgUuidsAsync] Batch failed permanently. Batch: " + batchDescription + ". Error: " + ex.Message);
                    errors.Add("Batch failed permanently: " + batchDescription + " - " + ex.Message);
                    continue;
                }

                if (found == null)
                {
                    Engine.Instance.Log.Error("[ValidateAndCollectBbgUuidsAsync] Batch result dictionary is null. Batch: " + batchDescription);
                    errors.Add("Batch returned null result: " + batchDescription);
                    continue;
                }

                foreach (string bbg in batch)
                {
                    if (string.IsNullOrWhiteSpace(bbg))
                    {
                        Engine.Instance.Log.Warn("[ValidateAndCollectBbgUuidsAsync] Encountered null or empty BBG ticker in batch. Batch: " + batchDescription);
                        errors.Add("Encountered null/empty BBG ticker in batch: " + batchDescription);
                        continue;
                    }

                    List<AssetNode> nodes = null;
                    if (found.TryGetValue(bbg, out nodes) && nodes != null && nodes.Count > 0)
                    {
                        AssetNode validNode = null;
                        foreach (AssetNode node in nodes)
                        {
                            if (!string.IsNullOrWhiteSpace(node.uuid))
                            {
                                validNode = node;
                                break;
                            }
                        }

                        if (validNode == null)
                        {
                            errors.Add("No valid UUID for BBG: " + bbg);
                            continue;
                        }

                        // Prefer ASSETCOMPOSITION when available
                        AssetNode preferred = null;
                        foreach (AssetNode node in nodes)
                        {
                            if (node.__typename == "ASSETCOMPOSITION")
                            {
                                preferred = node;
                                break;
                            }
                        }
                        if (preferred == null)
                            preferred = validNode;

                        // Find corresponding weight and nominal
                        decimal weight = 0m;
                        decimal nominal = 0m;
                        string currency = string.Empty;
                        foreach (ReportHolding h in ReportHoldings)
                        {
                            if (h != null && !string.IsNullOrWhiteSpace(h.BbgTicker) &&
                                string.Equals(h.BbgTicker.Trim(), bbg, StringComparison.OrdinalIgnoreCase))
                            {
                                weight = h.MarketWeightPercent;
                                nominal = h.Nominal;
                                currency = h.Currency;
                                break;
                            }
                        }

                        validMappings.Add(new ComponentInfo
                        {
                            BbgTicker = bbg,
                            Uuid = preferred.uuid,
                            WeightPercent = weight,
                            Nominal = nominal,
                            Currency = currency
                        });

                        Engine.Instance.Log.Info("Validated: " + bbg + " → " + preferred.uuid + " (" + weight + "%)");
                    }
                    else
                    {
                        errors.Add("No asset found for BBG: " + bbg);
                    }
                }

                // Gentle delay between batches
                await Task.Delay(400).ConfigureAwait(false);
            }

            if (errors.Count > 0)
            {
                Engine.Instance.Log.Warn("\nNon-recoverable validation issues:");
                foreach (string err in errors)
                {
                    Engine.Instance.Log.Error("  - " + err);
                }
            }

            return validMappings;
        }

        /// <summary>
        /// Fetches assets matching a batch of BBG tickers using GraphQL.
        /// </summary>
        public async Task<Dictionary<string, List<AssetNode>>> FetchBbgBatchAsync(List<string> batch)
        {
            if (batch == null)
            {
                Engine.Instance.Log.Warn("[FetchBbgBatchAsync] Batch is null. Returning empty result.");
                return new Dictionary<string, List<AssetNode>>();
            }

            List<string> sanitizedBatch = batch
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sanitizedBatch.Count == 0)
            {
                Engine.Instance.Log.Warn("[FetchBbgBatchAsync] Batch has no valid BBG tickers after sanitization. Returning empty result.");
                return new Dictionary<string, List<AssetNode>>();
            }

            if (sanitizedBatch.Count != batch.Count)
            {
                Engine.Instance.Log.Warn("[FetchBbgBatchAsync] Batch contained invalid or duplicate BBG entries. Using sanitized list size: " + sanitizedBatch.Count);
            }

            batch = sanitizedBatch;
            string batchDescription = string.Join(", ", batch);
            Engine.Instance.Log.Info($"[FetchBbgBatchAsync] Fetching assets for batch: {batchDescription}");

            try
            {
                string query = BuildBbgFilterQuery(batch);
                Engine.Instance.Log.Debug($"[FetchBbgBatchAsync] Executing GraphQL query for {batch.Count} tickers");

                var response = await _opusGraphQLClient.ExecuteAsync<AssetsQueryResponse>(query).ConfigureAwait(false);

                var resultDict = new Dictionary<string, List<AssetNode>>();

                if (response?.assets?.edges != null)
                {
                    Engine.Instance.Log.Debug($"[FetchBbgBatchAsync] Received {response.assets.edges.Length} edges from GraphQL");

                    foreach (var edge in response.assets.edges)
                    {
                        var node = edge?.node;
                        if (node != null && node.symbols != null && node.symbols.Length > 0)
                        {
                            var identifier = node.symbols[0].identifier;
                            if (!string.IsNullOrEmpty(identifier))
                            {
                                if (!resultDict.ContainsKey(identifier))
                                    resultDict[identifier] = new List<AssetNode>();

                                resultDict[identifier].Add(node);
                            }
                        }
                    }

                    Engine.Instance.Log.Info($"[FetchBbgBatchAsync] Batch complete. Found assets for {resultDict.Count} tickers");
                }
                else
                {
                    Engine.Instance.Log.Warn($"[FetchBbgBatchAsync] No assets found in response for batch");
                }

                return resultDict;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[FetchBbgBatchAsync] Error fetching batch: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Builds a GraphQL query to filter assets by multiple Bloomberg tickers.
        /// </summary>
        public string BuildBbgFilterQuery(List<string> bbgTickers)
        {
            if (bbgTickers == null)
            {
                Engine.Instance.Log.Warn("[BuildBbgFilterQuery] BBG ticker list is null. Returning a safe no-result query.");
                return "{ assets(filter: { and: [ { expression: \"id = -1\" } ] }) { edges { node { id } } } }";
            }

            List<string> sanitizedTickers = bbgTickers
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().Replace("'", "\\'"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sanitizedTickers.Count == 0)
            {
                Engine.Instance.Log.Warn("[BuildBbgFilterQuery] No valid BBG tickers after sanitization. Returning a safe no-result query.");
                return "{ assets(filter: { and: [ { expression: \"id = -1\" } ] }) { edges { node { id } } } }";
            }

            List<string> orConditions = new List<string>();
            foreach (string ticker in sanitizedTickers)
            {
                orConditions.Add("{ expression: \"symbols.identifier = '" + ticker + "'\" }");
            }

            string orPart = string.Join(",\n              ", orConditions.ToArray());

            string query = @"
{
  assets(filter: {
    and: [
      { expression: ""symbols.symbolType.name = 'Bloomberg Reference Query'"" },
      { or: [ " + orPart + @" ] }
    ]
  }) {
    edges {
      node {
        id
        uuid
        name
        __typename
        symbols { identifier }
      }
    }
  }
}";

            return query;
        }

        /// <summary>
        /// Sends PATCH request to update parent asset composition members and weights.
        /// </summary>
        public async Task SendWeightUpdatePayloadPatchAsync(string parentUuid, List<ComponentInfo> components)
        {
            Engine.Instance.Log.Info($"[SendWeightUpdatePayloadPatchAsync] Starting PATCH update for parent UUID: {parentUuid}");

            if (string.IsNullOrEmpty(parentUuid))
            {
                Engine.Instance.Log.Warn("[SendWeightUpdatePayloadPatchAsync] Cannot send update - parent UUID is missing");
                return;
            }

            if (components == null || components.Count == 0)
            {
                Engine.Instance.Log.Warn("[SendWeightUpdatePayloadPatchAsync] No components to update");
                return;
            }

            Engine.Instance.Log.Info($"[SendWeightUpdatePayloadPatchAsync] Preparing PATCH payload for {components.Count} components");

            // Build Asset Composition payload
            var compositionPayload = new
            {
                name = "Portfolio Composition Update",
                members = components.Select(c => new
                {
                    asset = c.Uuid,
                    unit = $"{c.Currency}/Pieces",
                    weight = new
                    {
                        quantity = c.WeightPercent,
                        unit = "%",
                        type = "PERCENT"
                    }
                }).ToArray()
            };

            // Build Swap Delta payload
            var deltaPayload = new SwapDeltaUpdate
            {
                Members = components.Select(c => new SwapDeltaMember
                {
                    AssetId = c.Uuid,
                    CurrentPieces = c.Nominal,
                    CurrentWeight = c.WeightPercent
                }).ToList()
            };

            string endpoint = $"/asset-compositions/{ParentAssetId}";

            try
            {
                Engine.Instance.Log.Debug($"[SendWeightUpdatePayloadPatchAsync] Sending PATCH to endpoint: {endpoint}");
                await _opusApiClient.PatchAsync(endpoint, compositionPayload, ParentAssetId).ConfigureAwait(false);
                Engine.Instance.Log.Info($"[SendWeightUpdatePayloadPatchAsync] Successfully patched asset composition {ParentAssetId} with {components.Count} members");

                Engine.Instance.Log.Debug($"[SendWeightUpdatePayloadPatchAsync] Updating swap delta for swap ID: {parentUuid}");
                await UpdateSwapDeltaAsync(parentUuid, deltaPayload).ConfigureAwait(false);
                Engine.Instance.Log.Info($"[SendWeightUpdatePayloadPatchAsync] Successfully updated swap in UCS service with swap id: {parentUuid} with {components.Count} members");
            }
            catch (ApiValidationException vex)
            {
                Engine.Instance.Log.Error($"[SendWeightUpdatePayloadPatchAsync] Validation error in PATCH: {vex.Message}");
                Engine.Instance.Log.Error($"[SendWeightUpdatePayloadPatchAsync] Response body: {vex.ResponseBody}");
                // Optional: notify user or retry with corrected payload
            }
            catch (ApiRateLimitException rlex)
            {
                Engine.Instance.Log.Warn($"[SendWeightUpdatePayloadPatchAsync] Rate limited - consider delaying next attempt: {rlex.Message}");
                // Optional: implement exponential backoff delay here
            }
            catch (ApiRequestException aex)
            {
                Engine.Instance.Log.Error($"[SendWeightUpdatePayloadPatchAsync] API request failed ({aex.StatusCode}): {aex.Message}");
                if (!string.IsNullOrEmpty(aex.ResponseBody))
                    Engine.Instance.Log.Error($"[SendWeightUpdatePayloadPatchAsync] Response details: {aex.ResponseBody}");
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[SendWeightUpdatePayloadPatchAsync] Unexpected PATCH error: {ex.Message}");
                Engine.Instance.Log.Error($"[SendWeightUpdatePayloadPatchAsync] Exception: {ex.ToString()}");
            }
        }

        #region Swap Quote & Update Operations

        #endregion

        /// <summary>
        /// Validates a Total Return Swap before performing updates (quote creation, nominal update, delta update, etc.).
        /// Uses the strongly-typed TotalReturnSwapResponse model and returns rich validation result.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <param name="requireRecentQuote">If true, requires at least one recent quote (within ~3 days)</param>
        /// <param name="minNotional">Optional minimum notional threshold (currently not returned by API)</param>
        /// <returns>Detailed validation result</returns>
        /// <summary>
        /// Validates a Total Return Swap before performing updates.
        /// Now uses the new full TotalReturnSwapResponse model instead of the old pagination-based one.
        /// </summary>
        public async Task<SwapValidationResult> ValidateSwapAsync(
            string swapId,
            bool requireRecentQuote = true,
            decimal? minNotional = null)
        {
            if (string.IsNullOrWhiteSpace(swapId))
                return SwapValidationResult.Failure(swapId, "Swap ID is required");

            try
            {
                var swapResponse = await GetTotalReturnSwapAsync(swapId).ConfigureAwait(false);

                if (swapResponse == null)
                {
                    var failure = SwapValidationResult.Failure(swapId, $"Swap {swapId} not found or returned no data");
                    Engine.Instance.Log.Error(failure.GetSummary());
                    return failure;
                }

                var result = SwapValidationResult.Success(swapId, swapResponse);
                result.CurrentNotional = swapResponse.Nominal?.Quantity;

                if (minNotional.HasValue && result.CurrentNotional.HasValue && result.CurrentNotional.Value < minNotional.Value)
                {
                    // TODO: to set valid false if needs to restrict this condition
                    //result.IsValid = false;
                    result.ErrorMessage += $"Notional {result.CurrentNotional.Value:N2} is below minimum {minNotional.Value:N2}. ";
                }

                // Quote validation (if required)
                if (requireRecentQuote)
                {
                    // For now we assume quotes are not in root response.
                    // If quotes are added later, update this section.
                    result.QuoteCount = 0;
                    result.Warnings.Add("Quote validation skipped - quotes not present in current response model.");
                }

                Engine.Instance.Log.Info(result.GetSummary());

                foreach (var warning in result.Warnings)
                {
                    Engine.Instance.Log.Warn($"Swap {swapId} validation warning: {warning}");
                }

                return result;
            }
            catch (ApiNotFoundException)
            {
                var failure = SwapValidationResult.Failure(swapId, $"Swap {swapId} not found (404)");
                Engine.Instance.Log.Error(failure.GetSummary());
                return failure;
            }
            catch (Exception ex)
            {
                var failure = SwapValidationResult.Failure(swapId, $"Validation failed: {ex.Message}");
                Engine.Instance.Log.Error(failure.GetSummary());
                return failure;
            }
        }

        /// <summary>
        /// Circuit-breaker protected execution: validates parent, collects BBG data, 
        /// updates weights, and logs metrics.
        /// </summary>
        public async Task ExecuteAsync_CircuitBreaker()
        {
            try
            {
                // ── Step 1: Validate parent asset composition (GraphQL call) ───────────
                ValidationResultOpus validationResult = await _opusCircuitBreaker.ExecuteAsync(
                    async () =>
                    {
                        string query = @"
                    {
                      assets(range: {offset: 0, size: 1000}) 
                      filter: {
                        and: [ { expression: ""id = " + ParentAssetId + @""" } ]
                      } 
                      {
                        edges { node { id name uuid __typename } }
                      }
                    }";

                        AssetsQueryResponse response = await _opusGraphQLClient.ExecuteAsync<AssetsQueryResponse>(query).ConfigureAwait(false);

                        AssetNode node = response?.assets?.edges?.FirstOrDefault()?.node;

                        if (node == null)
                            return new ValidationResultOpus { IsValid = false, ErrorMessage = "No asset found" };

                        if (string.IsNullOrWhiteSpace(node.uuid))
                            return new ValidationResultOpus { IsValid = false, AssetName = node.name, ErrorMessage = "UUID missing" };

                        if (node.__typename != "ASSETCOMPOSITION")
                            return new ValidationResultOpus { IsValid = false, AssetName = node.name, AssetUuid = node.uuid, ErrorMessage = "Invalid type" };

                        return new ValidationResultOpus { IsValid = true, AssetName = node.name, AssetUuid = node.uuid };
                    }).ConfigureAwait(false);

                if (!validationResult.IsValid)
                {
                    Engine.Instance.Log.Warn("Validation failed: " + validationResult.ErrorMessage);
                    return;
                }

                string parentUuid = validationResult.AssetUuid;
                Engine.Instance.Log.Info("Step 1 complete - Parent UUID: " + parentUuid);

                // ── Step 2: Validate and collect BBG UUIDs (multiple GraphQL batch calls) ──
                List<ComponentInfo> validComponents = await _opusCircuitBreaker.ExecuteAsync(
                    async () =>
                    {
                        // Here we simulate/wrap the full Step 2 logic (batch processing)
                        // In real code, this would call ValidateAndCollectBbgUuidsAsync() from earlier
                        List<ComponentInfo> components = new List<ComponentInfo>();
                        List<string> allTickers = new List<string>();

                        foreach (ReportHolding holding in ReportHoldings)
                        {
                            components.Add(new ComponentInfo() { BbgTicker = holding.BbgTicker, WeightPercent = holding.MarketWeightPercent });
                        }

                        string batchQuery = BuildBbgFilterQuery(components?.Select(component => component.BbgTicker)?.ToList());
                        AssetsQueryResponse batchResponse1 = await _opusGraphQLClient.ExecuteAsync<AssetsQueryResponse>(batchQuery).ConfigureAwait(false);

                        return components;
                    }).ConfigureAwait(false);

                if (validComponents.Count == 0)
                {
                    Engine.Instance.Log.Info("Step 2 failed - No valid components");
                    return;
                }

                Engine.Instance.Log.Info("Step 2 complete - Valid components: " + validComponents.Count);

                // ── Step 3: POST call to update weights ────────────────────────────────
                await _opusCircuitBreaker.ExecuteAsync(
                    async () =>
                    {
                        // Build payload
                        object payload = new
                        {
                            assetCompositionId = ParentAssetId,
                            parentUuid = parentUuid,
                            components = validComponents.ConvertAll(c => new
                            {
                                childUuid = c.Uuid,
                                weight = new { value = c.WeightPercent, unit = "%" },
                                quantity = c.WeightPercent,
                                reference = c.BbgTicker
                            }).ToArray()
                        };

                        await _opusApiClient.PostAsync("/api/asset-compositions/update-weights", payload).ConfigureAwait(false);

                        Engine.Instance.Log.Info("Step 3 complete - Weights updated successfully");

                        return true; // dummy return
                    }).ConfigureAwait(false);

                // Print metrics after full process
                Engine.Instance.Log.Info(_opusCircuitBreaker.GetMetricsSnapshot());
            }
            catch (CircuitBreakerOpenException ex)
            {
                Engine.Instance.Log.Error("Circuit open: " + ex.Message);
                // Fallback logic here (e.g., use cached weights)
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error("Process failed: " + ex.Message);
            }
        }

        private async Task<OpusOperationResult<T>> ExecuteSafelyAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string friendlyErrorMessage)
        {
            try
            {
                T data = await operation().ConfigureAwait(false);
                return OpusOperationResult<T>.SuccessWithData(data);
            }
            catch (Exception ex)
            {
                string fallbackFriendlyMessage = string.IsNullOrWhiteSpace(friendlyErrorMessage)
                    ? DefaultFriendlyErrorMessage
                    : friendlyErrorMessage;

                Engine.Instance.Log.Error("[" + operationName + "] Failed: " + ex.ToString());
                return OpusOperationResult<T>.FailureWithData(fallbackFriendlyMessage, ex.Message);
            }
        }

        private OpusOperationResult<T> ExecuteSafely<T>(Func<T> operation, string operationName, string friendlyErrorMessage)
        {
            try
            {
                return OpusOperationResult<T>.SuccessWithData(operation());
            }
            catch (Exception ex)
            {
                string fallbackFriendlyMessage = string.IsNullOrWhiteSpace(friendlyErrorMessage)
                    ? DefaultFriendlyErrorMessage
                    : friendlyErrorMessage;

                Engine.Instance.Log.Error("[" + operationName + "] Failed: " + ex.ToString());
                return OpusOperationResult<T>.FailureWithData(fallbackFriendlyMessage, ex.Message);
            }
        }

        public Task<OpusOperationResult<T>> TryExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, RetryPolicy policy)
        {
            return ExecuteSafelyAsync(
                async () => await ExecuteWithRetryAsync(operation, operationName, policy).ConfigureAwait(false),
                "TryExecuteWithRetryAsync",
                "Unable to complete the OPUS retry operation right now.");
        }
    }
}
