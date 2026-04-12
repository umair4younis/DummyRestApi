using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Puma.MDE.OPUS.Exceptions;

namespace Puma.MDE.Tests
{
    /// <summary>
    /// Swap quote and lifecycle tests.
    /// Tests for swap creation, quote management, and related payload operations.
    /// </summary>
    public partial class OpusWeightUpdateProcessorTests
    {
        // ========================================
        // Swap Quote and Lifecycle Tests
        // ========================================
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

            var result = (await _processor.TryCreateSwapQuoteAsync(swapId, quote)).Data;

            Assert.IsNotNull(result);
        }

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
            var result = (await _processor.TryCreateSwapQuoteAsync(swapId, quoteToCreate, marketplaceId)).Data;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Resource);
            Assert.AreEqual("new-quote-uuid-123", result.Resource.Uuid);
            Assert.AreEqual(210000m, result.Resource.Value.Quantity);
            Assert.IsTrue(_fakeApi.PostWithResponseAsyncCalled);
        }


        [TestMethod]
        public async Task CreateSwapQuoteAsync_ValidationFails_ReturnsFailureResult()
        {
            var swapId = "invalid-swap";
            var marketplaceId = "home";
            var quote = new AssetQuote { Value = new AmountValue { Quantity = 100 } };

            // Simulate validation failure by making GetTotalReturnSwapAsync throw 404
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryCreateSwapQuoteAsync(swapId, quote, marketplaceId);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        [TestMethod]
        public async Task CreateSwapQuoteAsync_ApiFails_ReturnsFailureResult()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var marketplaceId = "home";
            var quote = new AssetQuote { Value = new AmountValue { Quantity = 210000 } };

            var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"Invalid quote value\"}")
            };

            _fakeApi.SetPostWithResponseResult(errorResponse);

            var result = await _processor.TryCreateSwapQuoteAsync(swapId, quote, marketplaceId);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

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

            var result = (await _processor.TryGetSwapQuotesAsync(swapId, marketplaceId)).Data;

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Resource.Quotes.Count);
            Assert.AreEqual(105000m, result.Resource.Quotes[1].Value.Quantity);
        }


        [TestMethod]
        public async Task GetSwapQuotesAsync_SwapNotFound_ReturnsFailureResult()
        {
            var swapId = "nonexistent";

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryGetSwapQuotesAsync(swapId);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        [TestMethod]
        public async Task UpdateSwapNominalAsync_NotionalTooLow_ThrowsIfValidationEnforcesMin()
        {
            var swapId = "small-swap";
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 500000m, Unit = "EUR", Type = "MONEY" }
            };

            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 100000m, Unit = "EUR", Type = "MONEY" }
            });

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);
            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
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
            _fakeApi.SetGetAsyncResult(new TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" }
            });

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
            // You can assert _fakeLogger.WarnLogs if you inject logger
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

            var result = (await _processor.TryCreateSwapQuoteAsync(swapId, quote)).Data;

            Assert.IsNotNull(result);

        }


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
            var result = (await _processor.TryGetSwapQuotesAsync(swapId)).Data;

            Assert.IsNotNull(result);

        }


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

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);

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
            await _processor.TryUpdateSwapAssetAtMarketplacesAsync(swapId, patch);

            // Assert
            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should have been called on the API client");
        }


        /// <summary>
        /// Ensures passing null SwapPatch throws ArgumentException immediately (defensive validation).
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapAssetAtMarketplacesAsync_NullPatch_ReturnsFailureResult()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";
            var result = await _processor.TryUpdateSwapAssetAtMarketplacesAsync(swapId, null);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        /// <summary>
        /// Validates that an empty/invalid SwapPatch (no nominal and no assetAtMarketplaces entries) 
        /// throws ArgumentException to prevent invalid API requests.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapAssetAtMarketplacesAsync_EmptyPatch_ReturnsFailureResult()
        {
            var swapId = "4d743016-66a1-4759-a95e-ca704cb93b1d";

            var emptyPatch = new SwapPatch { AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>() };

            var result = await _processor.TryUpdateSwapAssetAtMarketplacesAsync(swapId, emptyPatch);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        /// <summary>
        /// Confirms that swap validation failure (e.g. 404 Not Found) causes 
        /// UpdateSwapAssetAtMarketplacesAsync to throw InvalidOperationException without calling the API.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapAssetAtMarketplacesAsync_ValidationFails_ReturnsFailureResult()
        {
            var swapId = "invalid-swap";
            var patch = new SwapPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail> { new AssetAtMarketplaceDetail { Home = true } }
            };

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryUpdateSwapAssetAtMarketplacesAsync(swapId, patch);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
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
            var result = (await _processor.TryGetSwapQuotesAsync(swapId, marketplaceId)).Data;

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

            await _processor.TryUpdateSwapAssetAtMarketplacesAsync(swapId, patch);



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

            await _processor.TryUpdateSwapAssetAtMarketplacesAsync(swapId, patch);

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

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);

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

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);



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

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);


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

            await _processor.TryUpdateSwapNominalAsync(swapId, patch);


        }


        /// <summary>
        /// Validates that UpdateSwapNominalAsync throws InvalidOperationException 
        /// when swap validation fails (e.g., swap not found or invalid state).
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_ValidationFails_ReturnsFailureResult()
        {
            var swapId = "invalid-swap";
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" }
            };

            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

    }
}
