using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS;
using System;
using System.Collections.Generic;
using Puma.MDE.OPUS.Tests;

namespace Puma.MDE.Tests
{
    /// <summary>
    /// Shared base class for all OpusWeightUpdateProcessor tests.
    /// Contains Setup/TestInitialize and shared test fixtures.
    /// 
    /// Test organization by concern:
    /// - OpusWeightUpdateProcessorTests.Core.cs: Constructor, Execute, ExecuteWithRetry, Asset validation
    /// - OpusWeightUpdateProcessorTests.ValidationAndDelta.cs: Swap validation and delta operations
    /// - OpusWeightUpdateProcessorTests.SwapLifecycle.cs: Quote and payload operations
    /// - OpusWeightUpdateProcessorTests.TrsLifecycle.cs: Total Return Swap lifecycle
    /// - OpusWeightUpdateProcessorTests.DeltaFlows.cs: Delta fetch and update flows
    /// </summary>
    [TestClass]
    public partial class OpusWeightUpdateProcessorTests
    {
        protected OpusWeightUpdateProcessor _processor;
        protected OpusCircuitBreaker _fakeCircuitBreaker;
        protected FakeOpusGraphQLClient _fakeGraphQL;
        protected FakeOpusApiClient _fakeApi;
        protected FakeLogger _fakeLogger;
        protected List<int> _recordedDelays = new List<int>();

        [TestInitialize]
        public void Setup()
        {
            var fakeConfig = new FakeOpusConfiguration();
            var fakeTokenProvider = new FakeTokenProvider(fakeConfig);
            _fakeGraphQL = new FakeOpusGraphQLClient(null, fakeTokenProvider, fakeConfig);

            _fakeApi = new FakeOpusApiClient();
            _fakeCircuitBreaker = new OpusCircuitBreaker(
                failureThreshold: 3,
                breakSeconds: 2,
                maxRetries: 2,
                baseRetryDelayMs: 1,
                backoffFactor: 1.0,
                jitterMaxFactor: 0.0);
            _fakeLogger = new FakeLogger();

            // Default successful swap lookup used by validation-first flows.
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = "default-swap",
                Nominal = new AmountValue { Quantity = 2000000m, Unit = "EUR", Type = "MONEY" }
            });

            _processor = new OpusWeightUpdateProcessor(_fakeGraphQL, _fakeApi);

            var field = typeof(OpusWeightUpdateProcessor)
                .GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_processor, _fakeCircuitBreaker);
        }
    }
}
