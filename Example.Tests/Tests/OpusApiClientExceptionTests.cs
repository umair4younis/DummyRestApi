using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Puma.MDE.OPUS.Exceptions;
using System.Collections.Generic;
using Puma.MDE.OPUS.Tests;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusApiClientExceptionTests
    {
        private FakeOpusApiClient _fakeApi;

        [TestInitialize]
        public void Setup()
        {
            _fakeApi = new FakeOpusApiClient();
            _fakeApi.Reset();   // Ensure clean state for every test
        }

        /// <summary>
        /// Verifies that a 500 Internal Server Error correctly throws ApiServerException 
        /// when calling the updated UpdateSwapDeltaAsync (3-parameter version).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiServerException))]
        public async Task UpdateSwapDeltaAsync_Server500_ThrowsApiServerException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/server-error-swap";
            var swapId = "server-error-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var mockResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"Internal server error\"}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that a 504 Gateway Timeout correctly maps to ApiServerException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiServerException))]
        public async Task UpdateSwapDeltaAsync_GatewayTimeout504_ThrowsApiServerException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/timeout-swap";
            var swapId = "timeout-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var mockResponse = new HttpResponseMessage((HttpStatusCode)504)
            {
                Content = new StringContent("Gateway Timeout")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that invalid JSON in a 200 OK response throws ApiResponseException 
        /// during deserialization in UpdateSwapDeltaAsync.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiResponseException))]
        public async Task UpdateSwapDeltaAsync_InvalidJsonResponse_ThrowsApiResponseException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/bad-json-swap";
            var swapId = "bad-json-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var invalidJson = "{ this is not valid json }";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson)
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that 403 Forbidden correctly throws ApiAuthorizationException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiAuthorizationException))]
        public async Task UpdateSwapDeltaAsync_403Forbidden_ThrowsApiAuthorizationException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/forbidden-swap";
            var swapId = "forbidden-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var mockResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"error\":\"Insufficient permissions\"}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that 401 Unauthorized correctly throws ApiAuthorizationException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiAuthorizationException))]
        public async Task UpdateSwapDeltaAsync_401Unauthorized_ThrowsApiAuthorizationException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/unauthorized-swap";
            var swapId = "unauthorized-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var mockResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"Invalid token\"}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that 429 Rate Limit correctly throws ApiRateLimitException 
        /// (updated behavior in UpdateSwapDeltaAsync).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiRateLimitException))]
        public async Task UpdateSwapDeltaAsync_429RateLimit_ThrowsApiRateLimitException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/rate-limit-swap";
            var swapId = "rate-limit-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
        {
            new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
        }
            };

            var mockResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent("{\"error\":\"Rate limit exceeded\"}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that 422 Unprocessable Entity throws ApiValidationException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiValidationException))]
        public async Task UpdateSwapDeltaAsync_422ValidationError_ThrowsApiValidationException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/validation-error-swap";
            var swapId = "validation-error-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var mockResponse = new HttpResponseMessage((HttpStatusCode)422)
            {
                Content = new StringContent("{\"errors\":[{\"message\":\"Invalid weight sum\"}]}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }

        /// <summary>
        /// Verifies that 404 Not Found correctly throws ApiNotFoundException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiNotFoundException))]
        public async Task UpdateSwapDeltaAsync_404NotFound_ThrowsApiNotFoundException()
        {
            const string endpoint = "/unicredit-swap-service/api/swaps/missing-swap";
            var swapId = "missing-swap";
            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            var mockResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":\"Swap not found\"}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _fakeApi.UpdateSwapDeltaAsync(endpoint, swapId, delta);
        }
    }
}