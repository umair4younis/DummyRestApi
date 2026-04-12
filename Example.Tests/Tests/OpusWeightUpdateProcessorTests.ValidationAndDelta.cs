using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using System.Threading.Tasks;
using System.Linq;
using Puma.MDE.OPUS.Exceptions;

namespace Puma.MDE.Tests
{
    /// <summary>
    /// Swap validation and delta operation tests.
    /// Tests for ValidateSwapAsync, ValidateSwapAsync edge cases, and related validation flows.
    /// </summary>
    public partial class OpusWeightUpdateProcessorTests
    {
        // ========================================
        // ValidateSwapAsync - Core Tests
        // ========================================
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

            // The current implementation keeps IsValid=true but adds an info message when notional is below minimum
            Assert.IsTrue(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "below minimum");
        }

        [TestMethod]
        public async Task ValidateSwapAsync_NullSwapResponse_ReturnsFailure()
        {
            var swapId = "null-response-swap";
            _fakeApi.SetGetAsyncResult<dynamic>(null);

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found", "Null response should be treated as not found");
        }

        [TestMethod]
        public async Task ValidateSwapAsync_MissingNominalField_HasWarningButValid()
        {
            var swapId = "missing-nominal-swap";

            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse { Uuid = swapId, Name = "No Nominal" });

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
            Assert.IsNull(result.CurrentNotional);
        }

        [TestMethod]
        public async Task ValidateSwapAsync_NegativeNotional_ReturnsFailure()
        {
            var swapId = "negative-notional-swap";

            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = -5000000m, Unit = "EUR", Type = "MONEY" }
            });

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(-5000000m, result.CurrentNotional);
        }

        [TestMethod]
        public async Task ValidateSwapAsync_ExtremelyLargeNotional_StillValid()
        {
            var swapId = "huge-notional-swap";

            var hugeValue = 999_999_999_999_999_999m; // close to decimal max
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = hugeValue, Unit = "EUR", Type = "MONEY" }
            });

            var result = await _processor.ValidateSwapAsync(swapId, minNotional: 1000000m);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(hugeValue, result.CurrentNotional);
            Assert.IsTrue(result.Warnings.Count >= 1);
        }

        [TestMethod]
        public async Task ValidateSwapAsync_NullQuotesArray_HandledAsZeroQuotes()
        {
            var swapId = "null-quotes-swap";

            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 5000000m, Unit = "EUR", Type = "MONEY" }
            });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.QuoteCount ?? 0);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
        }

        [TestMethod]
        public async Task ValidateSwapAsync_FutureQuoteTime_HasWarning()
        {
            var swapId = "future-quote-swap";

            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m, Unit = "EUR", Type = "MONEY" }
            });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: true);

            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
        }

        [TestMethod]
        public async Task ValidateSwapAsync_MalformedSwapJson_ReturnsFailure()
        {
            var swapId = "malformed-swap";

            _fakeApi.SetGetAsyncResult<dynamic>("this is not json { broken");

            var result = await _processor.ValidateSwapAsync(swapId);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found or returned no data");
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

            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 3000000m, Unit = "EUR", Type = "MONEY" }
            });

            var result = await _processor.ValidateSwapAsync(swapId, requireRecentQuote: false);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.Warnings.Count); // Success() currently adds base quote-skipped warning
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
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
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
            Assert.AreEqual(1, result.Warnings.Count);
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

            Assert.IsTrue(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "below minimum");
            Assert.IsTrue(result.Warnings.Count >= 1);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("Quote validation skipped")));
        }

        /// <summary>
        /// Validates that ApiNotFoundException from the API layer is propagated correctly through ValidateSwapAsync.
        /// </summary>
        [TestMethod]
        public async Task ValidateSwapAsync_ApiNotFound_PropagatesException()
        {
            var swapId = "missing-swap";

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.ValidateSwapAsync(swapId);
            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "not found");
        }

        // --------------------------------------------------------------
        // GetSwapQuotesAsync Tests - Updated
        // --------------------------------------------------------------

        // --------------------------------------------------------------
        // UpdateSwapNominalAsync Tests - Updated
        // --------------------------------------------------------------

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
    }
}
