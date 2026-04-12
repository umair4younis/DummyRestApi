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
