using Puma.MDE.OPUS.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Puma.MDE.OPUS.Exceptions;


namespace Puma.MDE.OPUS.Tests
{
    public class FakeOpusApiClient : OpusApiClient
    {
        // Call tracking flags
        public bool GetAsyncCalled { get; private set; }
        public bool PostAsyncCalled { get; private set; }
        public bool PatchAsyncCalled { get; private set; }
        public bool DeleteAsyncCalled { get; private set; }
        public bool PostWithResponseAsyncCalled { get; private set; }
        public bool PatchWithResponseAsyncCalled { get; private set; }
        public bool DeleteWithResponseAsyncCalled { get; private set; }
        public bool PutAsyncCalled { get; private set; }
        public bool PostWithResponseGenericCalled { get; private set; }
        public bool UpdateSwapDeltaAsyncCalled { get; private set; }

        // Circuit breaker simulation
        private Exception _circuitBreakerException;

        public void SetCircuitBreakerToThrow(Exception ex)
        {
            _circuitBreakerException = ex;
        }

        // Fixed results
        private object _getAsyncResult;
        private Tuple<HttpResponseMessage, string> _postWithResponseFixedResult;
        private Tuple<HttpResponseMessage, string> _patchWithResponseFixedResult;
        private Tuple<HttpResponseMessage, string> _putWithResponseFixedResult;
        private Tuple<HttpResponseMessage, string> _deleteWithResponseFixedResult;

        // Behaviors
        private Func<string, object, Tuple<HttpResponseMessage, string>> _postWithResponseBehavior;
        private Func<string, object, Tuple<HttpResponseMessage, string>> _patchWithResponseBehavior;
        private Func<string, object, HttpResponseMessage> _patchAsyncBehavior;
        private Func<string, string, SwapDeltaUpdate, Tuple<HttpResponseMessage, string>> _putWithResponseBehavior; // updated for new signature
        private Func<string, Tuple<HttpResponseMessage, string>> _deleteWithResponseBehavior;

        private Exception _patchAsyncThrowException;

        public FakeOpusApiClient()
        : base(
            new OpusHttpClientHandler(new FakeOpusConfiguration()),  // Real handler with fake config
            new FakeTokenProvider(new FakeOpusConfiguration()),      // Real token provider with fake config
            new FakeOpusConfiguration())                             // Fake configuration
        {
            Reset();   // Ensure clean state
        }

        public void Reset()
        {
            GetAsyncCalled = PostAsyncCalled = PatchAsyncCalled = DeleteAsyncCalled = false;
            PostWithResponseAsyncCalled = PatchWithResponseAsyncCalled = DeleteWithResponseAsyncCalled = false;
            PutAsyncCalled = false;
            PostWithResponseGenericCalled = false;
            UpdateSwapDeltaAsyncCalled = false;

            _circuitBreakerException = null;
            _getAsyncResult = null;
            _postWithResponseFixedResult = null;
            _patchWithResponseFixedResult = null;
            _putWithResponseFixedResult = null;
            _deleteWithResponseFixedResult = null;

            _postWithResponseBehavior = null;
            _patchWithResponseBehavior = null;
            _putWithResponseBehavior = null;
            _deleteWithResponseBehavior = null;
            _patchAsyncThrowException = null;
        }

        // ── GetAsync ───────────────────────────────────────────────────────────────
        public void SetGetAsyncResult<T>(T result)
        {
            _getAsyncResult = result;
        }

        public void SetGetAsyncToThrow(Exception ex)
        {
            _getAsyncResult = ex;
        }

        public override Task<T> GetAsync<T>(string endpoint)
        {
            GetAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_getAsyncResult is Exception ex) throw ex;

            return Task.FromResult(_getAsyncResult is T t ? t : default(T));
        }

        // ── PostAsync ──────────────────────────────────────────────────────────────
        public void SetPostAsyncResult() { }

        public override Task PostAsync(string endpoint, object data)
        {
            PostAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            return Task.CompletedTask;
        }

        // ── PostWithResponseAsync (non-generic) ────────────────────────────────────
        public void SetPostWithResponseResult(HttpResponseMessage response)
        {
            string body = response.Content != null
                ? response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                : "";
            _postWithResponseFixedResult = new Tuple<HttpResponseMessage, string>(response, body);
        }

        public void SetPostWithResponseBehavior(Func<string, object, Tuple<HttpResponseMessage, string>> behavior)
        {
            _postWithResponseBehavior = behavior;
        }

        public override Task<Tuple<HttpResponseMessage, string>> PostWithResponseAsync(string endpoint, object payload)
        {
            PostWithResponseAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            if (_postWithResponseBehavior != null)
                return Task.FromResult(_postWithResponseBehavior(endpoint, payload));

            if (_postWithResponseFixedResult != null)
                return Task.FromResult(_postWithResponseFixedResult);

            var defaultResp = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"resource\":{\"identifier\":\"mock-created\"}}")
            };
            string defaultBody = defaultResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return Task.FromResult(new Tuple<HttpResponseMessage, string>(defaultResp, defaultBody));
        }

        // ── Generic PostWithResponseAsync<T> ───────────────────────────────────────
        public override async Task<T> PostWithResponseAsync<T>(string endpoint, object payload)
        {
            PostWithResponseGenericCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            if (_postWithResponseBehavior != null)
            {
                var tuple = _postWithResponseBehavior(endpoint, payload);
                if (!string.IsNullOrEmpty(tuple.Item2))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<T>(tuple.Item2);
                    }
                    catch { }
                }
            }

            if (_postWithResponseFixedResult != null && !string.IsNullOrEmpty(_postWithResponseFixedResult.Item2))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(_postWithResponseFixedResult.Item2);
                }
                catch { }
            }

            return default(T);
        }

        // ── Convenience for strongly-typed Post ────────────────────────────────────
        public void SetPostWithResponseResult<T>(T result)
        {
            if (result == null)
            {
                _postWithResponseFixedResult = null;
                return;
            }

            string json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            _postWithResponseFixedResult = new Tuple<HttpResponseMessage, string>(response, json);
        }

        // ── PatchAsync ─────────────────────────────────────────────────────────────
        public void SetPatchAsyncResult() { }

        public void SetPatchAsyncToThrow(Exception ex)
        {
            _patchAsyncThrowException = ex;
        }

        public void SetPatchAsyncBehavior(Func<string, object, HttpResponseMessage> behavior)
        {
            _patchAsyncBehavior = behavior;
        }

        public void SetPatchWithResponseBehavior(Func<string, object, Tuple<HttpResponseMessage, string>> behavior)
        {
            _patchWithResponseBehavior = behavior;
        }

        public override Task PatchAsync(string endpoint, object data, string parentAssetId)
        {
            PatchAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_patchAsyncThrowException != null) throw _patchAsyncThrowException;
            return Task.CompletedTask;
        }

        // ── PatchWithResponseAsync ─────────────────────────────────────────────────
        public void SetPatchWithResponseResult(HttpResponseMessage response)
        {
            string body = response.Content != null
                ? response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                : "";
            _patchWithResponseFixedResult = new Tuple<HttpResponseMessage, string>(response, body);
        }

        public override Task<Tuple<HttpResponseMessage, string>> PatchWithResponseAsync(string endpoint, object data)
        {
            PatchWithResponseAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            if (_patchWithResponseBehavior != null)
                return Task.FromResult(_patchWithResponseBehavior(endpoint, data));

            if (_patchWithResponseFixedResult != null)
                return Task.FromResult(_patchWithResponseFixedResult);

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"updated\":true}")
            };
            return Task.FromResult(new Tuple<HttpResponseMessage, string>(resp, "{\"updated\":true}"));
        }

        // ── PutAsync & PutWithResponseAsync ────────────────────────────────────────
        public void SetPutAsyncResult() { }

        public void SetPutWithResponseResult(HttpResponseMessage response)
        {
            string body = response.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
            _putWithResponseFixedResult = Tuple.Create(response, body);
        }

        public void SetPutWithResponseResult<T>(T result)
        {
            if (result == null)
            {
                _putWithResponseFixedResult = null;
                return;
            }

            string json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            _putWithResponseFixedResult = Tuple.Create(response, json);
        }

        public override Task PutAsync(string endpoint, object data)
        {
            PutAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            return Task.CompletedTask;
        }

        public override Task<Tuple<HttpResponseMessage, string>> PutWithResponseAsync(string endpoint, object payload)
        {
            PutAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            if (_putWithResponseBehavior != null)
            {
                // Note: _putWithResponseBehavior expects 3 params now, but we keep backward for safety
                // For simplicity we ignore the extra param in fake if not needed
                return Task.FromResult(_putWithResponseBehavior != null
                    ? _putWithResponseBehavior(endpoint, null, payload as SwapDeltaUpdate)
                    : _putWithResponseFixedResult);
            }

            if (_putWithResponseFixedResult != null)
                return Task.FromResult(_putWithResponseFixedResult);

            var defaultResp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"resource\":{\"status\":\"success\"}}")
            };
            return Task.FromResult(Tuple.Create(defaultResp, "{\"resource\":{\"status\":\"success\"}}"));
        }

        // ── DeleteAsync / DeleteWithResponseAsync ──────────────────────────────────
        public void SetDeleteAsyncResult() { }

        public void SetDeleteAsyncToThrow(Exception ex)
        {
            throw ex;
        }

        public override Task DeleteAsync(string endpoint)
        {
            DeleteAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            return Task.CompletedTask;
        }

        public void SetDeleteWithResponseResult(Tuple<HttpResponseMessage, string> result)
        {
            _deleteWithResponseFixedResult = result;
        }

        public override Task<Tuple<HttpResponseMessage, string>> DeleteWithResponseAsync(string endpoint)
        {
            DeleteWithResponseAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            if (_deleteWithResponseBehavior != null)
                return Task.FromResult(_deleteWithResponseBehavior(endpoint));

            if (_deleteWithResponseFixedResult != null)
                return Task.FromResult(_deleteWithResponseFixedResult);

            var resp = new HttpResponseMessage(HttpStatusCode.NoContent);
            return Task.FromResult(new Tuple<HttpResponseMessage, string>(resp, ""));
        }

        // ── Special method: UpdateSwapDeltaAsync (new 3-parameter signature) ───────
        public override async Task<OpusApiResponse<SwapDeltaUpdateResponse>> UpdateSwapDeltaAsync(string endpoint, string swapId, SwapDeltaUpdate deltaUpdate)
        {
            UpdateSwapDeltaAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;

            // Simulate internal PUT call using BuildFullUrl logic via PutWithResponseAsync
            var tuple = await PutWithResponseAsync(endpoint, deltaUpdate);

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

        // ── Helper for GetWithResponseAsync (if used) ──────────────────────────────
        public void SetGetWithResponseResult(HttpResponseMessage response)
        {
            string body = response.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            _getAsyncResult = JsonConvert.DeserializeObject(body);
        }
    }
}