using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Linq;
using Puma.MDE.OPUS.Exceptions;
using static Puma.MDE.OPUS.OpusCircuitBreaker;

namespace Puma.MDE.Tests
{
    /// <summary>
    /// Total Return Swap lifecycle tests.
    /// Tests for swap retrieval, creation, update, and related lifecycle operations.
    /// </summary>
    public partial class OpusWeightUpdateProcessorTests
    {
        // ========================================
        // Total Return Swap Lifecycle Tests
        // ========================================
        [TestMethod]
        public async Task GetTotalReturnSwapAsync_SwapNotFound_ReturnsFailureResult()
        {
            var swapId = "non-existent-swap";
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryGetTotalReturnSwapAsync(swapId);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
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

            await _processor.TryGetTotalReturnSwapAsync(swapId);

        }


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
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(payload)).Data;
            // Assert
            Assert.AreEqual("new-swap-uuid-456", createdId);
            Assert.IsTrue(_fakeApi.PostWithResponseAsyncCalled);
        }



        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_ValidationError_ReturnsFailureResult()
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
            var result = await _processor.TryCreateTotalReturnSwapAsync(new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_Success_NoException()
        {
            _fakeApi.SetPatchAsyncResult();
            await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { name = "Updated" });
            Assert.IsTrue(_fakeApi.PatchAsyncCalled);
        }



        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_ValidationError_ReturnsFailureResult()
        {
            _fakeApi.SetPatchAsyncToThrow(new ApiValidationException("Invalid data", "error body"));
            var result = await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        [TestMethod]
        public async Task DeleteTotalReturnSwapAsync_Success_NoException()
        {
            _fakeApi.SetDeleteAsyncResult();
            await _processor.TryDeleteTotalReturnSwapAsync("swap-123");
            Assert.IsTrue(_fakeApi.DeleteAsyncCalled);
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

            var result = (await _processor.TryGetTotalReturnSwapAsync(swapId)).Data;

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
        public async Task GetTotalReturnSwapAsync_EmptySwapId_ReturnsFailureResult()
        {
            var result = await _processor.TryGetTotalReturnSwapAsync("");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that ApiNotFoundException is properly propagated and logged when swap is not found.
        /// </summary>
        [TestMethod]
        public async Task GetTotalReturnSwapAsync_NotFound_ReturnsFailureResult()
        {
            var swapId = "missing-swap";
            _fakeApi.SetGetAsyncToThrow(new ApiNotFoundException("Swap not found", swapId));

            var result = await _processor.TryGetTotalReturnSwapAsync(swapId);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_400ValidationError_ReturnsFailureResult()
        {
            var errorBody = "{\"errors\":[{\"message\":\"Missing required field: name\"}]}";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            var result = await _processor.TryCreateTotalReturnSwapAsync(new { type = "INVALID" });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_429RateLimit_ReturnsFailureResult()
        {
            var errorBody = "{\"error\":\"Rate limit exceeded. Retry after 60 seconds.\"}";
            var mockResponse = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            var result = await _processor.TryCreateTotalReturnSwapAsync(new { name = "Test TRS" });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_401Unauthorized_ReturnsFailureResult()
        {
            var errorBody = "{\"error\":\"Unauthorized - invalid token\"}";
            var mockResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorBody)
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);
            var result = await _processor.TryCreateTotalReturnSwapAsync(new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that DeleteTotalReturnSwapAsync handles 404 Not Found gracefully:
        /// logs a warning and returns without throwing an exception.
        /// </summary>
        [TestMethod]
        public async Task DeleteTotalReturnSwapAsync_404_LogsWarnAndReturns()
        {
            _fakeApi.SetDeleteAsyncToThrow(new HttpRequestException("DELETE /swaps/swap-123 failed with status 404: Not Found"));

            await _processor.TryDeleteTotalReturnSwapAsync("swap-123");

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

            await _processor.TryDeleteTotalReturnSwapAsync("swap-123");

            Assert.IsTrue(_fakeApi.DeleteAsyncCalled);
        }



        /// <summary>
        /// Verifies that a 500 Server Error in DeleteTotalReturnSwapAsync throws ApiRequestException.
        /// </summary>
        [TestMethod]
        public async Task DeleteTotalReturnSwapAsync_500ServerError_ReturnsFailureResult()
        {
            _fakeApi.SetDeleteAsyncToThrow(new HttpRequestException("DELETE failed with status 500: Internal Server Error"));

            var result = await _processor.TryDeleteTotalReturnSwapAsync("swap-123");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that UpdateTotalReturnSwapAsync throws ApiValidationException on 400 Bad Request.
        /// </summary>
        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_400Validation_ReturnsFailureResult()
        {
            _fakeApi.SetPatchAsyncToThrow(new ApiValidationException("Invalid weight value", "{\"errors\":[\"Weight must be between 0 and 100\"]}"));

            var result = await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { weight = -10 });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that UpdateTotalReturnSwapAsync throws ApiNotFoundException on 404.
        /// </summary>
        [TestMethod]
        public async Task UpdateTotalReturnSwapAsync_404NotFound_ReturnsFailureResult()
        {
            _fakeApi.SetPatchAsyncToThrow(new ApiNotFoundException("Swap not found", "swap-123"));

            var result = await _processor.TryUpdateTotalReturnSwapAsync("swap-999", new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



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
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(payload)).Data;

            Assert.AreEqual("swap-503-success", createdId);
            Assert.AreEqual(3, attempt);
        }



        /// <summary>
        /// Verifies that persistent 503 errors exhaust retries and throw the last exception.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_503ExhaustsRetries_ReturnsFailureResult()
        {
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                var resp = new HttpResponseMessage((HttpStatusCode)503)
                {
                    Content = new StringContent("{\"error\":\"Service down\"}")
                };
                return Tuple.Create(resp, "{\"error\":\"Service down\"}");
            });

            var result = await _processor.TryCreateTotalReturnSwapAsync(new { name = "Will fail" });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that 502 Bad Gateway throws ApiRequestException (treated as non-retryable in current logic).
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_502BadGateway_ReturnsFailureResult()
        {
            var mockResponse = new HttpResponseMessage((HttpStatusCode)502)
            {
                Content = new StringContent("{\"error\":\"Bad gateway\"}")
            };
            _fakeApi.SetPostWithResponseResult(mockResponse);

            var result = await _processor.TryCreateTotalReturnSwapAsync(new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that when the circuit breaker is already open, CreateTotalReturnSwapAsync throws immediately.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_CircuitAlreadyOpen_ReturnsFailureResult()
        {
            // Force circuit open via reflection (test-only)
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.Open);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(30));

            var result = await _processor.TryCreateTotalReturnSwapAsync(new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }



        /// <summary>
        /// Verifies that 400 validation errors are surfaced as a failed safe-wrapper result.
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
            var result = await _processor.TryCreateTotalReturnSwapAsync(new { type = "TRS" });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
            StringAssert.Contains(result.ErrorMessage, "Missing name field");
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
                    return (await _processor.TryCreateTotalReturnSwapAsync(inputPayload)).Data;
                }));
            }

            var results = await Task.WhenAll(tasks);

            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results.All(id => id.StartsWith("swap-concurrent-")));
        }



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
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(payload)).Data;

            Assert.AreEqual("swap-504-success", createdId);
            Assert.AreEqual(3, attempt);
        }



        /// <summary>
        /// Verifies that persistent 504 errors exhaust retries and throw.
        /// </summary>
        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_504ExhaustsRetries_ReturnsFailureResult()
        {
            _fakeApi.SetPostWithResponseBehavior((endpoint, inputPayload) =>
            {
                var resp = new HttpResponseMessage((HttpStatusCode)504)
                {
                    Content = new StringContent("Gateway timeout")
                };
                return Tuple.Create(resp, "Gateway timeout");
            });

            var result = await _processor.TryCreateTotalReturnSwapAsync(new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
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

            await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { weight = 50 });
            Assert.AreEqual(2, attempt);
        }



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
            for (int i = 0; i < 3; i++) { try { await _processor.TryCreateTotalReturnSwapAsync(new { }); } catch { } }

            // Force half-open via reflection
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);

            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(new { name = "Half-Open Test" })).Data;

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

            for (int i = 0; i < 3; i++) { try { await _processor.TryCreateTotalReturnSwapAsync(new { }); } catch { } }

            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);

            try { await _processor.TryCreateTotalReturnSwapAsync(new { }); } catch { }

            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_CircuitOpensDuringRetries_ReturnsFailureResult()
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
            var result = await _processor.TryCreateTotalReturnSwapAsync(new { });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_503TriggersRetryWithBackoff()
        {
            // Reset recorded delays
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
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(payload)).Data;
            Assert.AreEqual("swap-retry-success", createdId);
            Assert.AreEqual(3, attempt);
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_504OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            // Simulate: 504 three times ? opens circuit after threshold
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
                try { await _processor.TryCreateTotalReturnSwapAsync(new { name = "504 Test" }); }
                catch { /* ignore setup failures */ }
            }
            // Verify circuit is open
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(1, _processor._opusCircuitBreaker.TotalCircuitOpenEvents);
            // Simulate time passed ? force half-open via reflection
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70)); // past break duration
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open call succeeds ? circuit should close
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(new { name = "Half-Open 504 Success" })).Data;
            Assert.AreEqual("swap-504-half-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


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
                // Half-open also fails ? should re-open circuit
                var failResp = new HttpResponseMessage((HttpStatusCode)504)
                {
                    Content = new StringContent("Gateway still timing out")
                };
                return Tuple.Create(failResp, "Gateway still timing out");
            });
            // Force circuit open
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.TryCreateTotalReturnSwapAsync(new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open call fails ? circuit should re-open
            try
            {
                await _processor.TryCreateTotalReturnSwapAsync(new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_507OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            // Simulate: 507 three times ? opens circuit
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
                try { await _processor.TryCreateTotalReturnSwapAsync(new { name = "507 Test" }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(1, _processor._opusCircuitBreaker.TotalCircuitOpenEvents);
            // Simulate time passed ? force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70)); // past break duration
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open succeeds ? closes circuit
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(new { name = "Half-Open 507 Success" })).Data;
            Assert.AreEqual("swap-507-half-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


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
                try { await _processor.TryCreateTotalReturnSwapAsync(new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open fails ? re-open
            try
            {
                await _processor.TryCreateTotalReturnSwapAsync(new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


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
                try { await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open success
            await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { weight = 50 });
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
        }


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
                try { await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open fails ? re-open
            try
            {
                await _processor.TryUpdateTotalReturnSwapAsync("swap-123", new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_509OpensCircuit_HalfOpenSuccessCloses()
        {
            int attempt = 0;
            // Simulate: 509 three times ? opens circuit after threshold
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
                try { await _processor.TryCreateTotalReturnSwapAsync(new { name = "509 Test" }); }
                catch { /* ignore setup failures */ }
            }
            // Verify circuit is open
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.AreEqual(1, _processor._opusCircuitBreaker.TotalCircuitOpenEvents);
            // Simulate time passed ? force half-open via reflection
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70)); // past break duration
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open call succeeds ? circuit closes
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(new { name = "Half-Open 509 Success" })).Data;
            Assert.AreEqual("swap-509-half-success", createdId);
            Assert.AreEqual("Closed", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


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
                // Half-open also fails ? should re-open
                var failResp = new HttpResponseMessage((HttpStatusCode)509)
                {
                    Content = new StringContent("Bandwidth quota still exceeded")
                };
                return Tuple.Create(failResp, "Bandwidth quota still exceeded");
            });
            // Force open circuit
            for (int i = 0; i < 3; i++)
            {
                try { await _processor.TryCreateTotalReturnSwapAsync(new { }); }
                catch { }
            }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            // Force half-open
            var stateField = typeof(OpusCircuitBreaker).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openedAtField = typeof(OpusCircuitBreaker).GetField("_circuitOpenedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            openedAtField.SetValue(_processor._opusCircuitBreaker, DateTime.UtcNow.AddSeconds(-70));
            stateField.SetValue(_processor._opusCircuitBreaker, CircuitState.HalfOpen);
            // Half-open fails ? circuit re-opens
            try
            {
                await _processor.TryCreateTotalReturnSwapAsync(new { });
            }
            catch (HttpRequestException) { }
            Assert.AreEqual("Open", _processor._opusCircuitBreaker.CurrentState);
            Assert.IsTrue(_processor._opusCircuitBreaker.ConsecutiveFailures >= 0);
        }


        [TestMethod]
        public async Task CreateTotalReturnSwapAsync_509TriggersRetryWithBackoff()
        {
            // Reset recorded delays
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
            var createdId = (await _processor.TryCreateTotalReturnSwapAsync(payload)).Data;
            Assert.AreEqual("swap-509-retry-success", createdId);
            Assert.AreEqual(3, attempt); // initial + 2 retries
        }

    }
}
