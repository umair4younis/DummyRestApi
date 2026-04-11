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
    public partial class OpusWeightUpdateProcessor
    {

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

    }
}
