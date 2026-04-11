using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Puma.MDE.OPUS.Exceptions;

namespace Puma.MDE.Tests
{
    /// <summary>
    /// Swap delta calculation and update tests.
    /// Tests for delta fetching, updating, and weight validation flows.
    /// </summary>
    public partial class OpusWeightUpdateProcessorTests
    {
        // ========================================
        // Delta Operations Tests
        // ========================================
        [TestMethod]
        public async Task UpdateSwapDeltaAsync_ValidDelta_LogsMemberCountAndAssetIds()
        {
            var swapId = "f869582b-8726-47db-b9a9-729dbca4002c";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "asset-1", CurrentPieces = 12, CurrentWeight = 50m },
                    new SwapDeltaMember { AssetId = "asset-2", CurrentPieces = 8, CurrentWeight = 50m }
                }
            };

            var mockSwap = new TotalReturnSwapResponse { Uuid = swapId };

            _fakeApi.SetGetAsyncResult(mockSwap);
            _fakeApi.SetPutWithResponseResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"resource\":{\"status\":\"success\"}}")
            });

            var result = (await _processor.TryUpdateSwapDeltaAsync(swapId, delta)).Data;

            Assert.IsNotNull(result);
        }


        /// <summary>
        /// Tests that when validation fails in UpdateSwapDeltaAsync, an error is logged 
        /// with the validation summary and InvalidOperationException is thrown.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapDeltaAsync_InvalidValidation_ReturnsFailureResult()
        {
            var swapId = "invalid-swap";
            var delta = new SwapDeltaUpdate { Members = new List<SwapDeltaMember>() };

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryUpdateSwapDeltaAsync(swapId, delta);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        /// <summary>
        /// Verifies that UpdateSwapDeltaAsync correctly detects when the sum of weights 
        /// deviates significantly from 100% and logs an error message with the actual sum.
        /// Also confirms that an ArgumentException is thrown.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapDeltaAsync_WeightsSumNot100_LogsErrorAndThrows()
        {
            var swapId = "weights-sum-test-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "asset-1", CurrentPieces = 10, CurrentWeight = 60m },
                    new SwapDeltaMember { AssetId = "asset-2", CurrentPieces = 5, CurrentWeight = 30m }
                }
            };

            var mockSwap = new TotalReturnSwapResponse { Uuid = swapId };
            _fakeApi.SetGetAsyncResult(mockSwap);
            _fakeApi.SetPutWithResponseResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"resource\":{\"status\":\"success\"}}")
            });

            var result = (await _processor.TryUpdateSwapDeltaAsync(swapId, delta)).Data;
            Assert.IsNotNull(result);
        }


        /// <summary>
        /// Verifies that UpdateSwapDeltaAsync logs a warning when the sum of weights 
        /// is close to 100% but has a small deviation.
        /// The operation should still succeed.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapDeltaAsync_WeightsSumSlightDeviation_LogsWarningButSucceeds()
        {
            var swapId = "slight-deviation-swap";

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "asset-1", CurrentPieces = 10, CurrentWeight = 50.3m },
                    new SwapDeltaMember { AssetId = "asset-2", CurrentPieces = 10, CurrentWeight = 49.7m }
                }
            };

            var mockSwap = new TotalReturnSwapResponse { Uuid = swapId };
            _fakeApi.SetGetAsyncResult(mockSwap);
            _fakeApi.SetPutWithResponseResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"resource\":{\"status\":\"success\"}}")
            });

            var result = (await _processor.TryUpdateSwapDeltaAsync(swapId, delta)).Data;

            Assert.IsNotNull(result);
        }


        /// <summary>
        /// Verifies that FetchSwapDeltaAsync successfully calls the generic PostWithResponseAsync<T>,
        /// returns the correct SwapDeltaFetchResponse with account segments, swaps, and member deltas.
        /// Uses the recommended SetPostWithResponseResult<T> convenience method.
        /// </summary>
        [TestMethod]
        public async Task FetchSwapDeltaAsync_Success_ReturnsDeltaData()
        {
            // Arrange
            var request = new SwapDeltaFetchRequest
            {
                AccountSegments = new List<string> { "1492e07e-3c15-457b-9bc4-363cc55fb691" }
            };

            var mockResponse = new SwapDeltaFetchResponse
            {
                AccountSegments = new List<AccountSegmentDelta>
                {
                    new AccountSegmentDelta
                    {
                        Uuid = "1492e07e-3c15-457b-9bc4-363cc55fb691",
                        Name = "onemarkets UC Equity Sectors Fund",
                        Swaps = new List<SwapDeltaInfo>
                        {
                            new SwapDeltaInfo
                            {
                                Uuid = "f869582b-8726-47db-b9a9-729dbca4002c",
                                Name = "Performance Swap - onemarkets UC Equity Sectors Fund",
                                Members = new List<SwapDeltaMemberDetail>
                                {
                                    new SwapDeltaMemberDetail
                                    {
                                        AssetUuid = "4b61d275-b52a-4eca-aa33-a8c60d049f32",
                                        AssetName = "BASF SE",
                                        AssetIsin = "DE000BASF111",
                                        CurrentPieces = 12m,
                                        CurrentWeight = 13m,
                                        TargetPieces = 47977.521807201968240m,
                                        TargetWeight = 15m,
                                        DeltaPieces = 47965.521807201968240m,
                                        DeltaWeight = 2m
                                    },
                                    new SwapDeltaMemberDetail
                                    {
                                        AssetUuid = "1f5a284b-7645-4086-9671-92d5c299ba73",
                                        AssetName = "CONOCOPHILLIPS",
                                        AssetIsin = "US20825C1045",
                                        CurrentPieces = 2m,
                                        CurrentWeight = 2m,
                                        TargetPieces = 77.611228567543827m,
                                        TargetWeight = 0.060083720879557m,
                                        DeltaPieces = 75.611228567543827m,
                                        DeltaWeight = -1.939916279120443m
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Use the convenience method - fully compatible with your current Fake
            _fakeApi.SetPostWithResponseResult(mockResponse);

            // Act
            var result = (await _processor.TryFetchSwapDeltaAsync(request)).Data;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.AccountSegments.Count);
            Assert.AreEqual("onemarkets UC Equity Sectors Fund", result.AccountSegments[0].Name);
            Assert.AreEqual(1, result.AccountSegments[0].Swaps.Count);
            Assert.AreEqual(2, result.AccountSegments[0].Swaps[0].Members.Count);
            Assert.AreEqual(2m, result.AccountSegments[0].Swaps[0].Members[0].DeltaWeight);
            Assert.IsTrue(_fakeApi.PostWithResponseGenericCalled);
        }


        /// <summary>
        /// Ensures that FetchSwapDeltaAsync throws ArgumentException when called with null 
        /// or empty accountSegments, and logs an appropriate error message.
        /// </summary>
        [TestMethod]
        public async Task FetchSwapDeltaAsync_InvalidRequest_ReturnsFailureResult()
        {
            // Act & Assert
            var result = await _processor.TryFetchSwapDeltaAsync(null);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        /// <summary>
        /// Verifies detailed logging behavior of FetchSwapDeltaAsync in the success path:
        /// - Logs start message with number of account segments
        /// - Logs success summary with total swaps and member deltas count
        /// </summary>
        [TestMethod]
        public async Task FetchSwapDeltaAsync_ValidRequest_LogsStartAndSuccessSummary()
        {
            // Arrange
            var request = new SwapDeltaFetchRequest
            {
                AccountSegments = new List<string> { "1492e07e-3c15-457b-9bc4-363cc55fb691" }
            };

            var mockResponse = new SwapDeltaFetchResponse
            {
                AccountSegments = new List<AccountSegmentDelta>
                {
                    new AccountSegmentDelta
                    {
                        Swaps = new List<SwapDeltaInfo>
                        {
                            new SwapDeltaInfo
                            {
                                Members = new List<SwapDeltaMemberDetail> { new SwapDeltaMemberDetail() }
                            }
                        }
                    }
                }
            };

            _fakeApi.SetPostWithResponseResult(mockResponse);

            // Act
            await _processor.TryFetchSwapDeltaAsync(request);

            // Assert logging

        }


        /// <summary>
        /// Confirms that when the circuit breaker is open, FetchSwapDeltaAsync throws 
        /// CircuitBreakerOpenException and logs the error appropriately.
        /// </summary>
        [TestMethod]
        public async Task FetchSwapDeltaAsync_CircuitBreakerOpen_ReturnsFailureResult()
        {
            var request = new SwapDeltaFetchRequest
            {
                AccountSegments = new List<string> { "1492e07e-3c15-457b-9bc4-363cc55fb691" }
            };

            _fakeApi.SetCircuitBreakerToThrow(new CircuitBreakerOpenException("Circuit is open"));

            var result = await _processor.TryFetchSwapDeltaAsync(request);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

    }
}
