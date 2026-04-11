using Puma.MDE.OPUS.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Puma.MDE.OPUS.Tests
{
    public partial class FakeOpusApiClient : OpusApiClient
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

        public void SetPatchSwapToThrow(Exception ex)
        {
            _patchSwapThrowException = ex;
        }

        public void SetGetSwapQuotesResult(OpusApiResponse<QuoteGetResource> result)
        {
            _getSwapQuotesResult = result;
        }

        public void SetGetSwapQuotesToThrow(Exception ex)
        {
            _getSwapQuotesThrowException = ex;
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
        private Exception _patchSwapThrowException;
        private Exception _getSwapQuotesThrowException;
        private OpusApiResponse<QuoteGetResource> _getSwapQuotesResult;
        private Exception _deleteAsyncThrowException;

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
            _patchSwapThrowException = null;
            _getSwapQuotesThrowException = null;
            _getSwapQuotesResult = null;
            _deleteAsyncThrowException = null;
        }

        // -- GetAsync ---------------------------------------------------------------
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

        // -- PostAsync --------------------------------------------------------------
        public void SetPostAsyncResult() { }

        public override Task PostAsync(string endpoint, object data)
        {
            PostAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            return Task.CompletedTask;
        }

        // -- PostWithResponseAsync (non-generic) ------------------------------------
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

        // -- Generic PostWithResponseAsync<T> ---------------------------------------
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

        // -- Convenience for strongly-typed Post ------------------------------------
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

    }
}