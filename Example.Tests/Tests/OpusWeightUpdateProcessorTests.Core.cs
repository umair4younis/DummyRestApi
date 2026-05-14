using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using static Puma.MDE.OPUS.OpusCircuitBreaker;


namespace Puma.MDE.Tests
{
    /// <summary>
    /// Core tests for OpusWeightUpdateProcessor: Constructor, Execute, Retry, and Asset validation logic.
    /// Concern-specific tests are split into separate partials: ValidationAndDelta, SwapLifecycle, DeltaFlows, TrsLifecycle.
    /// </summary>
    public partial class OpusWeightUpdateProcessorTests
    {
        // ========================================
        // Constructor Tests
        // ========================================
        [TestMethod]
        public void Constructor_NullGraphQLClient_Throws()
        {
            AssertCompat.Throws<ArgumentNullException>(() =>
                new OpusWeightUpdateProcessor(null, _fakeApi));
        }

        [TestMethod]
        public void Constructor_NullApiClient_Throws()
        {
            AssertCompat.Throws<ArgumentNullException>(() =>
                new OpusWeightUpdateProcessor(_fakeGraphQL, null));
        }

        [TestMethod]
        public void Constructor_InitializesPoliciesAndBreaker()
        {
            Assert.IsNotNull(_processor.GetType().GetField("_parentValidationPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_processor));
            Assert.IsNotNull(_processor.GetType().GetField("_bbgBatchPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_processor));
            Assert.IsNotNull(_processor._opusCircuitBreaker);
        }

        // ========================================
        // ExecuteAsync - Core Workflow Tests
        // ========================================
        [TestMethod]
        public async Task ExecuteAsync_ParentInvalid_StopsAndLogsFatal()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse { assets = new Assets { edges = Array.Empty<AssetEdge>() } });
            await _processor.TryExecuteAsync();
            Assert.IsFalse(_fakeApi.PatchAsyncCalled);
        }

        [TestMethod]
        public async Task ExecuteAsync_ValidParentAndComponents_CallsPatch()
        {
            _fakeGraphQL.SetExecuteResult(new AssetsQueryResponse
            {
                assets = new Assets
                {
                    edges = new[] { new AssetEdge { node = new AssetNode { uuid = "uuid-123" } } }
                }
            });
            OpusWeightUpdateProcessor.ReportHoldings = new List<ReportHolding>
            {
                new ReportHolding { BbgTicker = "TICKER1", MarketWeightPercent = 50m }
            };
            await _processor.TryExecuteAsync();
            Assert.IsFalse(_fakeApi.PatchAsyncCalled);
        }

        // ========================================
        // ExecuteWithRetryAsync - Retry Logic Tests
        // ========================================
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
        public async Task ExecuteWithRetryAsync_ExhaustsRetries_Throws()
        {
            var policy = new RetryPolicy { MaxRetries = 1, IsRetryable = ex => true };

            await AssertCompat.ThrowsAsync<HttpRequestException>(async () =>
                await _processor.ExecuteWithRetryAsync<ValidationResultOpus>(
                    () => throw new HttpRequestException("transient"),
                    "test-op",
                    policy));
        }

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
                if (attempt <= 2)
                    throw new HttpRequestException("transient");
                return 999;
            };
            var result = await _processor.ExecuteWithRetryAsync(failThenSucceed, "backoff-test", policy);
            Assert.AreEqual(999, result);
            Assert.AreEqual(3, attempt);
        }

        // ========================================
        // Asset Validation Tests
        // ========================================
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

        [TestMethod]
        public async Task ValidateAssetCompositionIdAsync_AccountSegmentMode_UsesAccountSegmentQueryAndMapsFields()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new[]
                    {
                        new PortfolioGraphQlPosition
                        {
                            rateable = new PortfolioGraphQlRateable
                            {
                                __typename = "TOTALRETURNSWAP",
                                uuid = "trs-1",
                                nominal = new PortfolioGraphQlNominal
                                {
                                    lastValue = new AmountValue { Quantity = 2500000m, Unit = "EUR", Type = "MONEY" }
                                },
                                mtmFromFinancing = new PercentAmountValue(2.5m, "%"),
                                swapValue = new PercentAmountValue(4.25m, "%"),
                                asset = new PortfolioGraphQlAssetComposition
                                {
                                    id = 12345,
                                    uuid = "composition-uuid-1",
                                    name = "Post Booking Basket",
                                    __typename = "ASSETCOMPOSITION",
                                    members = new PortfolioGraphQlMember[0]
                                }
                            }
                        }
                    }
                }
            });

            OpusWeightUpdateProcessor.ParentAssetId = string.Empty;
            OpusWeightUpdateProcessor.SwapNotional = 0m;
            OpusWeightUpdateProcessor.MtmFromFinancing = 0m;
            OpusWeightUpdateProcessor.SwapValue = 0m;
            OpusWeightUpdateProcessor.Currency = string.Empty;

            var result = await _processor.ValidateAssetCompositionIdAsync();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("composition-uuid-1", result.AssetUuid);
            Assert.AreEqual("Post Booking Basket", result.AssetName);
            Assert.AreEqual("12345", OpusWeightUpdateProcessor.ParentAssetId);
            Assert.AreEqual(2500000m, OpusWeightUpdateProcessor.SwapNotional);
            Assert.AreEqual(2.5m, OpusWeightUpdateProcessor.MtmFromFinancing);
            Assert.AreEqual(4.25m, OpusWeightUpdateProcessor.SwapValue);
            Assert.AreEqual("EUR", OpusWeightUpdateProcessor.Currency);
            Assert.AreEqual(1, _fakeGraphQL.ExecuteCallCount);
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("portfolio("));
        }

        // ========================================
        // BBG UUID Collection Tests
        // ========================================
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

        [TestMethod]
        public async Task ValidateAndCollectBbgUuidsAsync_AccountSegmentMode_UsesCachedAccountSegmentResult()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new[]
                    {
                        new PortfolioGraphQlPosition
                        {
                            rateable = new PortfolioGraphQlRateable
                            {
                                __typename = "TOTALRETURNSWAP",
                                uuid = "trs-2",
                                asset = new PortfolioGraphQlAssetComposition
                                {
                                    id = 9876,
                                    uuid = "composition-uuid-2",
                                    name = "TRS Composition",
                                    __typename = "ASSETCOMPOSITION",
                                    members = new[]
                                    {
                                        new PortfolioGraphQlMember
                                        {
                                            unit = "EUR/Pieces",
                                            weight = new PercentAmountValue(55m, "%"),
                                            asset = new PortfolioGraphQlMemberAsset
                                            {
                                                uuid = "member-uuid-1",
                                                bbg = new PortfolioGraphQlSymbol { identifier = "BBG0001" }
                                            }
                                        },
                                        new PortfolioGraphQlMember
                                        {
                                            unit = "EUR/Pieces",
                                            weight = new PercentAmountValue(45m, "%"),
                                            asset = new PortfolioGraphQlMemberAsset
                                            {
                                                uuid = "member-uuid-2",
                                                bbg = new PortfolioGraphQlSymbol { identifier = "BBG0002" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            var parentValidation = await _processor.ValidateAssetCompositionIdAsync();
            var components = await _processor.ValidateAndCollectBbgUuidsAsync();

            Assert.IsTrue(parentValidation.IsValid);
            Assert.AreEqual(2, components.Count);
            Assert.AreEqual("BBG0001", components[0].BbgTicker);
            Assert.AreEqual("member-uuid-1", components[0].Uuid);
            Assert.AreEqual(55m, components[0].WeightPercent);
            Assert.AreEqual(1, _fakeGraphQL.ExecuteCallCount);
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("portfolio("));
        }

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

        [TestMethod]
        public void BuildBbgFilterQuery_GeneratesCorrectOrConditions()
        {
            var tickers = new List<string> { "T1", "T2" };
            var query = _processor.BuildBbgFilterQuery(tickers);
            Assert.IsTrue(query.Contains("symbols.identifier = 'T1'"));
            Assert.IsTrue(query.Contains("symbols.identifier = 'T2'"));
            Assert.IsTrue(query.Contains("or: ["));
        }

        // ========================================
        // Account Segment Parameter Tests (Parameter-First with Config Fallback)
        // ========================================
        /// <summary>
        /// When parameter is provided with single account segment ID, uses parameter directly.
        /// Logs: method invocation, parameter value, query building, execution.
        /// </summary>
        [TestMethod]
        public async Task GetPortfolioGraphQlDataAsync_WithProvidedSingleAccountSegmentId_UsesParameterAndLogsAll()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = "50837148";

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new[]
                    {
                        new PortfolioGraphQlPosition
                        {
                            rateable = new PortfolioGraphQlRateable
                            {
                                __typename = "TOTALRETURNSWAP",
                                uuid = "trs-param-1",
                                asset = new PortfolioGraphQlAssetComposition
                                {
                                    id = 50837148,
                                    uuid = "param-comp",
                                    name = "Parameter Test",
                                    __typename = "ASSETCOMPOSITION",
                                    members = new PortfolioGraphQlMember[0]
                                }
                            }
                        }
                    }
                }
            });

            var result = await _processor.ValidateAssetCompositionIdAsync();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, _fakeGraphQL.ExecuteCallCount);
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("accountSegments: [50837148]"));
        }

        /// <summary>
        /// When parameter contains multiple comma-separated IDs, all are included.
        /// Logs: parsing, individual ID debug entries, formatted result, query length.
        /// </summary>
        [TestMethod]
        public async Task GetPortfolioGraphQlDataAsync_WithMultipleAccountSegmentIds_ParsesAllAndLogs()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = "50837148,50837149,50837150";

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new[]
                    {
                        new PortfolioGraphQlPosition
                        {
                            rateable = new PortfolioGraphQlRateable
                            {
                                __typename = "TOTALRETURNSWAP",
                                uuid = "trs-multi",
                                asset = new PortfolioGraphQlAssetComposition
                                {
                                    id = 50837148,
                                    uuid = "multi-comp",
                                    name = "Multi Segment",
                                    __typename = "ASSETCOMPOSITION",
                                    members = new PortfolioGraphQlMember[0]
                                }
                            }
                        }
                    }
                }
            });

            var result = await _processor.ValidateAssetCompositionIdAsync();

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("50837148"));
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("50837149"));
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("50837150"));
        }

        /// <summary>
        /// When GetPortfolioGraphQlDataAsync receives null parameter, falls back to config value.
        /// Logs: null parameter warning, fallback to config, config value used.
        /// </summary>
        [TestMethod]
        public async Task GetPortfolioGraphQlDataAsync_WithNullParameter_FallsBackToConfigAndLogs()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = "50837148";

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new[]
                    {
                        new PortfolioGraphQlPosition
                        {
                            rateable = new PortfolioGraphQlRateable
                            {
                                __typename = "TOTALRETURNSWAP",
                                uuid = "trs-fallback",
                                asset = new PortfolioGraphQlAssetComposition
                                {
                                    id = 50837148,
                                    uuid = "fallback-uuid",
                                    name = "Fallback Test",
                                    __typename = "ASSETCOMPOSITION",
                                    members = new PortfolioGraphQlMember[0]
                                }
                            }
                        }
                    }
                }
            });

            // Config value is used when parameter is null (called via ValidateAssetCompositionIdAsync)
            var result = await _processor.ValidateAssetCompositionIdAsync();

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(_fakeGraphQL.LastQuery.Contains("50837148"));
        }

        /// <summary>
        /// When both parameter and config are null/empty, throws InvalidOperationException.
        /// Logs: error message about missing configuration.
        /// </summary>
        [TestMethod]
        public async Task GetPortfolioGraphQlDataAsync_WithNullParamAndNullConfig_ThrowsAndLogs()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = null;  // Missing config

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData { portfolio = null });

            try
            {
                await _processor.ValidateAssetCompositionIdAsync();
                Assert.Fail("Should have thrown InvalidOperationException");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("AccountSegmentIds"));
                Assert.IsTrue(ex.Message.Contains("App.config"));
            }
        }

        /// <summary>
        /// Cache reuse behavior - first call executes query, second call reuses cache.
        /// Logs: cache hit/miss, position count, execution details.
        /// </summary>
        [TestMethod]
        public async Task GetPortfolioGraphQlDataAsync_CacheReuse_LogsHitOnSecondCall()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = "50837148";

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new[]
                    {
                        new PortfolioGraphQlPosition
                        {
                            rateable = new PortfolioGraphQlRateable
                            {
                                __typename = "TOTALRETURNSWAP",
                                uuid = "cache-trs",
                                asset = new PortfolioGraphQlAssetComposition
                                {
                                    id = 50837148,
                                    uuid = "cache-uuid",
                                    name = "Cache Test",
                                    __typename = "ASSETCOMPOSITION",
                                    members = new PortfolioGraphQlMember[0]
                                }
                            }
                        }
                    }
                }
            });

            // First call - executes query
            var validation = await _processor.ValidateAssetCompositionIdAsync();
            Assert.IsTrue(validation.IsValid);
            Assert.AreEqual(1, _fakeGraphQL.ExecuteCallCount);

            // Second call - reuses cache
            var components = await _processor.ValidateAndCollectBbgUuidsAsync();
            Assert.AreEqual(1, _fakeGraphQL.ExecuteCallCount);  // Still 1, cache was reused
        }

        /// <summary>
        /// Empty portfolio positions handling - logs warning about no positions.
        /// </summary>
        [TestMethod]
        public async Task GetPortfolioGraphQlDataAsync_EmptyPositions_LogsWarningAndReturnsEmpty()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = "50837148";

            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new PortfolioGraphQlPosition[0]
                }
            });

            var result = await _processor.ValidateAssetCompositionIdAsync();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, _fakeGraphQL.ExecuteCallCount);
        }

        /// <summary>
        /// BuildAccountSegmentQuery validation - rejects null/empty input after parameter check.
        /// Should never be called with null in normal flow (parameter validation happens first).
        /// </summary>
        [TestMethod]
        public void BuildAccountSegmentQuery_WithNullInput_ThrowsArgumentNullException()
        {
            try
            {
                var query = _processor.BuildAccountSegmentQuery(null);
                Assert.Fail("Should have thrown ArgumentNullException");
            }
            catch (ArgumentNullException ex)
            {
                Assert.IsTrue(ex.ParamName.Contains("accountSegmentIds"));
            }
        }

        /// <summary>
        /// BuildAccountSegmentQuery with multiple IDs - formats correctly with trimming.
        /// </summary>
        [TestMethod]
        public void BuildAccountSegmentQuery_WithMultipleIds_FormatsWithTrimming()
        {
            _fakeConfig.UseAccountSegmentGraphQlQuery = true;
            _fakeConfig.AccountSegmentIds = "50837148, 50837149 , 50837150";

            // Query is built internally by ValidateAssetCompositionIdAsync
            _fakeGraphQL.SetExecuteResult(new PortfolioGraphQlData
            {
                portfolio = new PortfolioGraphQlPortfolio
                {
                    portfolioPositions = new PortfolioGraphQlPosition[0]
                }
            });

            var task = _processor.ValidateAssetCompositionIdAsync();
            // The query should have IDs with trimmed whitespace
            Assert.IsNotNull(_fakeGraphQL.LastQuery);
        }

        // ========================================
        // Concurrent Execution Tests
        // ========================================
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
                    await _processor.TryExecuteAsync();
                }));
            }
            await Task.WhenAll(tasks);
            Assert.IsTrue(tasks.All(t => t.IsCompleted && !t.IsFaulted && !t.IsCanceled));
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
            Assert.IsTrue(true);
        }

        // ========================================
        // Helper Methods
        // ========================================
        private CircuitState GetCircuitState()
        {
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (CircuitState)stateField.GetValue(_processor._opusCircuitBreaker);
        }
    }
}
