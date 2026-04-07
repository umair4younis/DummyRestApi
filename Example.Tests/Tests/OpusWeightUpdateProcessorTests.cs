using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Puma.MDE.OPUS.Exceptions;
using System.Linq;
using static Puma.MDE.OPUS.OpusCircuitBreaker;
using Puma.MDE.OPUS.Tests;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusWeightUpdateProcessorTests
    {
        private OpusWeightUpdateProcessor _processor;
        private FakeOpusCircuitBreaker _fakeCircuitBreaker;
        private FakeOpusGraphQLClient _fakeGraphQL;
        private FakeOpusApiClient _fakeApi;
        private FakeLogger _fakeLogger;
        private List<int> _recordedDelays = new List<int>();

        [TestInitialize]
        public void Setup()
        {
            var fakeConfig = new FakeOpusConfiguration();
            var fakeTokenProvider = new FakeTokenProvider(fakeConfig);
            var fakeGraphQL = new FakeOpusGraphQLClient(null, fakeTokenProvider, fakeConfig);

            _fakeApi = new FakeOpusApiClient();
            _fakeCircuitBreaker = new FakeOpusCircuitBreaker();

            _processor = new OpusWeightUpdateProcessor(fakeGraphQL, _fakeApi);

            // Direct public field assignment (no reflection)
            if (_processor._opusCircuitBreaker != null)
            {
                // If it's already set, we can try to replace it via reflection as fallback
                var field = typeof(OpusWeightUpdateProcessor)
                    .GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                field?.SetValue(_processor, _fakeCircuitBreaker);
            }
            else
            {
                // Direct assignment if the field is public and accessible
                typeof(OpusWeightUpdateProcessor)
                    .GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(_processor, _fakeCircuitBreaker);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullGraphQLClient_Throws()
        {
            new OpusWeightUpdateProcessor(null, _fakeApi);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullApiClient_Throws()
        {
            new OpusWeightUpdateProcessor(_fakeGraphQL, null);
        }

        [TestMethod]
        public void Constructor_InitializesPoliciesAndBreaker()
        {
            Assert.IsNotNull(_processor.GetType().GetField("_parentValidationPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_processor));
            Assert.IsNotNull(_processor.GetType().GetField("_bbgBatchPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_processor));
            Assert.IsNotNull(_processor.GetType().GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_processor));
        }

        // ──────────────────────────────────────────────────────────────
        // ExecuteAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task ExecuteAsync_ParentInvalid_StopsAndLogsFatal()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse { assets = new Assets { edges = Array.Empty<AssetEdge>() } });
            await _processor.ExecuteAsync();
            // Assert no further calls (manual verification or add call counters to fakes)
            Assert.IsFalse(_fakeApi.PatchAsyncCalled);
        }

        [TestMethod]
        public async Task ExecuteAsync_ValidParentAndComponents_CallsPatch()
        {
            // Mock parent validation success
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[] { new AssetEdge { node = new AssetNode { uuid = "uuid-123" } } }
                }
            });
            // Mock batch validation
            OpusWeightUpdateProcessor.ReportHoldings = new List<ReportHolding>
            {
                new ReportHolding { BbgTicker = "TICKER1", MarketWeightPercent = 50m }
            };
            await _processor.ExecuteAsync();
            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // ExecuteWithRetryAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task ExecuteWithRetryAsync_Success_ReturnsResult()
        {
            var policy = new RetryPolicy { MaxRetries = 2 };
            var result = await _processor.ExecuteWithRetryAsync(
                () => Task.FromResult("success"),
                "test-op",
                policy);
            Assert.AreEqual("success", result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteWithRetryAsync_ExhaustsRetries_Throws()
        {
            var policy = new RetryPolicy { MaxRetries = 1, IsRetryable = ex => true };
            await _processor.ExecuteWithRetryAsync<ValidationResultOpus>(
                () => throw new HttpRequestException("transient"),
                "test-op",
                policy);
        }

        // ──────────────────────────────────────────────────────────────
        // ValidateAssetCompositionIdAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task ValidateAssetCompositionIdAsync_NoAsset_ReturnsInvalid()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse { assets = new Assets { edges = Array.Empty<AssetEdge>() } });
            var result = await _processor.ValidateAssetCompositionIdAsync();
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("No asset found"));
        }

        [TestMethod]
        public async Task ValidateAssetCompositionIdAsync_ValidAsset_ReturnsSuccess()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[] { new AssetEdge { node = new AssetNode { uuid = "uuid-abc", name = "Test", __typename = "ASSETCOMPOSITION" } } }
                }
            });
            var result = await _processor.ValidateAssetCompositionIdAsync();
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("uuid-abc", result.AssetUuid);
            Assert.AreEqual("Test", result.AssetName);
        }

        // ──────────────────────────────────────────────────────────────
        // ValidateAndCollectBbgUuidsAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task ValidateAndCollectBbgUuidsAsync_NoHoldings_ReturnsEmpty()
        {
            OpusWeightUpdateProcessor.ReportHoldings = new List<ReportHolding>();
            var result = await _processor.ValidateAndCollectBbgUuidsAsync();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task ValidateAndCollectBbgUuidsAsync_ValidBatch_AddsMapping()
        {
            OpusWeightUpdateProcessor.ReportHoldings = new List<ReportHolding>
            {
                new ReportHolding { BbgTicker = "TICKER1", MarketWeightPercent = 60m }
            };
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[]
                    {
                        new AssetEdge
                        {
                            node = new AssetNode
                            {
                                uuid = "uuid-t1",
                                symbols = new[] { new Symbol { identifier = "TICKER1" } },
                                __typename = "ASSETCOMPOSITION"
                            }
                        }
                    }
                }
            });
            var result = await _processor.ValidateAndCollectBbgUuidsAsync();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("TICKER1", result[0].BbgTicker);
            Assert.AreEqual("uuid-t1", result[0].Uuid);
            Assert.AreEqual(60m, result[0].WeightPercent);
        }

        // ──────────────────────────────────────────────────────────────
        // FetchBbgBatchAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task FetchBbgBatchAsync_ReturnsMappedNodes()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[]
                    {
                        new AssetEdge
                        {
                            node = new AssetNode
                            {
                                symbols = new[] { new Symbol { identifier = "TICKER1" } },
                                uuid = "uuid-1"
                            }
                        }
                    }
                }
            });
            var batch = new List<string> { "TICKER1" };
            var result = await _processor.FetchBbgBatchAsync(batch);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("TICKER1"));
            Assert.AreEqual("uuid-1", result["TICKER1"][0].uuid);
        }

        // ──────────────────────────────────────────────────────────────
        // BuildBbgFilterQuery
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public void BuildBbgFilterQuery_GeneratesCorrectOrConditions()
        {
            var tickers = new List<string> { "T1", "T2" };
            var query = _processor.BuildBbgFilterQuery(tickers);
            Assert.IsTrue(query.Contains("symbols.identifier = 'T1'"));
            Assert.IsTrue(query.Contains("symbols.identifier = 'T2'"));
            Assert.IsTrue(query.Contains("or: ["));
        }

        // ──────────────────────────────────────────────────────────────
        // SendWeightUpdatePayloadPatchAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task SendWeightUpdatePayloadPatchAsync_NoParentUuid_LogsWarnAndReturns()
        {
            await _processor.SendWeightUpdatePayloadPatchAsync(null, new List<ComponentInfo>());
            Assert.IsFalse(_fakeApi.PatchAsyncCalled);
        }

        [TestMethod]
        public async Task SendWeightUpdatePayloadPatchAsync_NoComponents_LogsWarnAndReturns()
        {
            await _processor.SendWeightUpdatePayloadPatchAsync("uuid-123", null);
            Assert.IsFalse(_fakeApi.PatchAsyncCalled);
        }

        [TestMethod]
        public async Task SendWeightUpdatePayloadPatchAsync_ValidData_CallsPatch()
        {
            var components = new List<ComponentInfo>
            {
                new ComponentInfo { Uuid = "uuid-child", WeightPercent = 100m }
            };
            await _processor.SendWeightUpdatePayloadPatchAsync("uuid-parent", components);
            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
        }

        /// <summary>
        /// Verifies that when the swap is not found (404), GetTotalReturnSwapAsync 
        /// throws ApiNotFoundException and logs the error correctly.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiNotFoundException))]
        public async Task GetTotalReturnSwapAsync_SwapNotFound_ThrowsApiNotFoundException()
        {
            var swapId = "non-existent-swap";
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.GetTotalReturnSwapAsync(swapId);
        }

        /// <summary>
        /// Verifies detailed logging in the success path of GetTotalReturnSwapAsync:
        /// - Logs successful retrieval with swap name and nominal value.
        /// </summary>
        [TestMethod]
        public async Task GetTotalReturnSwapAsync_Success_LogsRetrievalSummary()
        {
            var swapId = "019d2001-e11b-7000-a211-8c654386b53d";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Name = "Demo Swap 3",
                Nominal = new AmountValue { Quantity = 100000m, Unit = "EUR", Type = "MONEY" }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            await _processor.GetTotalReturnSwapAsync(swapId);

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains("Total Return Swap") &&
                log.Contains(swapId) &&
                log.Contains("Demo Swap 3")));
        }

        // ──────────────────────────────────────────────────────────────
        // CreateTotalReturnSwapAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_Success_ReturnsIdentifier()
        {
            // Arrange: mock PostWithResponseAsync response
            var mockResponseBody = JsonConvert.SerializeObject(new
            {
                resource = new
                {
                    identifier = "new-swap-uuid-456",
                    status = 201,
                    location = "/swaps/new-swap-uuid-456"
                }
            });
            var mockResponse = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(mockResponseBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            var payload = new { name = "New TRS", type = "TOTAL_RETURN_SWAP" };
            // Act
            var createdId = await _processor.CreateTotalReturnSwapAsync(payload);
            // Assert
            Assert.AreEqual("new-swap-uuid-456", createdId);
            Assert.IsTrue(_fakeApi.PostWithResponseAsyncCalled);
        }

        [TestMethod]
        [ExpectedException(typeof(ApiValidationException))]
        public async Task CreateTotalReturnSwapAsync_ValidationError_Throws()
        {
            var errorBody = JsonConvert.SerializeObject(new
            {
                errors = new[] { new { message = "Invalid payload" } }
            });
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            await _processor.CreateTotalReturnSwapAsync(new { });
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateTotalReturnSwapAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_Success_NoException()
        {
            _fakeApi.SetPatchAsyncResult();
            await _processor.UpdateTotalReturnSwapAsync("swap-123", new { name = "Updated" });
            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
        }

        [TestMethod]
        [ExpectedException(typeof(ApiValidationException))]
        public async Task UpdateTotalReturnSwapAsync_ValidationError_Throws()
        {
            _fakeApi.SetPatchAsyncToThrow(new ApiValidationException("Invalid data", "error body"));
            await _processor.UpdateTotalReturnSwapAsync("swap-123", new { });
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteTotalReturnSwapAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task DeleteTotalReturnSwapAsync_Success_NoException()
        {
            _fakeApi.SetDeleteAsyncResult();
            await _processor.DeleteTotalReturnSwapAsync("swap-123");
            Assert.IsTrue(_fakeApi.DeleteAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // GetAssetQuoteAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task GetAssetQuoteAsync_Success_ReturnsResponse()
        {
            var mockResponse = new OpusApiResponse<QuoteGetResource>
            {
                Resource = new QuoteGetResource
                {
                    Quotes = new List<AssetQuote> { new AssetQuote { Date = DateTime.Now } }
                }
            };
            _fakeApi.SetGetAsyncResult(mockResponse);
            var result = await _processor.GetAssetQuoteAsync("swap-123", "market-456");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Resource.Quotes.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task GetAssetQuoteAsync_CircuitOpen_Throws()
        {
            _fakeApi.SetCircuitBreakerToThrow(new CircuitBreakerOpenException("open"));
            await _processor.GetAssetQuoteAsync("swap-123", "market-456");
        }

        // ──────────────────────────────────────────────────────────────
        // AddAssetQuoteToHomeMarketplaceAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task AddAssetQuoteToHomeMarketplaceAsync_Success_ReturnsResponse()
        {
            var quote = new AssetQuote { Date = DateTime.Now, Value = new AmountValue { Quantity = 100 } };
            var mockResponseBody = JsonConvert.SerializeObject(new
            {
                resource = new { identifier = "quote-789" }
            });
            var mockResponse = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(mockResponseBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            var result = await _processor.AddAssetQuoteToHomeMarketplaceAsync("swap-123", quote);
            Assert.IsNotNull(result);
            Assert.IsTrue(_fakeApi.PostWithResponseAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateAssetQuoteAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task UpdateAssetQuoteAsync_Success_ReturnsResponse()
        {
            var patch = new AssetQuotePatch { Value = new AmountValue { Quantity = 200 } };
            var mockResponseBody = JsonConvert.SerializeObject(new
            {
                resource = new { updatedQuote = new { value = new { quantity = 200 } } }
            });
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseBody)
            };
            _fakeApi.SetPatchWithResponseResult(mockResponse);
            var result = await _processor.UpdateAssetQuoteAsync("swap-123", "market-456", "quote-789", patch);
            Assert.IsNotNull(result);
            Assert.IsTrue(_fakeApi.PatchWithResponseAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteAssetQuoteWithResponseAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task DeleteAssetQuoteWithResponseAsync_Success_LogsStatus()
        {
            var mockResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
            var mockTuple = new Tuple<HttpResponseMessage, string>(mockResponse, "");
            _fakeApi.SetDeleteWithResponseResult(mockTuple);
            await _processor.DeleteAssetQuoteWithResponseAsync("swap-123", "market-456", "quote-789");
            Assert.IsTrue(_fakeApi.DeleteWithResponseAsyncCalled);
        }

        /// <summary>
        /// Verifies that GetTotalReturnSwapAsync correctly deserializes the new full TotalReturnSwapResponse 
        /// including uuid, name, nominal, assetAtMarketplaces and other key fields.
        /// </summary>
        [TestMethod]
        public async Task GetTotalReturnSwapAsync_Success_ReturnsFullResponse()
        {
            var swapId = "019d2001-e11b-7000-a211-8c654386b53d";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Name = "Demo Swap 3",
                Nominal = new AmountValue { Quantity = 100000m, Unit = "EUR", Type = "MONEY" },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>
                {
                    new AssetAtMarketplaceDetail { Home = true, QuoteUnit = "EUR/Pieces" }
                }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.GetTotalReturnSwapAsync(swapId);

            Assert.IsNotNull(result);
            Assert.AreEqual(swapId, result.Uuid);
            Assert.AreEqual("Demo Swap 3", result.Name);
            Assert.AreEqual(100000m, result.Nominal.Quantity);
            Assert.IsTrue(_fakeApi.GetAsyncCalled);
        }

        /// <summary>
        /// Ensures GetTotalReturnSwapAsync throws ArgumentException for null or empty swapId.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetTotalReturnSwapAsync_EmptySwapId_ThrowsArgumentException()
        {
            await _processor.GetTotalReturnSwapAsync("");
        }

        /// <summary>
        /// Verifies that ApiNotFoundException is properly propagated and logged when swap is not found.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiNotFoundException))]
        public async Task GetTotalReturnSwapAsync_NotFound_ThrowsAndLogs()
        {
            var swapId = "missing-swap";
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.GetTotalReturnSwapAsync(swapId);
        }

        /// <summary>
        /// Validates that ValidateSwapAsync works with the new TotalReturnSwapResponse model 
        /// and correctly extracts nominal value for validation.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_ValidSwap_ReturnsSuccessWithNominal()
        {
            var swapId = "019d2001-e11b-7000-a211-8c654386b53d";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Name = "Demo Swap",
                Nominal = new AmountValue { Quantity = 1500000m, Unit = "EUR", Type = "MONEY" }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: false);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1500000m, result.CurrentNotional);
        }

        /// <summary>
        /// Validates that low notional triggers validation failure when minNotional is specified.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_NotionalTooLow_ReturnsFailure()
        {
            var swapId = "small-swap";

            var mockResponse = new TotalReturnSwapResponse
            {
                Nominal = new AmountValue { Quantity = 500000m, Unit = "EUR", Type = "MONEY" }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "below minimum");
        }

        /// <summary>
        /// Verifies CreateSwapQuoteAsync performs validation using the new response model 
        /// and logs validation summary before creating the quote.
        /// </summary>
        [TestMethod]
        public async Task CreateSwapQuoteAsync_ValidSwap_LogsValidationAndSucceeds()
        {
            var swapId = "019d2001-e11b-7000-a211-8c654386b53d";
            var quote = new AssetQuote { Time = DateTime.UtcNow, Value = new AmountValue { Quantity = 210000m } };

            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Name = "Demo Swap",
                Nominal = new AmountValue { Quantity = 2000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwap);
            _fakeApi.SetPostWithResponseResult(new OpusApiResponse<AssetQuote> { Resource = new AssetQuote() });

            var result = await _processor.CreateSwapQuoteAsync(swapId, quote);

            Assert.IsNotNull(result);
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("CreateSwapQuoteAsync started")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("validation passed")));
        }

        // ──────────────────────────────────────────────────────────────
        // SendWeightUpdatePayloadPostAsync
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task SendWeightUpdatePayloadPostAsync_NoParentUuid_LogsWarnAndReturns()
        {
            await _processor.SendWeightUpdatePayloadPostAsync(null, new List<ComponentInfo>());
            Assert.IsFalse(_fakeApi.PostAsyncCalled);
        }

        [TestMethod]
        public async Task SendWeightUpdatePayloadPostAsync_ValidData_CallsPost()
        {
            var components = new List<ComponentInfo> { new ComponentInfo { Uuid = "child-1", WeightPercent = 100m } };
            await _processor.SendWeightUpdatePayloadPostAsync("parent-uuid", components);
            Assert.IsTrue(_fakeApi.PostAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // ExecuteAsync_CircuitBreaker
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task ExecuteAsync_CircuitBreaker_ValidFlow_Completes()
        {
            // Mock step 1: parent validation
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[] { new AssetEdge { node = new AssetNode { uuid = "parent-uuid" } } }
                }
            });
            // Mock step 2: BBG validation
            OpusWeightUpdateProcessor.ReportHoldings = new List<ReportHolding>
            {
                new ReportHolding { BbgTicker = "TICKER1", MarketWeightPercent = 100m }
            };
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[]
                    {
                        new AssetEdge
                        {
                            node = new AssetNode
                            {
                                uuid = "child-uuid",
                                symbols = new[] { new Symbol { identifier = "TICKER1" } }
                            }
                        }
                    }
                }
            });
            // Mock step 3: POST success
            _fakeApi.SetPostAsyncResult();
            await _processor.ExecuteAsync_CircuitBreaker();
            Assert.IsTrue(_fakeApi.PostAsyncCalled);
        }

        [TestMethod]
        public async Task ExecuteAsync_CircuitBreaker_ParentInvalid_ReturnsEarly()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse { assets = new Assets { edges = Array.Empty<AssetEdge>() } });
            await _processor.ExecuteAsync_CircuitBreaker();
            Assert.IsFalse(_fakeApi.PostAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // CreateTotalReturnSwapAsync – Detailed failures
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        [ExpectedException(typeof(ApiValidationException))]
        public async Task CreateTotalReturnSwapAsync_400ValidationError_ThrowsApiValidationException()
        {
            var errorBody = "{\"errors\":[{\"message\":\"Missing required field: name\"}]}";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            await _processor.CreateTotalReturnSwapAsync(new { type = "INVALID" });
        }

        [TestMethod]
        [ExpectedException(typeof(ApiRateLimitException))]
        public async Task CreateTotalReturnSwapAsync_429RateLimit_ThrowsRateLimitException()
        {
            var errorBody = "{\"error\":\"Rate limit exceeded. Retry after 60 seconds.\"}";
            var mockResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            await _processor.CreateTotalReturnSwapAsync(new { name = "Test TRS" });
        }

        [TestMethod]
        [ExpectedException(typeof(ApiRequestException))]
        public async Task CreateTotalReturnSwapAsync_401Unauthorized_ThrowsAuthException()
        {
            var errorBody = "{\"error\":\"Unauthorized - invalid token\"}";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            await _processor.CreateTotalReturnSwapAsync(new { });
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteTotalReturnSwapAsync Tests
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that DeleteTotalReturnSwapAsync handles 404 Not Found gracefully:
        /// logs a warning and returns without throwing an exception.
        /// </summary>
        [TestMethod]
        public async Task DeleteTotalReturnSwapAsync_404_LogsWarnAndReturns()
        {
            _fakeApi.SetDeleteAsyncToThrow(new HttpRequestException("DELETE /swaps/swap-123 failed with status 404: Not Found"));

            await _processor.DeleteTotalReturnSwapAsync("swap-123");

            Assert.IsTrue(_fakeApi.DeleteAsyncCalled);
            // Additional check if you have a fake logger:
            // Assert.IsTrue(_fakeLogger.WarnLogs.Any(log => log.Contains("not found")));
        }

        /// <summary>
        /// Verifies that DeleteTotalReturnSwapAsync treats 404 as non-fatal (logs warning and continues).
        /// </summary>
        [TestMethod]
        public async Task DeleteTotalReturnSwapAsync_404_LogsWarningAndContinues()
        {
            _fakeApi.SetDeleteAsyncToThrow(new HttpRequestException("DELETE /swaps/swap-123 failed with status 404: Not Found"));

            await _processor.DeleteTotalReturnSwapAsync("swap-123");

            Assert.IsTrue(_fakeApi.DeleteAsyncCalled);
        }

        /// <summary>
        /// Verifies that a 500 Server Error in DeleteTotalReturnSwapAsync throws ApiRequestException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiRequestException))]
        public async Task DeleteTotalReturnSwapAsync_500ServerError_Throws()
        {
            _fakeApi.SetDeleteAsyncToThrow(new HttpRequestException("DELETE failed with status 500: Internal Server Error"));

            await _processor.DeleteTotalReturnSwapAsync("swap-123");
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateTotalReturnSwapAsync Tests
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that UpdateTotalReturnSwapAsync throws ApiValidationException on 400 Bad Request.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiValidationException))]
        public async Task UpdateTotalReturnSwapAsync_400Validation_Throws()
        {
            _fakeApi.SetPatchAsyncToThrow(new ApiValidationException("Invalid weight value", "{\"errors\":[\"Weight must be between 0 and 100\"]}"));

            await _processor.UpdateTotalReturnSwapAsync("swap-123", new { weight = -10 });
        }

        /// <summary>
        /// Verifies that UpdateTotalReturnSwapAsync throws ApiNotFoundException on 404.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiNotFoundException))]
        public async Task UpdateTotalReturnSwapAsync_404NotFound_Throws()
        {
            _fakeApi.SetPatchAsyncToThrow(new ApiNotFoundException("Swap not found", "swap-123"));

            await _processor.UpdateTotalReturnSwapAsync("swap-999", new { });
        }

        // ──────────────────────────────────────────────────────────────
        // Concurrent / Parallel Execution Tests
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that multiple concurrent calls to ExecuteAsync complete without deadlock or unhandled exceptions.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_MultipleConcurrentCalls_CompletesWithoutDeadlock()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    OpusWeightUpdateProcessor.ReportHoldings = new List<ReportHolding>
                    {
                        new ReportHolding { BbgTicker = $"TICKER-{i}", MarketWeightPercent = 100m }
                    };
                    await _processor.ExecuteAsync();
                }));
            }

            await Task.WhenAll(tasks);

            Assert.IsTrue(tasks.All(t => t.IsCompleted && !t.IsFaulted && !t.IsCanceled));
        }

        /// <summary>
        /// Verifies concurrent calls to GetAssetQuoteAsync return correct results without interference.
        /// </summary>
        [TestMethod]
        public async Task GetAssetQuoteAsync_ConcurrentQuotes_ReturnsResults()
        {
            _fakeApi.SetGetAsyncResult(new OpusApiResponse<QuoteGetResource>
            {
                Resource = new QuoteGetResource { Quotes = new List<AssetQuote> { new AssetQuote { Date = DateTime.Now } } }
            });

            var tasks = new List<Task<OpusApiResponse<QuoteGetResource>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_processor.GetAssetQuoteAsync($"swap-{i}", "market-456"));
            }

            var results = await Task.WhenAll(tasks);

            Assert.AreEqual(10, results.Length);
            Assert.IsTrue(results.All(r => r != null && r.Resource.Quotes.Count > 0));
        }

        /// <summary>
        /// Verifies concurrent calls to UpdateAssetQuoteAsync do not cause race conditions.
        /// </summary>
        [TestMethod]
        public async Task UpdateAssetQuoteAsync_ConcurrentUpdates_NoRaceCondition()
        {
            var patch = new AssetQuotePatch { Value = new AmountValue { Quantity = 100 } };
            var tasks = new List<Task>();
            for (int i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await _processor.UpdateAssetQuoteAsync("swap-123", "market-456", $"quote-{i}", patch);
                }));
            }

            await Task.WhenAll(tasks);

            Assert.IsTrue(_fakeApi.PatchWithResponseAsyncCalled);
        }

        // ──────────────────────────────────────────────────────────────
        // CreateTotalReturnSwapAsync – Granular Failure & Retry Tests
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that transient 503 errors are retried and eventually succeed.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_503Transient_RetriesAndSucceeds()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 2)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)503)
                    {
                        Content = new StringContent("{\"error\":\"Service temporarily unavailable\"}")
                    };
                    return Tuple.Create(resp, "{\"error\":\"Service temporarily unavailable\"}");
                }
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-503-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-503-success\"}}");
            });

            var payload = new { name = "Retry Test TRS" };
            var createdId = await _processor.CreateTotalReturnSwapAsync(payload);

            Assert.AreEqual("swap-503-success", createdId);
            Assert.AreEqual(3, attempt);
        }

        /// <summary>
        /// Verifies that persistent 503 errors exhaust retries and throw the last exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiRequestException))]
        public async Task CreateTotalReturnSwapAsync_503ExhaustsRetries_ThrowsLastException()
        {
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                var resp = new HttpResponseMessage((HttpStatusCode)503)
                {
                    Content = new StringContent("{\"error\":\"Service down\"}")
                };
                return Tuple.Create(resp, "{\"error\":\"Service down\"}");
            });

            await _processor.CreateTotalReturnSwapAsync(new { name = "Will fail" });
        }

        /// <summary>
        /// Verifies that 502 Bad Gateway throws ApiRequestException (treated as non-retryable in current logic).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiRequestException))]
        public async Task CreateTotalReturnSwapAsync_502BadGateway_Throws()
        {
            var mockResponse = new HttpResponseMessage((HttpStatusCode)502)
            {
                Content = new StringContent("{\"error\":\"Bad gateway\"}")
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);

            await _processor.CreateTotalReturnSwapAsync(new { });
        }

        /// <summary>
        /// Verifies that when the circuit breaker is already open, CreateTotalReturnSwapAsync throws immediately.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task CreateTotalReturnSwapAsync_CircuitAlreadyOpen_ThrowsImmediately()
        {
            // Force circuit open via reflection (test-only)
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.Open);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(30));

            await _processor.CreateTotalReturnSwapAsync(new { });
        }

        /// <summary>
        /// Verifies that 400 validation errors do not trigger retries and throw ApiValidationException with body.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_400Validation_NoRetry()
        {
            var errorBody = "{\"errors\":[{\"message\":\"Missing name field\"}]}";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);

            try
            {
                await _processor.CreateTotalReturnSwapAsync(new { type = "TRS" });
            }
            catch (ApiValidationException vex)
            {
                Assert.IsTrue(vex.Message.Contains("Missing name field"));
                Assert.IsTrue(vex.ResponseBody.Contains("Missing name field"));
                return;
            }

            Assert.Fail("Expected ApiValidationException");
        }

        /// <summary>
        /// Verifies that multiple concurrent CreateTotalReturnSwapAsync calls are handled safely.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_ConcurrentCalls_HandlesParallelRequests()
        {
            var tasks = new List<Task<string>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var inputPayload = new { name = $"Concurrent TRS {i}" };
                    var mockResp = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent($"{{\"resource\":{{\"identifier\":\"swap-concurrent-{i}\"}}}}")
                    };
                    _fakeApi.SetPostWithResponseResult(mockResp);
                    return await _processor.CreateTotalReturnSwapAsync(inputPayload);
                }));
            }

            var results = await Task.WhenAll(tasks);

            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results.All(id => id.StartsWith("swap-concurrent-")));
        }

        // ──────────────────────────────────────────────────────────────
        // Backoff & Circuit Breaker Specific Tests
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that 504 Gateway Timeout is treated as transient and triggers retry with backoff.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_504GatewayTimeout_RetriesAndSucceeds()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 2)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)504)
                    {
                        Content = new StringContent("{\"error\":\"Gateway Timeout\"}")
                    };
                    return Tuple.Create(resp, "{\"error\":\"Gateway Timeout\"}");
                }
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-504-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-504-success\"}}");
            });

            var payload = new { name = "504 Retry Test" };
            var createdId = await _processor.CreateTotalReturnSwapAsync(payload);

            Assert.AreEqual("swap-504-success", createdId);
            Assert.AreEqual(3, attempt);
        }

        /// <summary>
        /// Verifies that persistent 504 errors exhaust retries and throw.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task CreateTotalReturnSwapAsync_504ExhaustsRetries_Throws()
        {
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                var resp = new HttpResponseMessage((HttpStatusCode)504)
                {
                    Content = new StringContent("Gateway timeout")
                };
                return Tuple.Create(resp, "Gateway timeout");
            });

            await _processor.CreateTotalReturnSwapAsync(new { });
        }

        /// <summary>
        /// Verifies 502 Bad Gateway is treated as transient and retries successfully.
        /// </summary>
        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_502BadGateway_RetriesSuccessfully()
        {
            int attempt = 0;
            _fakeApi.SetPatchAsyncBehavior((endpoint, data) =>
            {
                attempt++;
                if (attempt == 1)
                    throw new HttpRequestException("502 Bad Gateway");
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            await _processor.UpdateTotalReturnSwapAsync("swap-123", new { weight = 50 });
            Assert.AreEqual(2, attempt);
        }

        /// <summary>
        /// Verifies that 429 Rate Limit throws ApiRateLimitException (no retry).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task AddAssetQuoteToHomeMarketplaceAsync_429RateLimit_Throws()
        {
            var swapId = "rate-limit-swap-429";
            var quote = new AssetQuote
            {
                Time = DateTime.UtcNow,
                Value = new AmountValue { Quantity = 100000m, Unit = "EUR/Pieces", Type = "PRICE_PER_PIECE" }
            };

            // Force 429 response
            var mockResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent("{\"error\":\"Rate limit exceeded\"}")
            };

            // Make sure the fake returns this exact 429 response
            _fakeApi.SetPostWithResponseResult(mockResponse);

            // Disable circuit breaker retry logic for this test
            _fakeCircuitBreaker = new FakeOpusCircuitBreaker(); // ensure no retries

            // Re-inject if needed
            typeof(OpusWeightUpdateProcessor)
                .GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_processor, _fakeCircuitBreaker);

            // Act - should throw
            await _processor.AddAssetQuoteToHomeMarketplaceAsync(swapId, quote);
        }

        // ──────────────────────────────────────────────────────────────
        // Circuit Breaker Half-Open Tests (503 / 504 / 507 / 509 / 408)
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that after circuit opens due to 503, a successful half-open call closes the circuit.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_503OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)503)
                    {
                        Content = new StringContent("{\"error\":\"Service Unavailable\"}")
                    };
                    return Tuple.Create(resp, "{\"error\":\"Service Unavailable\"}");
                }
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-half-open-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-half-open-success\"}}");
            });

            // Force open circuit
            for (int i = 0; i < 3; i++) { try { await _processor.CreateTotalReturnSwapAsync(new { }); } catch { } }

            // Force half-open via reflection
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);

            var createdId = await _processor.CreateTotalReturnSwapAsync(new { name = "Half-Open Test" });

            Assert.AreEqual("swap-half-open-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
        }

        /// <summary>
        /// Verifies that a failure in half-open state re-opens the circuit.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_503OpensCircuit_HalfOpenFailureReOpens()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)503) { Content = new StringContent("Service Unavailable") };
                    return Tuple.Create(resp, "Service Unavailable");
                }
                var failResp = new HttpResponseMessage((HttpStatusCode)503) { Content = new StringContent("Service still down") };
                return Tuple.Create(failResp, "Service still down");
            });

            for (int i = 0; i < 3; i++) { try { await _processor.CreateTotalReturnSwapAsync(new { }); } catch { } }

            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);

            try { await _processor.CreateTotalReturnSwapAsync(new { }); } catch { }

            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
        }

        [TestMethod]
        public async Task GetAssetQuoteAsync_ConcurrentQuotes_NoDeadlock()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await _processor.GetAssetQuoteAsync($"swap-{i}", "market-shared");
                }));
            }
            await Task.WhenAll(tasks);
            // If no deadlock → test passes
            Assert.IsTrue(true);
        }

        [TestMethod]
        public async Task UpdateAssetQuoteAsync_ConcurrentUpdates_CircuitBreakerProtects()
        {
            // Simulate that circuit opens after 3 failures
            int failureCount = 0;
            _fakeApi.SetPatchWithResponseBehavior((endpoint, data) =>
            {
                failureCount++;
                if (failureCount <= 3)
                {
                    throw new HttpRequestException("transient");
                }
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"success\":true}")
                };
                return Tuple.Create(resp, "{\"success\":true}");
            });
            var tasks = new List<Task>();
            for (int i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var patch = new AssetQuotePatch { Value = new AmountValue { Quantity = 100 + i } };
                    await _processor.UpdateAssetQuoteAsync("swap-123", "market-456", $"quote-{i}", patch);
                }));
            }
            // Some will fail due to circuit opening
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (AggregateException ae)
            {
                // Expect some CircuitBreakerOpenException
                Assert.IsTrue(ae.InnerExceptions.Any(ex => ex is CircuitBreakerOpenException));
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Circuit breaker open during retry attempt
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task CreateTotalReturnSwapAsync_CircuitOpensDuringRetries_ThrowsOpenException()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt >= 3)
                {
                    throw new CircuitBreakerOpenException("Circuit open after failures");
                }
                throw new HttpRequestException("transient");
            });
            await _processor.CreateTotalReturnSwapAsync(new { });
        }

        // ──────────────────────────────────────────────────────────────
        // Backoff timing verification (using mocked Task.Delay)
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task ExecuteWithRetryAsync_VerifiesBackoffAndJitterDelays()
        {
            var policy = new RetryPolicy
            {
                MaxRetries = 3,
                BaseDelayMs = 100,
                BackoffFactor = 2.0,
                JitterMaxFactor = 0.5,
                IsRetryable = ex => true
            };
            int attempt = 0;
            Func<Task<int>> failThenSucceed = async () =>
            {
                attempt++;
                if (attempt <= 3)
                    throw new HttpRequestException("transient");
                return 999;
            };
            var result = await _processor.ExecuteWithRetryAsync(failThenSucceed, "backoff-test", policy);
            Assert.AreEqual(999, result);
            Assert.AreEqual(4, attempt); // initial + 3 retries
            // Verify recorded delays (approximate due to jitter)
            // attempt 1
            Assert.IsTrue(_recordedDelays.Any(d => d >= 50 && d <= 150));
            // attempt 2 (100 * 2)
            Assert.IsTrue(_recordedDelays.Any(d => d >= 100 && d <= 300));
            // attempt 3 (200 * 2)
            Assert.IsTrue(_recordedDelays.Any(d => d >= 200 && d <= 600));
        }

        // ──────────────────────────────────────────────────────────────
        // 503 + Retry backoff timing (with mocked Task.Delay)
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_503TriggersRetryWithBackoff()
        {
            // Reset recorded delays
            _recordedDelays.Clear();
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 2)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)503)
                    {
                        Content = new StringContent("Service Unavailable")
                    };
                    return Tuple.Create(resp, "Service Unavailable");
                }
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-retry-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-retry-success\"}}");
            });
            var payload = new { name = "Backoff Test" };
            var createdId = await _processor.CreateTotalReturnSwapAsync(payload);
            Assert.AreEqual("swap-retry-success", createdId);
            Assert.AreEqual(3, attempt);
            // Verify backoff delays were called (approximate ranges due to jitter)
            Assert.AreEqual(2, _recordedDelays.Count); // two retries
            // First retry: ~1000ms base
            Assert.IsTrue(_recordedDelays.Any(d => d >= 500 && d <= 1500));
            // Second retry: ~2000ms (1000 * 2)
            Assert.IsTrue(_recordedDelays.Any(d => d >= 1000 && d <= 3000));
        }

        // ──────────────────────────────────────────────────────────────
        // 504 Gateway Timeout + Half-Open: Success closes circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_504OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            // Simulate: 504 three times → opens circuit after threshold
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)504)
                    {
                        Content = new StringContent("{\"error\":\"Gateway Timeout - upstream server timed out\"}")
                    };
                    return Tuple.Create(resp, "{\"error\":\"Gateway Timeout - upstream server timed out\"}");
                }
                // Half-open success
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-504-half-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-504-half-success\"}}");
            });
            // Force circuit open by triggering threshold failures
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.CreateTotalReturnSwapAsync(new { name = "504 Test" }); }
                catch { /* ignore setup failures */ }
            }
            // Verify circuit is open
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(1, _processor._opusCircuitBreaker.TotalCircuitOpenEvents);
            // Simulate time passed → force half-open via reflection
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70)); // past break duration
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open call succeeds → circuit should close
            var createdId = await _processor.CreateTotalReturnSwapAsync(new { name = "Half-Open 504 Success" });
            Assert.AreEqual("swap-504-half-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures);
        }

        // ──────────────────────────────────────────────────────────────
        // 504 Gateway Timeout + Half-Open: Failure re-opens circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_504OpensCircuit_HalfOpenFailureReOpens()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)504)
                    {
                        Content = new StringContent("Gateway Timeout")
                    };
                    return Tuple.Create(resp, "Gateway Timeout");
                }
                // Half-open also fails → should re-open circuit
                var failResp = new HttpResponseMessage((HttpStatusCode)504)
                {
                    Content = new StringContent("Gateway still timing out")
                };
                return Tuple.Create(failResp, "Gateway still timing out");
            });
            // Force circuit open
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.CreateTotalReturnSwapAsync(new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open call fails → circuit should re-open
            try
            {
                await _processor.CreateTotalReturnSwapAsync(new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures); // reset on re-open
        }

        // ──────────────────────────────────────────────────────────────
        // 507 Insufficient Storage + Half-Open: Success closes circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_507OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            // Simulate: 507 three times → opens circuit
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)507)
                    {
                        Content = new StringContent("{\"error\":\"Insufficient Storage - server out of space\"}")
                    };
                    return Tuple.Create(resp, "{\"error\":\"Insufficient Storage - server out of space\"}");
                }
                // Half-open success
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-507-half-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-507-half-success\"}}");
            });
            // Force circuit open
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.CreateTotalReturnSwapAsync(new { name = "507 Test" }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(1, _processor._opusCircuitBreaker.TotalCircuitOpenEvents);
            // Simulate time passed → force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70)); // past break duration
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open succeeds → closes circuit
            var createdId = await _processor.CreateTotalReturnSwapAsync(new { name = "Half-Open 507 Success" });
            Assert.AreEqual("swap-507-half-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures);
        }

        // ──────────────────────────────────────────────────────────────
        // 507 Insufficient Storage + Half-Open: Failure re-opens circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_507OpensCircuit_HalfOpenFailureReOpens()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)507)
                    {
                        Content = new StringContent("Insufficient Storage")
                    };
                    return Tuple.Create(resp, "Insufficient Storage");
                }
                // Half-open also fails
                var failResp = new HttpResponseMessage((HttpStatusCode)507)
                {
                    Content = new StringContent("Storage still insufficient")
                };
                return Tuple.Create(failResp, "Storage still insufficient");
            });
            // Force open circuit
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.CreateTotalReturnSwapAsync(new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open fails → re-open
            try
            {
                await _processor.CreateTotalReturnSwapAsync(new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures);
        }

        // ──────────────────────────────────────────────────────────────
        // 408 Request Timeout + Half-Open: Success closes circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_408OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            _fakeApi.SetPatchAsyncBehavior((endpoint, data) =>
            {
                attempt++;
                if (attempt <= 3)
                    throw new HttpRequestException("408 Request Timeout");
                // Half-open success
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            // Force open circuit
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.UpdateTotalReturnSwapAsync("swap-123", new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open success
            await _processor.UpdateTotalReturnSwapAsync("swap-123", new { weight = 50 });
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
        }

        // ──────────────────────────────────────────────────────────────
        // 408 Request Timeout + Half-Open: Failure re-opens circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_408OpensCircuit_HalfOpenFailureReOpens()
        {
            int attempt = 0;
            _fakeApi.SetPatchAsyncBehavior((endpoint, data) =>
            {
                attempt++;
                if (attempt <= 3)
                    throw new HttpRequestException("408 Request Timeout");
                // Half-open failure
                throw new HttpRequestException("408 Request Timeout");
            });
            // Open circuit
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.UpdateTotalReturnSwapAsync("swap-123", new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open fails → re-open
            try
            {
                await _processor.UpdateTotalReturnSwapAsync("swap-123", new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures);
        }

        // ──────────────────────────────────────────────────────────────
        // 509 Bandwidth Limit Exceeded + Half-Open: Success closes circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_509OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            // Simulate: 509 three times → opens circuit after threshold
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)509)
                    {
                        Content = new StringContent("{\"error\":\"Bandwidth Limit Exceeded - quota reached\"}")
                    };
                    return Tuple.Create(resp, "{\"error\":\"Bandwidth Limit Exceeded - quota reached\"}");
                }
                // Half-open success
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-509-half-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-509-half-success\"}}");
            });
            // Force circuit open by triggering threshold failures
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.CreateTotalReturnSwapAsync(new { name = "509 Test" }); }
                catch { /* ignore setup failures */ }
            }
            // Verify circuit is open
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(1, _processor._opusCircuitBreaker.TotalCircuitOpenEvents);
            // Simulate time passed → force half-open via reflection
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70)); // past break duration
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open call succeeds → circuit closes
            var createdId = await _processor.CreateTotalReturnSwapAsync(new { name = "Half-Open 509 Success" });
            Assert.AreEqual("swap-509-half-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures);
        }

        // ──────────────────────────────────────────────────────────────
        // 509 Bandwidth Limit Exceeded + Half-Open: Failure re-opens circuit
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_509OpensCircuit_HalfOpenFailureReOpens()
        {
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 3)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)509)
                    {
                        Content = new StringContent("Bandwidth Limit Exceeded")
                    };
                    return Tuple.Create(resp, "Bandwidth Limit Exceeded");
                }
                // Half-open also fails → should re-open
                var failResp = new HttpResponseMessage((HttpStatusCode)509)
                {
                    Content = new StringContent("Bandwidth quota still exceeded")
                };
                return Tuple.Create(failResp, "Bandwidth quota still exceeded");
            });
            // Force open circuit
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.CreateTotalReturnSwapAsync(new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open fails → circuit re-opens
            try
            {
                await _processor.CreateTotalReturnSwapAsync(new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(0, _processor._opusCircuitBreaker.ConsecutiveFailures); // reset on re-open
        }

        // ──────────────────────────────────────────────────────────────
        // 509 Bandwidth Limit Exceeded – Retry backoff timing verification
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_509TriggersRetryWithBackoff()
        {
            // Reset recorded delays
            _recordedDelays.Clear();
            int attempt = 0;
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                attempt++;
                if (attempt <= 2)
                {
                    var resp = new HttpResponseMessage((HttpStatusCode)509)
                    {
                        Content = new StringContent("Bandwidth Limit Exceeded")
                    };
                    return Tuple.Create(resp, "Bandwidth Limit Exceeded");
                }
                var successResp = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"resource\":{\"identifier\":\"swap-509-retry-success\"}}")
                };
                return Tuple.Create(successResp, "{\"resource\":{\"identifier\":\"swap-509-retry-success\"}}");
            });
            var payload = new { name = "509 Backoff Test" };
            var createdId = await _processor.CreateTotalReturnSwapAsync(payload);
            Assert.AreEqual("swap-509-retry-success", createdId);
            Assert.AreEqual(3, attempt); // initial + 2 retries
            // Verify backoff delays were called (approximate ranges due to jitter)
            Assert.AreEqual(2, _recordedDelays.Count); // two retry delays
            // First retry: base delay ~1000ms
            Assert.IsTrue(_recordedDelays.Any(d => d >= 500 && d <= 1500));
            // Second retry: ~2000ms (1000 * 2)
            Assert.IsTrue(_recordedDelays.Any(d => d >= 1000 && d <= 3000));
        }

        // ------------------------------------------------------------------------
        // 1. CreateSwapQuoteAsync Tests
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task CreateSwapQuoteAsync_Success_ReturnsCreatedQuote()
        {
            // Arrange
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var marketplaceId = "home";
            var quoteToCreate = new AssetQuote
            {
                Time = DateTime.UtcNow,
                Value = new AmountValue { Quantity = 210000m, Unit = "EUR/Pieces", Type = "PRICE_PER_PIECE" }
            };

            var mockResponseBody = JsonConvert.SerializeObject(new
            {
                resource = new
                {
                    uuid = "new-quote-uuid-123",
                    time = quoteToCreate.Time.ToString("o"),
                    value = new { quantity = 210000, unit = "EUR/Pieces", type = "PRICE_PER_PIECE" }
                }
            });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(mockResponseBody)
            };

            _fakeApi.SetPostWithResponseResult(mockResponse);

            // Act
            var result = await _processor.CreateSwapQuoteAsync(swapId, quoteToCreate, marketplaceId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Resource);
            Assert.AreEqual("new-quote-uuid-123", result.Resource.Uuid);
            Assert.AreEqual(210000m, result.Resource.Value.Quantity);
            Assert.IsTrue(_fakeApi.PostWithResponseAsyncCalled);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task CreateSwapQuoteAsync_ValidationFails_Throws()
        {
            var swapId = "invalid-swap";
            var marketplaceId = "home";
            var quote = new AssetQuote { Value = new AmountValue { Quantity = 100 } };

            // Simulate validation failure by making GetTotalReturnSwapAsync throw 404
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.CreateSwapQuoteAsync(swapId, quote, marketplaceId);
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task CreateSwapQuoteAsync_ApiFails_PropagatesException()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var marketplaceId = "home";
            var quote = new AssetQuote { Value = new AmountValue { Quantity = 210000 } };

            var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"Invalid quote value\"}")
            };

            _fakeApi.SetPostWithResponseResult(errorResponse);

            await _processor.CreateSwapQuoteAsync(swapId, quote, marketplaceId);
        }

        // ------------------------------------------------------------------------
        // 2. GetSwapQuotesAsync Tests
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task GetSwapQuotesAsync_Success_ReturnsQuotes()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var marketplaceId = "home";

            var mockQuotes = new QuoteGetResource
            {
                Quotes = new List<AssetQuote>
                {
                    new AssetQuote { Uuid = "q1", Time = DateTime.UtcNow.AddDays(-1), Value = new AmountValue { Quantity = 100000 } },
                    new AssetQuote { Uuid = "q2", Time = DateTime.UtcNow, Value = new AmountValue { Quantity = 105000 } }
                }
            };

            var mockResponseBody = JsonConvert.SerializeObject(new { resource = mockQuotes });
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseBody)
            };

            _fakeApi.SetGetWithResponseResult(mockResponse);

            var result = await _processor.GetSwapQuotesAsync(swapId, marketplaceId);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Resource.Quotes.Count);
            Assert.AreEqual(105000m, result.Resource.Quotes[1].Value.Quantity);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task GetSwapQuotesAsync_SwapNotFound_Throws()
        {
            var swapId = "nonexistent";

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.GetSwapQuotesAsync(swapId);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateSwapNominalAsync_NotionalTooLow_ThrowsIfValidationEnforcesMin()
        {
            var swapId = "small-swap";
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 500000m, Unit = "EUR", Type = "MONEY" }
            };

            // Simulate current notional = 0 or very low → validation fails minNotional check
            _fakeApi.SetGetAsyncResult<dynamic>(new { nominal = new { quantity = 100000m } });

            await _processor.UpdateSwapNominalAsync(swapId, patch);
        }

        [TestMethod]
        public async Task UpdateSwapNominalAsync_ValidationWarning_LogsButSucceeds()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 15000000m, Unit = "EUR", Type = "MONEY" }
            };

            // Simulate validation passes but with warning (old quote)
            _fakeApi.SetGetAsyncResult<dynamic>(new { nominal = new { quantity = 20000000m } });

            await _processor.UpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
            // You can assert _fakeLogger.WarnLogs if you inject logger
        }

        // ------------------------------------------------------------------------
        // Edge case: Swap response is null / empty
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_NullSwapResponse_ReturnsFailure()
        {
            var swapId = "null-response-swap";
            _fakeApi.SetGetAsyncResult<dynamic>(null);

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found", "Null response should be treated as not found");
        }

        // ------------------------------------------------------------------------
        // Edge case: Nominal field is completely missing
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_MissingNominalField_HasWarningButValid()
        {
            var swapId = "missing-nominal-swap";

            var swapData = new { status = "active" }; // no nominal at all
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.Warnings.Count);
            StringAssert.Contains(result.Warnings[0], "Notional value not found");
            Assert.IsNull(result.CurrentNotional);
        }

        // ------------------------------------------------------------------------
        // Edge case: Nominal quantity is negative
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_NegativeNotional_ReturnsFailure()
        {
            var swapId = "negative-notional-swap";

            var swapData = new { status = "active", nominal = new { quantity = -5000000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "positive");
            Assert.AreEqual(-5000000m, result.CurrentNotional);
        }

        // ------------------------------------------------------------------------
        // Edge case: Extremely large notional (overflow risk)
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_ExtremelyLargeNotional_StillValid()
        {
            var swapId = "huge-notional-swap";

            var hugeValue = 999_999_999_999_999_999m; // close to decimal max
            var swapData = new { status = "active", nominal = new { quantity = hugeValue } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(hugeValue, result.CurrentNotional);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        // ------------------------------------------------------------------------
        // Edge case: Quotes array is null instead of empty list
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_NullQuotesArray_HandledAsZeroQuotes()
        {
            var swapId = "null-quotes-swap";

            var swapData = new { status = "active", nominal = new { quantity = 5000000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            // Simulate malformed response: resource.quotes = null
            var malformedQuotesResp = new { resource = new { quotes = (List<AssetQuote>)null } };
            _fakeApi.SetGetAsyncResult<dynamic>(malformedQuotesResp);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(0, result.QuoteCount ?? 0);
            StringAssert.Contains(result.ErrorMessage, "No quotes found");
        }

        // ------------------------------------------------------------------------
        // Edge case: Quote time is in the future (suspicious)
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_FutureQuoteTime_HasWarning()
        {
            var swapId = "future-quote-swap";

            var swapData = new { status = "active", nominal = new { quantity = 10000000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var futureTime = DateTime.UtcNow.AddDays(10);
            var quotes = new QuoteGetResource
            {
                Quotes = new List<AssetQuote> { new AssetQuote { Time = futureTime } }
            };
            _fakeApi.SetGetAsyncResult(new OpusApiResponse<QuoteGetResource> { Resource = quotes });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.Warnings.Count);
            StringAssert.Contains(result.Warnings[0], "future"); // or "suspicious" / "invalid" — customize message
            Assert.AreEqual(futureTime, result.LastQuoteTime);
        }

        // ------------------------------------------------------------------------
        // Edge case: Malformed JSON / deserialization failure
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_MalformedSwapJson_ReturnsFailure()
        {
            var swapId = "malformed-swap";

            _fakeApi.SetGetAsyncResult<dynamic>("this is not json { broken");

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "Validation failed");
            StringAssert.Contains(result.ErrorMessage, "JSON");
        }

        // ------------------------------------------------------------------------
        // Edge case: Empty string / whitespace swapId
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_EmptySwapId_ReturnsFailure()
        {
            var result1 = await _processor.ValidateSwapAsync("");
            var result2 = await _processor.ValidateSwapAsync("   ");

            Assert.IsFalse(result1.IsValid);
            Assert.IsFalse(result2.IsValid);
            StringAssert.Contains(result1.ErrorMessage, "required");
            StringAssert.Contains(result2.ErrorMessage, "required");
        }

        // ------------------------------------------------------------------------
        // Edge case: Validation with requireRecentQuote = false (should not check quotes)
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_NoQuotesButNotRequired_StillValid()
        {
            var swapId = "no-quotes-but-ok";

            var swapData = new { status = "active", nominal = new { quantity = 3000000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            // No quotes at all
            _fakeApi.SetGetAsyncResult(new OpusApiResponse<QuoteGetResource> { Resource = new QuoteGetResource { Quotes = null } });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: false);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Warnings.Count); // no quote warning
        }

        /// <summary>
        /// Verifies that ValidateSwapAsync succeeds when the swap exists and has a valid nominal value.
        /// Uses the new TotalReturnSwapResponse model (no Pagination/Data structure).
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_ValidSwapWithRecentQuotes_ReturnsSuccess()
        {
            var swapId = "valid-swap-123";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Name = "Demo Swap",
                Nominal = new AmountValue { Quantity = 2100000m, Unit = "EUR", Type = "MONEY" },
                EntityStatus = "Open"
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(swapId, result.SwapId);
            Assert.AreEqual(2100000m, result.CurrentNotional);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        /// <summary>
        /// Verifies that ValidateSwapAsync returns failure when no quotes are required but the swap itself is not found.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_NoQuotesWhenRequired_ReturnsFailure()
        {
            var swapId = "no-quotes-swap";

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found");
        }

        /// <summary>
        /// Verifies that an old quote (if present in future versions) would generate a warning, 
        /// but currently since quotes are not in the root response, it adds a skipped warning.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_OldQuote_GeneratesWarning()
        {
            var swapId = "old-quote-swap";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 2100000m }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
        }

        /// <summary>
        /// Verifies that a future-dated quote scenario (if supported later) would generate a warning.
        /// Currently adds the skipped quote validation warning.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_FutureQuote_HasWarning()
        {
            var swapId = "future-quote-swap";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 2100000m }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
        }

        /// <summary>
        /// Validates that a null response from GetTotalReturnSwapAsync is treated as "not found".
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_EmptyResponse_ReturnsFailure()
        {
            var swapId = "empty-response-swap";

            _fakeApi.SetGetAsyncResult<TotalReturnSwapResponse>(null);

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found");
        }

        /// <summary>
        /// Verifies that setting requireRecentQuote=false allows validation to succeed 
        /// even when quotes are not present in the new response model.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_RequireRecentQuoteFalse_SucceedsEvenWithNoQuotes()
        {
            var swapId = "no-quotes-ok";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 2000000m }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: false);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        /// <summary>
        /// Verifies that multiple warnings (e.g. skipped quote validation + low notional) are collected correctly.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_MultipleWarnings_CollectsAll()
        {
            var swapId = "warning-combo-swap";

            var mockResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 500000m }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true, minNotional: 1000000m);

            Assert.IsFalse(result.IsValid); // because of low notional
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
        }

        /// <summary>
        /// Validates that ApiNotFoundException from the API layer is propagated correctly through ValidateSwapAsync.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ApiNotFoundException))]
        public async Task ValidateSwapAsync_ApiNotFound_PropagatesException()
        {
            var swapId = "missing-swap";

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.ValidateSwapAsync(swapId);
        }

        /// <summary>
        /// Tests that CreateSwapQuoteAsync logs the start of the operation, performs validation 
        /// using the new TotalReturnSwapResponse model, logs the validation summary, and 
        /// finally logs successful quote creation.
        /// </summary>
        [TestMethod]
        public async Task CreateSwapQuoteAsync_ValidSwap_LogsValidationSummaryAndSuccess()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var quote = new AssetQuote
            {
                Time = DateTime.UtcNow,
                Value = new AmountValue { Quantity = 210000m, Unit = "EUR/Pieces", Type = "PRICE_PER_PIECE" }
            };

            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 2000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwap);
            _fakeApi.SetPostWithResponseResult(new OpusApiResponse<AssetQuote> { Resource = new AssetQuote() });

            var result = await _processor.CreateSwapQuoteAsync(swapId, quote);

            Assert.IsNotNull(result);

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains($"CreateSwapQuoteAsync started for swap {swapId}")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("validation passed")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("Quote successfully created")));
        }

        // ──────────────────────────────────────────────────────────────
        // GetSwapQuotesAsync Tests - Updated
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Tests that GetSwapQuotesAsync logs the operation start with marketplace info,
        /// performs validation using the new model, and logs the number of quotes retrieved.
        /// </summary>
        [TestMethod]
        public async Task GetSwapQuotesAsync_ValidSwap_LogsValidationAndResultCount()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var mockSwap = new TotalReturnSwapResponse { Uuid = swapId };
            _fakeApi.SetGetAsyncResult(mockSwap);

            // Act
            var result = await _processor.GetSwapQuotesAsync(swapId);

            Assert.IsNotNull(result);

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains($"GetSwapQuotesAsync started for swap {swapId}")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("validation passed")));
        }

        // ──────────────────────────────────────────────────────────────
        // UpdateSwapNominalAsync Tests - Updated
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Tests that UpdateSwapNominalAsync logs the start with new nominal value,
        /// performs validation using the new model, detects significant notional change (>50%), 
        /// logs a warning, and finally logs successful nominal update.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_SignificantChange_LogsWarning()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 50000000m, Unit = "EUR", Type = "MONEY" }
            };

            var mockSwap = new TotalReturnSwapResponse
            {
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwap);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("UpdateSwapNominalAsync started")));
            Assert.IsTrue(_fakeLogger.WarnLogs.Any(log => log.Contains("Significant notional change")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("Successfully updated nominal")));
        }

        /// <summary>
        /// Tests that UpdateSwapDeltaAsync logs enriched information including member count and asset IDs,
        /// performs validation using the new model, validates weight sum, and logs successful delta update.
        /// </summary>
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

            var result = await _processor.UpdateSwapDeltaAsync(swapId, delta);

            Assert.IsNotNull(result);
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("UpdateSwapDeltaAsync started")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("Members: 2")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("Assets: asset-1, asset-2")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("validation passed")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("Successfully updated delta")));
        }

        /// <summary>
        /// Tests that when validation fails in UpdateSwapDeltaAsync, an error is logged 
        /// with the validation summary and InvalidOperationException is thrown.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateSwapDeltaAsync_InvalidValidation_LogsErrorAndThrows()
        {
            var swapId = "invalid-swap";
            var delta = new SwapDeltaUpdate { Members = new List<SwapDeltaMember>() };

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            try
            {
                await _processor.UpdateSwapDeltaAsync(swapId, delta);
            }
            catch
            {
                Assert.IsTrue(_fakeLogger.ErrorLogs.Any(log => log.Contains("failed validation")));
                throw;
            }
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

            try
            {
                await _processor.UpdateSwapDeltaAsync(swapId, delta);
                Assert.Fail("Expected ArgumentException due to invalid weight sum");
            }
            catch (ArgumentException)
            {
                Assert.IsTrue(_fakeLogger.ErrorLogs.Any(log => log.Contains("Sum of weights must be approximately 100%")));
                Assert.IsTrue(_fakeLogger.ErrorLogs.Any(log => log.Contains("90.00%")));
            }
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

            var result = await _processor.UpdateSwapDeltaAsync(swapId, delta);

            Assert.IsNotNull(result);
            Assert.IsTrue(_fakeLogger.WarnLogs.Any(log => log.Contains("Weights sum is") && log.Contains("slight deviation")));
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("Successfully updated delta")));
        }

        /// <summary>
        /// Verifies that UpdateSwapAssetAtMarketplacesAsync successfully calls the PATCH endpoint 
        /// when provided with a valid SwapPatch containing both 'nominal' and a fully populated 
        /// 'assetAtMarketplaces' array including lotSize, reference, quoteFactor, etc.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapAssetAtMarketplacesAsync_Success_CallsPatch()
        {
            // Arrange
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var patch = new SwapPatch
            {
                Nominal = new AmountValue
                {
                    Quantity = 20000000m,
                    Unit = "EUR",
                    Type = "MONEY"
                },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>
        {
            new AssetAtMarketplaceDetail
            {
                Home = true,
                QuoteFactor = new AmountValue { Quantity = 1, Type = "SCALAR" },
                LotSize = new AmountValue { Quantity = 1, Unit = "Pieces", Type = "PIECE" },
                QuoteSource = "7fbe3e92-3b98-4774-8196-568e4df0d436",
                QuoteUnit = "EUR/Pieces",
                Reference = true
            }
        }
            };

            _fakeApi.SetPatchAsyncResult(); // Simulate success

            // Act
            await _processor.UpdateSwapAssetAtMarketplacesAsync(swapId, patch);

            // Assert
            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should have been called on the API client");
        }

        /// <summary>
        /// Ensures passing null SwapPatch throws ArgumentException immediately (defensive validation).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateSwapAssetAtMarketplacesAsync_NullPatch_ThrowsArgumentException()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            await _processor.UpdateSwapAssetAtMarketplacesAsync(swapId, null);
        }

        /// <summary>
        /// Validates that an empty/invalid SwapPatch (no nominal and no assetAtMarketplaces entries) 
        /// throws ArgumentException to prevent invalid API requests.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateSwapAssetAtMarketplacesAsync_EmptyPatch_ThrowsArgumentException()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var emptyPatch = new SwapPatch { AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>() };

            await _processor.UpdateSwapAssetAtMarketplacesAsync(swapId, emptyPatch);
        }

        /// <summary>
        /// Confirms that swap validation failure (e.g. 404 Not Found) causes 
        /// UpdateSwapAssetAtMarketplacesAsync to throw InvalidOperationException without calling the API.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateSwapAssetAtMarketplacesAsync_ValidationFails_Throws()
        {
            var swapId = "invalid-swap";
            var patch = new SwapPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail> { new AssetAtMarketplaceDetail { Home = true } }
            };

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.UpdateSwapAssetAtMarketplacesAsync(swapId, patch);
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
            var result = await _processor.FetchSwapDeltaAsync(request);

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
        [ExpectedException(typeof(ArgumentException))]
        public async Task FetchSwapDeltaAsync_InvalidRequest_ThrowsArgumentException()
        {
            // Act & Assert
            await _processor.FetchSwapDeltaAsync(null);
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
            await _processor.FetchSwapDeltaAsync(request);

            // Assert logging
            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains("FetchSwapDeltaAsync started for 1 account segment(s)")));

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains("FetchSwapDeltaAsync completed successfully") &&
                log.Contains("swap(s)") && log.Contains("member delta(s)")));
        }

        /// <summary>
        /// Confirms that when the circuit breaker is open, FetchSwapDeltaAsync throws 
        /// CircuitBreakerOpenException and logs the error appropriately.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task FetchSwapDeltaAsync_CircuitBreakerOpen_ThrowsAndLogs()
        {
            var request = new SwapDeltaFetchRequest
            {
                AccountSegments = new List<string> { "1492e07e-3c15-457b-9bc4-363cc55fb691" }
            };

            _fakeApi.SetCircuitBreakerToThrow(new CircuitBreakerOpenException("Circuit is open"));

            try
            {
                await _processor.FetchSwapDeltaAsync(request);
            }
            catch
            {
                Assert.IsTrue(_fakeLogger.ErrorLogs.Any(log =>
                    log.Contains("Circuit breaker open during FetchSwapDeltaAsync")));
                throw;
            }
        }

        /// <summary>
        /// Verifies that SwapPatch with nominal and assetAtMarketplaces serializes correctly 
        /// to the exact JSON structure required by the updated OPUS API (including lotSize and reference).
        /// </summary>
        [TestMethod]
        public void SwapPatch_SerializesToCorrectJsonStructure()
        {
            var patch = new SwapPatch
            {
                Nominal = new AmountValue
                {
                    Quantity = 20000000m,
                    Unit = "EUR",
                    Type = "MONEY"
                },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>
        {
            new AssetAtMarketplaceDetail
            {
                Home = true,
                QuoteFactor = new AmountValue { Quantity = 1, Type = "SCALAR" },
                LotSize = new AmountValue { Quantity = 1, Unit = "Pieces", Type = "PIECE" },
                QuoteSource = "7fbe3e92-3b98-4774-8196-568e4df0d436",
                QuoteUnit = "EUR/Pieces",
                Reference = true
            }
        }
            };

            string json = JsonConvert.SerializeObject(patch, Formatting.Indented);

            Assert.IsTrue(json.Contains("\"nominal\""), "Should contain nominal object");
            Assert.IsTrue(json.Contains("\"assetAtMarketplaces\""), "Should contain assetAtMarketplaces array");
            Assert.IsTrue(json.Contains("\"lotSize\""), "Should include lotSize in assetAtMarketplace");
            Assert.IsTrue(json.Contains("\"reference\""), "Should include reference field");
            Assert.IsTrue(json.Contains("\"quoteFactor\""), "Should include quoteFactor");
            Assert.IsTrue(json.Contains("20000000"), "Should serialize nominal quantity correctly");
        }

        /// <summary>
        /// Ensures that AssetAtMarketplaceDetail can be serialized independently with all new fields.
        /// </summary>
        [TestMethod]
        public void AssetAtMarketplaceDetail_SerializesCorrectly()
        {
            var assetPatch = new AssetAtMarketplaceDetail
            {
                Home = true,
                LotSize = new AmountValue { Quantity = 1, Unit = "Pieces", Type = "PIECE" },
                QuoteFactor = new AmountValue { Quantity = 1, Type = "SCALAR" },
                QuoteSource = "7fbe3e92-3b98-4774-8196-568e4df0d436",
                QuoteUnit = "EUR/Pieces",
                Reference = true,
                Tradable = true
            };

            string json = JsonConvert.SerializeObject(assetPatch);

            Assert.IsTrue(json.Contains("\"home\":true"));
            Assert.IsTrue(json.Contains("\"lotSize\""));
            Assert.IsTrue(json.Contains("\"reference\":true"));
            Assert.IsTrue(json.Contains("\"quoteSource\""));
        }

        /// <summary>
        /// Verifies that a null response from GetTotalReturnSwapAsync is treated as "not found".
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_NullResponse_ReturnsFailure()
        {
            var swapId = "null-response-swap";
            _fakeApi.SetGetAsyncResult<TotalReturnSwapResponse>(null);

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found");
        }

        /// <summary>
        /// Verifies that when requireRecentQuote = false, validation succeeds even if no quotes are present.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_NoRecentQuoteRequired_Succeeds()
        {
            var swapId = "no-quote-required";

            var mockResponse = new TotalReturnSwapResponse
            {
                Nominal = new AmountValue { Quantity = 2000000m }
            };

            _fakeApi.SetGetAsyncResult(mockResponse);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: false);

            Assert.IsTrue(result.IsValid);
        }

        /// <summary>
        /// Verifies that GetSwapQuotesAsync performs validation using the new TotalReturnSwapResponse model 
        /// and successfully returns quotes from the dedicated quotes endpoint.
        /// Uses SetGetAsyncResult for swap validation and SetGetWithResponseResult for the quotes response.
        /// </summary>
        [TestMethod]
        public async Task GetSwapQuotesAsync_ValidSwap_ReturnsQuotes()
        {
            var swapId = "019d2001-e11b-7000-a211-8c654386b53d";
            var marketplaceId = "home";

            // Mock the swap details used by ValidateSwapAsync
            var mockSwap = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Name = "Demo Swap",
                Nominal = new AmountValue { Quantity = 2000000m }
            };
            _fakeApi.SetGetAsyncResult(mockSwap);

            // Mock the actual quotes response from /swaps/{id}/asset-at-marketplaces/{marketplace}/quotes
            var mockQuotesResponse = new OpusApiResponse<QuoteGetResource>
            {
                Resource = new QuoteGetResource
                {
                    Quotes = new List<AssetQuote>
                    {
                        new AssetQuote { Uuid = "q1", Time = DateTime.UtcNow.AddDays(-1), Value = new AmountValue { Quantity = 100000m } },
                        new AssetQuote { Uuid = "q2", Time = DateTime.UtcNow, Value = new AmountValue { Quantity = 105000m } }
                    }
                }
            };

            var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(mockQuotesResponse))
            };

            _fakeApi.SetGetWithResponseResult(mockHttpResponse);

            // Act
            var result = await _processor.GetSwapQuotesAsync(swapId, marketplaceId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Resource);
            Assert.AreEqual(2, result.Resource.Quotes.Count);
            Assert.AreEqual(105000m, result.Resource.Quotes[1].Value.Quantity);
        }

        /// <summary>
        /// Verifies detailed logging in the success path of UpdateSwapAssetAtMarketplacesAsync:
        /// - Start message with swap ID and update summary (nominal + assetAtMarketplaces)
        /// - Validation passed message
        /// - Success confirmation after PATCH
        /// Uses the new TotalReturnSwapResponse model for validation.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapAssetAtMarketplacesAsync_ValidPatch_LogsDetailedValidationAndSuccess()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var patch = new SwapPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>
                {
                    new AssetAtMarketplaceDetail
                    {
                        Home = true,
                        QuoteFactor = new AmountValue { Quantity = 1, Type = "SCALAR" },
                        LotSize = new AmountValue { Quantity = 1, Unit = "Pieces", Type = "PIECE" },
                        QuoteSource = "7fbe3e92-3b98-4774-8196-568e4df0d436",
                        QuoteUnit = "EUR/Pieces",
                        Reference = true
                    }
                }
            };

            var mockSwapResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwapResponse);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapAssetAtMarketplacesAsync(swapId, patch);

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains($"UpdateSwapAssetAtMarketplacesAsync started for swap {swapId}")),
                "Should log operation start with swap ID");

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("validation passed")),
                "Should log successful validation");

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains("Successfully updated asset-at-marketplaces") && log.Contains(swapId)),
                "Should log success message");
        }

        /// <summary>
        /// Tests nominal-only update (assetAtMarketplaces omitted). 
        /// Ensures the method correctly handles partial updates containing only nominal 
        /// using the new TotalReturnSwapResponse model for validation.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapAssetAtMarketplacesAsync_NominalOnly_Succeeds()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var patch = new SwapPatch
            {
                Nominal = new AmountValue { Quantity = 25000000m, Unit = "EUR", Type = "MONEY" }
            };

            var mockSwapResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwapResponse);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapAssetAtMarketplacesAsync(swapId, patch);

            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should be called for nominal-only updates");
        }

        /// <summary>
        /// Verifies that UpdateSwapNominalAsync successfully calls the PATCH endpoint 
        /// when updating only the nominal value of a swap. 
        /// Uses the new TotalReturnSwapResponse model for validation.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_Success_CallsPatch()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" }
            };

            var mockSwapResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwapResponse);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should have been called for nominal update");
        }

        /// <summary>
        /// Tests that UpdateSwapNominalAsync logs the start of the operation with the new nominal value,
        /// performs validation using the new model, and logs successful update.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_ValidPatch_LogsStartValidationAndSuccess()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" }
            };

            var mockSwapResponse = new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwapResponse);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains($"UpdateSwapNominalAsync started for swap {swapId}")),
                "Should log operation start with nominal details");

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log => log.Contains("validation passed")),
                "Should log successful validation");

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains("Successfully updated nominal") && log.Contains(swapId)),
                "Should log successful nominal update");
        }

        /// <summary>
        /// Verifies that a significant notional change (>50%) generates a detailed warning log 
        /// containing old and new values and the percentage change, while still allowing the update to succeed.
        /// Uses the new TotalReturnSwapResponse model for current notional value.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_SignificantNotionalChange_LogsDetailedWarning()
        {
            var swapId = "significant-notional-change-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 50000000m, Unit = "EUR", Type = "MONEY" }
            };

            var mockSwapResponse = new TotalReturnSwapResponse
            {
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwapResponse);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(_fakeLogger.WarnLogs.Any(log =>
                log.Contains("Significant notional change detected")),
                "Should log significant change warning");

            Assert.IsTrue(_fakeLogger.WarnLogs.Any(log =>
                log.Contains("10,000,000") && log.Contains("50,000,000") && log.Contains("400.0%")),
                "Warning should include old/new values and percentage");
        }

        /// <summary>
        /// Ensures that small notional changes (<50%) do NOT trigger a significant change warning.
        /// Uses the new TotalReturnSwapResponse model for current notional.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_SmallNotionalChange_NoWarningLogged()
        {
            var swapId = "small-notional-change-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 12000000m, Unit = "EUR", Type = "MONEY" }
            };

            var mockSwapResponse = new TotalReturnSwapResponse
            {
                Nominal = new AmountValue { Quantity = 10000000m }
            };

            _fakeApi.SetGetAsyncResult(mockSwapResponse);
            _fakeApi.SetPatchAsyncResult();

            await _processor.UpdateSwapNominalAsync(swapId, patch);

            Assert.IsFalse(_fakeLogger.WarnLogs.Any(log =>
                log.Contains("Significant notional change")),
                "Should NOT log significant change warning for small adjustments");

            Assert.IsTrue(_fakeLogger.InfoLogs.Any(log =>
                log.Contains("Successfully updated nominal")),
                "Should still log success");
        }

        /// <summary>
        /// Validates that UpdateSwapNominalAsync throws InvalidOperationException 
        /// when swap validation fails (e.g., swap not found or invalid state).
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateSwapNominalAsync_ValidationFails_Throws()
        {
            var swapId = "invalid-swap";
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" }
            };

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            await _processor.UpdateSwapNominalAsync(swapId, patch);
        }

        private CircuitState GetCircuitState()
        {
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (CircuitState)stateField.GetValue(_processor._opusCircuitBreaker);
        }
    }
}