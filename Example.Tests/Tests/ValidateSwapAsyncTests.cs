using Microsoft.VisualStudio.TestTools.UnitTesting;
using Example.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.OPUS.Exceptions;
using Example.OPUS;
using Example.OPUS.Tests;


namespace Example.Tests
{
    [TestClass]
    public class ValidateSwapAsyncTests
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

            // Robust injection - works whether the field is public or private
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
                Assert.Fail("Could not find _opusCircuitBreaker field in OpusWeightUpdateProcessor. " +
                            "Please check the field name and accessibility in the class.");
            }
        }

        // ------------------------------------------------------------------------
        // Happy path - valid swap with recent quote and good notional
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_ValidSwapWithRecentQuote_ReturnsSuccess()
        {
            // Arrange
            var swapId = "valid-swap-123";

            // Mock GET /swaps/{id}
            var swapData = new
            {
                status = "active",
                nominal = new { quantity = 25000000m }
            };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            // Mock GET quotes - recent quote exists
            var quotes = new QuoteGetResource
            {
                Quotes = new List<AssetQuote>
                {
                    new AssetQuote
                    {
                        Time = DateTime.UtcNow.AddHours(-12),
                        Value = new AmountValue { Quantity = 100000m }
                    }
                }
            };
            OpusApiResponse<QuoteGetResource> quotesResponse = new OpusApiResponse<QuoteGetResource> { Resource = quotes };
            _fakeApi.SetGetAsyncResult(quotesResponse);

            // Act
            var result = await _processor.ValidateSwapAsync(
                swapId,
                requireRecentQuote: true,
                minNotional: 1000000m);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(swapId, result.SwapId);
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(25000000m, result.CurrentNotional);
            Assert.AreEqual(1, result.QuoteCount);
            Assert.IsNotNull(result.LastQuoteTime);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        // ------------------------------------------------------------------------
        // Swap not found (404)
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_SwapNotFound_ReturnsFailure()
        {
            var swapId = "missing-swap-999";

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(swapId, result.SwapId);
            StringAssert.Contains(result.ErrorMessage, "not found");
            Assert.IsTrue(result.ErrorMessage.Contains("404") || result.ErrorMessage.Contains("not found"));
        }

        // ------------------------------------------------------------------------
        // Terminated / matured swap
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_TerminatedSwap_ReturnsFailure()
        {
            var swapId = "terminated-swap";

            var swapData = new { status = "terminated" };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "terminated");
        }

        // ------------------------------------------------------------------------
        // Notional too low or zero
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_NotionalZero_ReturnsFailure()
        {
            var swapId = "zero-notional-swap";

            var swapData = new { status = "active", nominal = new { quantity = 0m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "positive");
        }

        [TestMethod]
        public async Task ValidateSwapAsync_NotionalBelowMin_HasWarningButValid()
        {
            var swapId = "small-but-valid";

            var swapData = new { status = "active", nominal = new { quantity = 500000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.Warnings.Count);
            StringAssert.Contains(result.Warnings[0], "below minimum");
        }

        // ------------------------------------------------------------------------
        // No quotes when required
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_RequireQuote_NoQuotes_ReturnsFailure()
        {
            var swapId = "no-quotes-swap";

            var swapData = new { status = "active", nominal = new { quantity = 5000000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            // No quotes
            var quotesResp = new OpusApiResponse<QuoteGetResource> { Resource = new QuoteGetResource { Quotes = new List<AssetQuote>() } };
            _fakeApi.SetGetAsyncResult(quotesResp);

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "No quotes found");
        }

        // ------------------------------------------------------------------------
        // Old quotes (warning only)
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_OldQuote_GeneratesWarning()
        {
            var swapId = "old-quote-swap";

            var swapData = new { status = "active", nominal = new { quantity = 15000000m } };
            _fakeApi.SetGetAsyncResult<dynamic>(swapData);

            var oldQuoteTime = DateTime.UtcNow.AddDays(-5);
            var quotes = new QuoteGetResource
            {
                Quotes = new List<AssetQuote>
                {
                    new AssetQuote { Time = oldQuoteTime, Value = new AmountValue { Quantity = 100000 } }
                }
            };
            _fakeApi.SetGetAsyncResult(new OpusApiResponse<QuoteGetResource> { Resource = quotes });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.Warnings.Count);
            StringAssert.Contains(result.Warnings[0], "older than");
            Assert.AreEqual(oldQuoteTime, result.LastQuoteTime);
        }

        // ------------------------------------------------------------------------
        // Circuit breaker open during validation
        // ------------------------------------------------------------------------
        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task ValidateSwapAsync_CircuitOpen_Throws()
        {
            var swapId = "circuit-open-swap";

            _fakeApi.SetCircuitBreakerToThrow(new CircuitBreakerOpenException("Circuit is open"));

            await _processor.ValidateSwapAsync(swapId);
        }

        // ------------------------------------------------------------------------
        // Unexpected exception during fetch
        // ------------------------------------------------------------------------
        [TestMethod]
        public async Task ValidateSwapAsync_UnexpectedError_ReturnsFailure()
        {
            var swapId = "error-swap";

            _fakeApi.SetGetAsyncToThrow(new HttpRequestException("Connection timeout"));

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "Validation failed");
            StringAssert.Contains(result.ErrorMessage, "timeout");
        }
    }
}