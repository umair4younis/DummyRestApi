using Puma.MDE.OPUS.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Puma.MDE.OPUS.Tests
{
    public partial class FakeOpusApiClient
    {
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

        public override Task PatchAsync(string endpoint, object data, bool encodeUrl = true, int timeoutMs = 0)
        {
            PatchAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_patchAsyncThrowException != null) throw _patchAsyncThrowException;
            if (_patchAsyncBehavior != null) return Task.FromResult(_patchAsyncBehavior(endpoint, data));
            return Task.CompletedTask;
        }

        public override Task PatchAsync(string endpoint, object data, string parentAssetId)
        {
            PatchAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_patchAsyncThrowException != null) throw _patchAsyncThrowException;
            return Task.CompletedTask;
        }

        public override Task PatchSwapAsync(string endpoint, string swapId, object patchPayload)
        {
            PatchAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_patchSwapThrowException != null) throw _patchSwapThrowException;
            return Task.CompletedTask;
        }

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

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
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

        public void SetDeleteAsyncResult() { }

        public void SetDeleteAsyncToThrow(Exception ex)
        {
            _deleteAsyncThrowException = ex;
        }

        public override Task DeleteAsync(string endpoint)
        {
            DeleteAsyncCalled = true;
            if (_circuitBreakerException != null) throw _circuitBreakerException;
            if (_deleteAsyncThrowException != null) throw _deleteAsyncThrowException;
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
    }
}
