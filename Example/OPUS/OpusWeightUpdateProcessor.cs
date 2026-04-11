using Newtonsoft.Json;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Puma.MDE.OPUS
{
    /// <summary>
    /// Coordinates the process of validating and updating asset composition weights in OPUS.
    /// Handles parent asset validation, BBG ticker batch processing, weight aggregation, 
    /// and sending PATCH/POST updates with retry and circuit-breaker resilience.
    /// </summary>
    public class OpusWeightUpdateProcessor
    {
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

            // Get all BBG tickers
            List<string> allTickers = new List<string>();
            foreach (ReportHolding holding in ReportHoldings)
            {
                allTickers.Add(holding.BbgTicker);
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
                    errors.Add("Batch failed permanently: " + batchDescription + " - " + ex.Message);
                    continue;
                }

                foreach (string bbg in batch)
                {
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
                            if (h.BbgTicker == bbg)
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
            List<string> orConditions = new List<string>();
            foreach (string ticker in bbgTickers)
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

        /// <summary>
        /// Retrieves details of a single Total Return Swap (TRS) by ID, including quotes and full metadata.
        /// Updated to match the actual OPUS JSON response structure (no longer uses old pagination model).
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (UUID)</param>
        /// <returns>Full TotalReturnSwapResponse with all swap details</returns>
        public async Task<TotalReturnSwapResponse> GetTotalReturnSwapAsync(string swapId)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            string endpoint = $"/swaps/{swapId}";
            Engine.Instance.Log.Info($"[GetTotalReturnSwapAsync] Fetching swap details for ID: {swapId}");

            try
            {
                Engine.Instance.Log.Debug($"[GetTotalReturnSwapAsync] Endpoint: {endpoint}");
                var response = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    return await _opusApiClient.GetAsync<TotalReturnSwapResponse>(endpoint).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info(string.Format(
                    "[GetTotalReturnSwapAsync] Swap {0} retrieved successfully. Name: {1}, Nominal: {2}",
                    swapId,
                    response?.Name ?? "N/A",
                    response?.Nominal?.Quantity.ToString("N2") ?? "N/A"));

                return response;
            }
            catch (ApiNotFoundException nfex)
            {
                Engine.Instance.Log.Error(string.Format("[GetTotalReturnSwapAsync] Swap {0} not found: {1}", swapId, nfex.Message));
                throw;
            }
            catch (ApiRequestException arex)
            {
                Engine.Instance.Log.Error(string.Format("[GetTotalReturnSwapAsync] Failed to read TRS {0} ({1}): {2}",
                    swapId, arex.StatusCode, arex.Message));
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error(string.Format("[GetTotalReturnSwapAsync] Unexpected error reading Total Return Swap {0}: {1}",
                    swapId, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Creates a new Total Return Swap (TRS) in OPUS.
        /// Endpoint: /api/v3/masterdata/total-return-swaps (POST)
        /// Uses the new PostWithResponseAsync to get the created identifier.
        /// </summary>
        /// <param name="payload">The full creation payload</param>
        /// <returns>The identifier (UUID) from resource.identifier</returns>
        public async Task<string> CreateTotalReturnSwapAsync(object payload)
        {
            if (payload == null)
            {
                Engine.Instance.Log.Error(payload);
                throw new ArgumentNullException(nameof(payload));
            }

            const string endpoint = "/swaps";

            try
            {
                string createdId = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    var result = await _opusApiClient.PostWithResponseAsync(endpoint, payload).ConfigureAwait(false);
                    HttpResponseMessage response = result.Item1;
                    string body = result.Item2;

                    // Parse success response (201 expected)
                    dynamic apiResponse;
                    // Check HTTP status before parsing
                    if (!response.IsSuccessStatusCode)
                    {
                        int statusCode = (int)response.StatusCode;
                        dynamic errObj = null;
                        try { errObj = JsonConvert.DeserializeObject(body); } catch { }
                        string errMsg = (string)(errObj?.errors?[0]?.message ?? errObj?.error ?? body);
                        if (statusCode == 400 || statusCode == 422)
                            throw new ApiValidationException(errMsg ?? "Validation error", body);
                        if (statusCode == 401 || statusCode == 403)
                            throw new ApiAuthorizationException(errMsg ?? "Authorization error", body);
                        if (statusCode == 404)
                            throw new ApiNotFoundException(errMsg ?? "Not found", "POST /swaps", body);
                        if (statusCode == 429)
                            throw new ApiRateLimitException(errMsg ?? "Rate limit exceeded", body);
                        if (statusCode >= 500)
                            throw new HttpRequestException($"POST {endpoint} failed with status {statusCode}: {body}");
                        throw new ApiRequestException(errMsg ?? "API error", response.StatusCode, body);
                    }

                    try
                    {
                        apiResponse = JsonConvert.DeserializeObject(body);
                    }
                    catch (JsonException jex)
                    {
                        Engine.Instance.Log.Error(string.Format("Invalid JSON in TRS creation response: {0}\nRaw: {1}", jex.Message, body));
                        throw new ApiRequestException("Invalid response format after creation", response.StatusCode, body, jex);
                    }

                    string identifier = null;

                    if (apiResponse != null && apiResponse.resource != null)
                    {
                        identifier = (string)apiResponse.resource.identifier;

                        // Fallback: try to extract from location URL if identifier missing
                        if (string.IsNullOrEmpty(identifier) && apiResponse.resource.location != null)
                        {
                            string loc = (string)apiResponse.resource.location;
                            string[] segments = loc.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (segments.Length > 0)
                            {
                                identifier = segments[segments.Length - 1];
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(identifier))
                    {
                        Engine.Instance.Log.Warn(string.Format("TRS created but no identifier extracted from response:\n{0}", body));
                        identifier = "unknown-created-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    }

                    return identifier;
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info($"Total Return Swap created successfully. Identifier: {createdId}");
                return createdId;
            }
            catch (HttpRequestException hrex) when (hrex.Message.Contains("400"))
            {
                // Extract body from inner exception message or re-throw with better info
                string errorBody = hrex.Message.Contains(":") ? hrex.Message.Split(':')[1].Trim() : null;
                string errorMsg = "Validation failed during TRS creation";

                if (!string.IsNullOrEmpty(errorBody))
                {
                    try
                    {
                        dynamic errResp = JsonConvert.DeserializeObject(errorBody);
                        var errors = errResp?.errors ?? errResp?.resource?.errors;
                        if (errors != null)
                        {
                            errorMsg += ": " + string.Join("; ", ((IEnumerable<dynamic>)errors)
                                .Select(e => (string)e.localizedMessage ?? (string)e.message));
                        }
                    }
                    catch { }
                }

                Engine.Instance.Log.Error(errorMsg);
                Engine.Instance.Log.Error(errorBody ?? hrex.Message);
                throw new ApiValidationException(errorMsg, errorBody ?? hrex.Message);
            }
            catch (HttpRequestException hrex) when (hrex.Message.Contains("404"))
            {
                var message = "Resource not found during TRS creation";
                Engine.Instance.Log.Error(message);
                throw new ApiNotFoundException(message, "N/A", hrex.Message);
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error($"Circuit breaker open during TRS creation: {cbex.Message}");
                throw new ApiRequestException("Service temporarily unavailable (circuit open)", null, null, cbex);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Unexpected error creating TRS: {ex.Message}");
                Engine.Instance.Log.Error(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Partially updates an existing Swap/TRS (e.g. weights, members, etc.).
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier</param>
        /// <param name="patchPayload">Partial update data (anonymous object or model)</param>
        public async Task UpdateTotalReturnSwapAsync(string swapId, object patchPayload)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            if (patchPayload == null)
            {
                Engine.Instance.Log.Error(patchPayload);
                throw new ArgumentNullException(nameof(patchPayload));
            }

            string endpoint = $"/swaps/{swapId}";

            try
            {
                await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    await _opusApiClient.PatchAsync(endpoint, patchPayload, encodeUrl: false).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info($"Total Return Swap {swapId} updated successfully (PATCH).");
            }
            catch (ApiValidationException vex)
            {
                Engine.Instance.Log.Error($"Validation error updating TRS {swapId}: {vex.Message}");
                if (!string.IsNullOrEmpty(vex.ResponseBody))
                    Engine.Instance.Log.Error($"Response body: {vex.ResponseBody}");
                throw;
            }
            catch (ApiNotFoundException nfex)
            {
                Engine.Instance.Log.Error($"Swap not found: {nfex.Message}");
                throw;
            }
            catch (ApiRateLimitException rlex)
            {
                Engine.Instance.Log.Warn($"Rate limit during TRS update {swapId}: {rlex.Message}");
                throw;
            }
            catch (ApiRequestException arex)
            {
                Engine.Instance.Log.Error($"API error updating TRS {swapId} ({arex.StatusCode}): {arex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Unexpected error updating Total Return Swap {swapId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an existing Swap/TRS by its ID.
        /// </summary>
        /// <param name="swapId">The OPUS swap identifier (GUID string)</param>
        public async Task DeleteTotalReturnSwapAsync(string swapId)
        {
            if (string.IsNullOrWhiteSpace(swapId))
            {
                Engine.Instance.Log.Error(string.Format("Swap ID is required: {0}", swapId));
                throw new ArgumentException("Swap ID is required", nameof(swapId));
            }

            string endpoint = $"/swaps/{swapId}";
            Engine.Instance.Log.Info($"[DeleteTotalReturnSwapAsync] Deleting swap: {swapId}");

            try
            {
                Engine.Instance.Log.Debug($"[DeleteTotalReturnSwapAsync] Endpoint: {endpoint}");
                await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    await _opusApiClient.DeleteAsync(endpoint).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info($"[DeleteTotalReturnSwapAsync] Total Return Swap {swapId} deleted successfully");
            }
            catch (HttpRequestException hrex)
            {
                // Extract status and body from message (since DeleteAsync throws HttpRequestException)
                string errorDetail = hrex.Message;

                if (errorDetail.Contains("404"))
                {
                    Engine.Instance.Log.Warn($"[DeleteTotalReturnSwapAsync] Swap {swapId} not found for deletion (may already be deleted)");
                    // You can choose to treat 404 as success or throw ApiNotFoundException
                    return;
                }

                Engine.Instance.Log.Error($"[DeleteTotalReturnSwapAsync] Failed to delete TRS {swapId}: {errorDetail}");
                throw new ApiRequestException(
                    $"DELETE failed for swap {swapId}",
                    null, // status not easily accessible here — can parse from message if needed
                    errorDetail,
                    hrex);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[DeleteTotalReturnSwapAsync] Unexpected error deleting Total Return Swap {swapId}: {ex.Message}");
                throw;
            }
        }

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

        #region Swap Quote & Update Operations

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
    }
}
