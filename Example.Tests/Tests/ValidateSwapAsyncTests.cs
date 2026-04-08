using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Tests;


namespace Puma.MDE.Tests
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

            var circuitField = typeof(OpusWeightUpdateProcessor)
                .GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (circuitField == null)
            {
                Assert.Fail("Could not find _opusCircuitBreaker field in OpusWeightUpdateProcessor.");
            }

            circuitField.SetValue(_processor, _fakeCircuitBreaker);
        }

        [TestMethod]
        public async Task ValidateSwapAsync_ValidSwapWithRecentQuote_ReturnsSuccess()
        {
            var swapId = "valid-swap-123";
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 25000000m }
            });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(swapId, result.SwapId);
            Assert.AreEqual(25000000m, result.CurrentNotional);
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("Quote validation skipped")));
        }

        [TestMethod]
        public async Task ValidateSwapAsync_SwapNotFound_ReturnsFailure()
        {
            var swapId = "missing-swap-999";
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "404");
        }

        [TestMethod]
        public async Task ValidateSwapAsync_NotionalZero_DoesNotFailValidation()
        {
            var swapId = "zero-notional-swap";
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 0m }
            });

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "below minimum");
        }

        [TestMethod]
        public async Task ValidateSwapAsync_NotionalBelowMin_SetsInformationalMessage()
        {
            var swapId = "small-but-valid";
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 500000m }
            });

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "below minimum");
        }

        [TestMethod]
        public async Task ValidateSwapAsync_RequireQuote_AddsSkippedWarning()
        {
            var swapId = "no-quotes-swap";
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 5000000m }
            });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("Quote validation skipped")));
        }

        [TestMethod]
        public async Task ValidateSwapAsync_CircuitOpen_ReturnsFailureResult()
        {
            var swapId = "circuit-open-swap";
            _fakeApi.SetCircuitBreakerToThrow(new CircuitBreakerOpenException("Circuit is open"));

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "Validation failed");
            StringAssert.Contains(result.ErrorMessage, "Circuit is open");
        }

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
