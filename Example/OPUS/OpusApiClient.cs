using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Net;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using System.Linq;
using Puma.MDE.OPUS.Utilities;


namespace Puma.MDE.OPUS
{
    /// <summary>
    /// HTTP client wrapper for interacting with the OPUS REST API.
    /// Supports both standard masterdata endpoints and unicredit-swap-service endpoints.
    /// </summary>
    public class OpusApiClient
    {
        private readonly OpusHttpClientHandler _opusHttpClientHandler;
        private readonly OpusTokenProvider _opusTokenProvider;
        private readonly OpusConfiguration _opusConfiguration;
        private readonly OpusCircuitBreaker _opusCircuitBreaker;
        public HttpClient _httpClient;

        /// <summary>
        /// Initializes the API client with handler, token provider, configuration, and circuit breaker.
        /// </summary>
        public OpusApiClient(OpusHttpClientHandler opusHttpClientHandler,
            OpusTokenProvider opusTokenProvider, OpusConfiguration opusConfiguration)
        {
            _opusHttpClientHandler = opusHttpClientHandler;
            _opusTokenProvider = opusTokenProvider ?? throw new ArgumentNullException(nameof(opusTokenProvider));
            _opusConfiguration = opusConfiguration ?? throw new ArgumentNullException(nameof(opusConfiguration));

            _httpClient = opusHttpClientHandler != null
                ? new HttpClient(opusHttpClientHandler._opusHttpClientHandler)
                : new HttpClient();

            // Critical: Set timeout to prevent indefinite hangs. Default is 100 seconds, but be explicit.
            // Network timeouts should be shorter than circuit breaker break duration (90s by default).
            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            _opusCircuitBreaker = new OpusCircuitBreaker(
                failureThreshold: 4,
                breakSeconds: 90
            );

            Engine.Instance.Log.Info($"[OpusApiClient] Initialized with timeout=60s, circuit breaker(threshold=4, break=90s)");
        }

        /// <summary>
        /// Builds the full URL using the correct base depending on the endpoint.
        /// Uses GetBaseUrlForEndpoint to support both /opus/api/v3/masterdata and /unicredit-swap-service/api paths.
        /// </summary>
        private string BuildFullUrl(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return _opusConfiguration.RestUrl;

            // Remove leading slash for clean concatenation
            endpoint = endpoint.TrimStart('/');

            string baseUrl = _opusConfiguration.GetBaseUrlForEndpoint(endpoint);
            return $"{baseUrl.TrimEnd('/')}/{endpoint}";
        }

        /// <summary>
        /// Sends a GET request to the specified endpoint and deserializes the response.
        /// </summary>
        public virtual async Task<T> GetAsync<T>(string endpoint)
        {
            Engine.Instance.Log.Info($"[OpusApiClient] GetAsync<{typeof(T).Name}> started for endpoint: {endpoint}");

            try
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);
                Engine.Instance.Log.Debug($"[OpusApiClient] GET request to: {fullUrl}");

                T result = await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(fullUrl).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var message = string.Format("GET {0} failed with status {1}", endpoint, response.StatusCode);
                        Engine.Instance.Log.Error($"[OpusApiClient] {message}");
                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Debug($"[OpusApiClient] GET succeeded, deserializing response type {typeof(T).Name}");
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializerSettingsProvider.Deserialize<T>(json);
                }).ConfigureAwait(false);

                Engine.Instance.Log.Info($"[OpusApiClient] GetAsync<{typeof(T).Name}> completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[OpusApiClient] GetAsync<{typeof(T).Name}> failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a GET request and returns both the full response and body string.
        /// </summary>
        public async Task<Tuple<HttpResponseMessage, string>> GetWithResponseAsync(string endpoint)
        {
            Engine.Instance.Log.Info($"[GET] Starting request to endpoint: {endpoint}");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);

                HttpResponseMessage response = await _httpClient.GetAsync(fullUrl).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var message = $"[GET] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {body.Substring(0, Math.Min(500, body.Length))}";
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                Engine.Instance.Log.Info($"[GET] Succeeded: {endpoint} - Status: {response.StatusCode}");
                return new Tuple<HttpResponseMessage, string>(response, body);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a POST request with the provided data.
        /// </summary>
        public virtual async Task PostAsync(string endpoint, object data)
        {
            Engine.Instance.Log.Info($"[POST] Starting request to endpoint: {endpoint}");

            try
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string jsonBody = JsonSerializerSettingsProvider.Serialize(data);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                string fullUrl = BuildFullUrl(endpoint);
                Engine.Instance.Log.Debug($"[POST] Full URL: {fullUrl}");

                await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await _httpClient.PostAsync(fullUrl, content).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var message = string.Format("POST {0} failed with status {1}", endpoint, response.StatusCode);
                        Engine.Instance.Log.Error(message);
                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Info($"[POST] Succeeded: {endpoint} - Status: {response.StatusCode}");
                    return response;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[POST] Failed for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends a POST request and returns both the response and body string.
        /// </summary>
        public virtual async Task<Tuple<HttpResponseMessage, string>> PostWithResponseAsync(string endpoint, object data)
        {
            Engine.Instance.Log.Info($"[POST] Starting request to endpoint: {endpoint}");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);

                string json = JsonSerializerSettingsProvider.Serialize(data);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await _httpClient.PostAsync(fullUrl, content).ConfigureAwait(false);
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var message = $"[POST] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {body.Substring(0, Math.Min(500, body.Length))}";
                        Engine.Instance.Log.Error(message);

                        if ((int)response.StatusCode == 429)
                            throw new ApiRateLimitException("Rate limit exceeded", body);

                        if ((int)response.StatusCode >= 500)
                            throw new ApiServerException($"Server error {response.StatusCode}", body);

                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Info($"[POST] Succeeded: {endpoint} - Status: {response.StatusCode}");
                    return new Tuple<HttpResponseMessage, string>(response, body);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Generic version of PostWithResponseAsync for strongly-typed responses.
        /// Used by FetchSwapDeltaAsync and other methods.
        /// </summary>
        public virtual async Task<T> PostWithResponseAsync<T>(string endpoint, object data)
        {
            Engine.Instance.Log.Info($"[POST Generic] Starting request to endpoint: {endpoint}");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);

                string json = JsonSerializerSettingsProvider.Serialize(data);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await _httpClient.PostAsync(fullUrl, content).ConfigureAwait(false);
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var message = $"[POST Generic] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {body.Substring(0, Math.Min(500, body.Length))}";
                        Engine.Instance.Log.Error(message);
                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Info($"[POST Generic] Succeeded: {endpoint} - Status: {response.StatusCode}");
                    return JsonSerializerSettingsProvider.Deserialize<T>(body);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// PATCH request (standard).
        /// </summary>
        public virtual async Task PatchAsync(string endpoint, object patchPayload, bool encodeUrl = true, int timeoutMs = 0)
        {
            Engine.Instance.Log.Info($"[PATCH] Starting request to endpoint: {endpoint}");

            try
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);
                if (encodeUrl)
                {
                    fullUrl = Uri.EscapeUriString(fullUrl);
                }

                Engine.Instance.Log.Debug($"[PATCH] Full URL: {fullUrl}, EncodeUrl: {encodeUrl}, TimeoutMs: {timeoutMs}");

                if (timeoutMs > 0)
                    _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

                await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await _httpClient.PatchAsync(fullUrl, patchPayload).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var message = string.Format($"PATCH {endpoint} failed ({response.StatusCode}): {errorBody}");
                        Engine.Instance.Log.Error(message);
                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Info($"[PATCH] Succeeded: {endpoint} - Status: {response.StatusCode}");
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[PATCH] Failed for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// PATCH with parent asset ID context.
        /// </summary>
        public virtual async Task PatchAsync(string endpoint, object patchPayload, string parentAssetId)
        {
            await PrepareAuthHeaderAsync().ConfigureAwait(false);

            string fullUrl = BuildFullUrl(endpoint);

            HttpResponseMessage response = null;
            string responseBody = null;

            try
            {
                // Wrap in circuit breaker for consistency with other methods
                await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    response = await _httpClient.PatchAsync(fullUrl, patchPayload).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        Engine.Instance.Log.Info($"PATCH {endpoint} succeeded ({response.StatusCode})");
                        return true;
                    }

                    responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return false;
                }).ConfigureAwait(false);

                // If we get here and response succeeded, return early
                if (response?.IsSuccessStatusCode == true)
                {
                    return;
                }
            }
            catch (CircuitBreakerOpenException cbex)
            {
                Engine.Instance.Log.Error($"Circuit breaker open - PATCH aborted: {cbex.Message}");
                throw new ApiRequestException("Service temporarily unavailable (circuit open)", null, null);
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"Unexpected error during PATCH {endpoint}: {ex.Message}");
                throw new ApiRequestException("PATCH request failed unexpectedly", null, ex.Message);
            }
            finally
            {
                response?.Dispose();  // Always dispose response to prevent connection exhaustion
            }

            // Error handling logic remains the same...
            string errorDetail = "Unknown error";
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    dynamic errorJson = JsonSerializerSettingsProvider.Deserialize<dynamic>(responseBody);
                    errorDetail = errorJson?.error?.message ?? errorJson?.errors?[0]?.message ?? responseBody;
                }
                catch
                {
                    errorDetail = responseBody.Length > 500 ? responseBody.Substring(0, 500) + "..." : responseBody;
                }
            }

            var statusCode = (int)response?.StatusCode;
            string logMessage = $"PATCH {endpoint} failed ({statusCode}): {errorDetail}";

            if (statusCode == 400)
            {
                Engine.Instance.Log.Error($"Validation error: {logMessage}");
                throw new ApiValidationException($"Invalid request: {errorDetail}", responseBody);
            }
            else if (statusCode == 401 || statusCode == 403)
            {
                Engine.Instance.Log.Error($"Authentication error: {logMessage}");
                throw new ApiRequestException("Authentication failed - check token", (HttpStatusCode)statusCode, responseBody);
            }
            else if (statusCode == 404)
            {
                Engine.Instance.Log.Error($"Resource not found: {logMessage}");
                throw new ApiRequestException($"Asset composition not found: {parentAssetId}", (HttpStatusCode)statusCode, responseBody);
            }
            else if (statusCode == 429)
            {
                Engine.Instance.Log.Warn($"Rate limited: {logMessage}");
                throw new ApiRateLimitException($"Rate limit exceeded - retry later", responseBody);
            }
            else if (statusCode >= 500)
            {
                Engine.Instance.Log.Error($"Server error: {logMessage}");
                throw new ApiRequestException("public server error in OPUS", (HttpStatusCode)statusCode, responseBody);
            }
            else
            {
                Engine.Instance.Log.Error($"Unexpected HTTP error: {logMessage}");
                throw new ApiRequestException($"PATCH failed with status {statusCode}", (HttpStatusCode)statusCode, responseBody);
            }
        }

        /// <summary>
        /// PATCH with response body.
        /// </summary>
        public virtual async Task<Tuple<HttpResponseMessage, string>> PatchWithResponseAsync(string endpoint, object patchPayload)
        {
            Engine.Instance.Log.Info($"[PATCH] Starting request to endpoint: {endpoint}");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);

                HttpResponseMessage response = await _httpClient.PatchAsync(fullUrl, patchPayload).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var message = $"[PATCH] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {body.Substring(0, Math.Min(500, body.Length))}";
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                Engine.Instance.Log.Info($"[PATCH] Succeeded: {endpoint} - Status: {response.StatusCode}");
                return new Tuple<HttpResponseMessage, string>(response, body);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// PUT request.
        /// </summary>
        public virtual async Task PutAsync(string endpoint, object data)
        {
            Engine.Instance.Log.Info($"[PUT] Starting request to endpoint: {endpoint}");

            await PrepareAuthHeaderAsync().ConfigureAwait(false);

            string fullUrl = BuildFullUrl(endpoint);

            string jsonBody = JsonSerializerSettingsProvider.Serialize(data);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                await _opusCircuitBreaker.ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await _httpClient.PutAsync(fullUrl, content).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var message = $"[PUT] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {errorBody.Substring(0, Math.Min(500, errorBody.Length))}";
                        Engine.Instance.Log.Error(message);
                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Info($"[PUT] Succeeded: {endpoint} - Status: {response.StatusCode}");
                    return response;
                }).ConfigureAwait(false);
            }
            finally
            {
                content.Dispose();
            }
        }

        /// <summary>
        /// PUT with response.
        /// </summary>
        public virtual async Task<Tuple<HttpResponseMessage, string>> PutWithResponseAsync(string endpoint, object data)
        {
            Engine.Instance.Log.Info($"[PUT] Starting request to endpoint: {endpoint}");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);

                string json = JsonSerializerSettingsProvider.Serialize(data);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await _httpClient.PutAsync(fullUrl, content).ConfigureAwait(false);
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var message = $"[PUT] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {body.Substring(0, Math.Min(500, body.Length))}";
                        Engine.Instance.Log.Error(message);
                        throw new HttpRequestException(message);
                    }

                    Engine.Instance.Log.Info($"[PUT] Succeeded: {endpoint} - Status: {response.StatusCode}");
                    return new Tuple<HttpResponseMessage, string>(response, body);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// DELETE request.
        /// </summary>
        public virtual async Task DeleteAsync(string endpoint)
        {
            await PrepareAuthHeaderAsync().ConfigureAwait(false);

            string fullUrl = BuildFullUrl(endpoint);

            await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                HttpResponseMessage response = await _httpClient.DeleteAsync(fullUrl).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var message = string.Format("DELETE {0} failed with status {1}: {2}", endpoint, (int)response.StatusCode, errorBody);
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                return response;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// DELETE with response.
        /// </summary>
        public virtual async Task<Tuple<HttpResponseMessage, string>> DeleteWithResponseAsync(string endpoint)
        {
            Engine.Instance.Log.Info($"[DELETE] Starting request to endpoint: {endpoint}");

            return await _opusCircuitBreaker.ExecuteAsync(async () =>
            {
                await PrepareAuthHeaderAsync().ConfigureAwait(false);

                string fullUrl = BuildFullUrl(endpoint);

                HttpResponseMessage response = await _httpClient.DeleteAsync(fullUrl).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var message = $"[DELETE] Failed: {endpoint} - Status: {(int)response.StatusCode} - Body: {body.Substring(0, Math.Min(500, body.Length))}";
                    Engine.Instance.Log.Error(message);
                    throw new HttpRequestException(message);
                }

                Engine.Instance.Log.Info($"[DELETE] Succeeded: {endpoint} - Status: {response.StatusCode}");
                return new Tuple<HttpResponseMessage, string>(response, body);
            }).ConfigureAwait(false);
        }

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

        /// <summary>
        /// Prepares the Authorization header with a fresh access token.
        /// </summary>
        private async Task PrepareAuthHeaderAsync()
        {
            string token = await _opusTokenProvider.GetAccessTokenAsync().ConfigureAwait(false);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}