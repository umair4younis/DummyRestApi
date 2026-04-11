using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Tests;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
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
            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual("updated", result.Data.Resource.Status);
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_Api400_ReturnsFailureResult()
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

            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_Api403_ReturnsFailureResult()
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

            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_WeightsSumNot100_AllowsUpdateWithWarning()
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

            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 20000000m }
            };
            _fakeApi.SetGetAsyncResult(mockSwap);

            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);
            Assert.IsTrue(result.IsSuccess);

            Assert.IsTrue(_fakeApi.UpdateSwapDeltaAsyncCalled, "Delta update should still be sent when weights are not exactly 100.");
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_NegativePieces_ReturnsFailureResult()
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
            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_WeightsOutOfRange_ReturnsFailureResult()
        {
            var swapId = "invalid-weight-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
        {
            new SwapDeltaMember { AssetId = "a1", CurrentPieces = 10, CurrentWeight = 150m }
        }
            };

            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_MissingAssetId_ReturnsFailureResult()
        {
            var swapId = "missing-assetid-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "", CurrentPieces = 10, CurrentWeight = 100m }
                }
            };

            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
        }
    }
}
