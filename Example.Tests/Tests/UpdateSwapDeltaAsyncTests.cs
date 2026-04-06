using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Example.OPUS;
using Example.OPUS.Exceptions;
using Example.OPUS.Models;
using Example.OPUS.Tests;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Example.Tests
{
    [TestClass]
    public class UpdateSwapDeltaAsyncTests
    {
        private OpusWeightUpdateProcessor _processor;
        private FakeOpusApiClient _fakeApi;
        private FakeOpusCircuitBreaker _fakeCircuitBreaker;

        [TestInitialize]
        public void Setup()
        {
            var fakeConfig = new FakeOpusConfiguration();
            var fakeTokenProvider = new FakeTokenProvider(fakeConfig);
            var fakeGraphQL = new FakeOpusGraphQLClient(null, fakeTokenProvider, fakeConfig);

            _fakeApi = new FakeOpusApiClient();
            _fakeCircuitBreaker = new FakeOpusCircuitBreaker();

            _processor = new OpusWeightUpdateProcessor(fakeGraphQL, _fakeApi);

            // Robust injection of fake circuit breaker (works whether field is public or private)
            var circuitField = typeof(OpusWeightUpdateProcessor)
                .GetField("_opusCircuitBreaker",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            if (circuitField != null)
            {
                circuitField.SetValue(_processor, _fakeCircuitBreaker);
            }
            else
            {
                Assert.Fail("Could not find _opusCircuitBreaker field in OpusWeightUpdateProcessor.");
            }
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_Success_ReturnsResponse()
        {
            var swapId = "f869582b-8726-47db-b9a9-729dbca4002c";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "4b61d275-b52a-4eca-aa33-a8c60d049f32", CurrentPieces = 12, CurrentWeight = 50m },
                    new SwapDeltaMember { AssetId = "1f5a284b-7645-4086-9671-92d5c299ba73", CurrentPieces = 8, CurrentWeight = 50m }
                }
            };

            // Mock successful validation
            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 20000000m }
            };
            _fakeApi.SetGetAsyncResult(mockSwap);

            // Mock successful PUT response
            var mockResponseBody = JsonConvert.SerializeObject(new
            {
                resource = new
                {
                    status = "updated",
                    message = "Delta applied successfully"
                }
            });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseBody)
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            // Act
            var result = await _processor.UpdateSwapDeltaAsync(swapId, delta);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("updated", result.Resource.Status);
        }

        [TestMethod]
        [ExpectedException(typeof(ApiValidationException))]
        public async Task UpdateSwapDeltaAsync_Api400_ThrowsValidationException()
        {
            var swapId = "bad-request-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            // Mock successful validation
            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 20000000m }
            };
            _fakeApi.SetGetAsyncResult(mockSwap);

            // Mock 400 response
            var errorBody = "{\"errors\":[\"Invalid weight sum\"]}";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody)
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _processor.UpdateSwapDeltaAsync(swapId, delta);
        }

        [TestMethod]
        [ExpectedException(typeof(ApiAuthorizationException))]
        public async Task UpdateSwapDeltaAsync_Api403_ThrowsAuthException()
        {
            var swapId = "forbidden-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            // Mock successful validation
            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 20000000m }
            };
            _fakeApi.SetGetAsyncResult(mockSwap);

            var mockResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"error\":\"Insufficient permissions\"}")
            };

            _fakeApi.SetPutWithResponseResult(mockResponse);

            await _processor.UpdateSwapDeltaAsync(swapId, delta);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateSwapDeltaAsync_WeightsSumNot100_Throws()
        {
            var swapId = "sum-wrong-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 10, CurrentWeight = 60m },
                    new SwapDeltaMember { AssetId = "a2", CurrentPieces = 5, CurrentWeight = 30m }
                }
            };

            // Mock valid swap for validation
            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 20000000m }
            };
            _fakeApi.SetGetAsyncResult(mockSwap);

            await _processor.UpdateSwapDeltaAsync(swapId, delta);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateSwapDeltaAsync_NegativePieces_ThrowsEarly()
        {
            var swapId = "negative-pieces-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
        {
            new SwapDeltaMember { AssetId = "a1", CurrentPieces = -5, CurrentWeight = 50m }
        }
            };

            // No need to mock swap - early validation should catch this before ValidateSwapAsync
            await _processor.UpdateSwapDeltaAsync(swapId, delta);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateSwapDeltaAsync_WeightsOutOfRange_ThrowsEarly()
        {
            var swapId = "invalid-weight-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
        {
            new SwapDeltaMember { AssetId = "a1", CurrentPieces = 10, CurrentWeight = 150m }
        }
            };

            await _processor.UpdateSwapDeltaAsync(swapId, delta);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateSwapDeltaAsync_MissingAssetId_ThrowsEarly()
        {
            var swapId = "missing-assetid-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "", CurrentPieces = 10, CurrentWeight = 100m }
                }
            };

            await _processor.UpdateSwapDeltaAsync(swapId, delta);
        }
    }
}