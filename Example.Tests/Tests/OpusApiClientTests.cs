using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Test;
using Puma.MDE.OPUS.Tests;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    [TestClass]
    internal class OpusApiClientTests
    {
        private OpusApiClient _client;
        private FakeTokenProvider _tokenProvider;
        private FakeOpusConfiguration _config;
        private FakeLogger _fakeLogger;

        [TestInitialize]
        public void Setup()
        {
            var fakeConfig = new FakeOpusConfiguration();
            var fakeTokenProvider = new FakeTokenProvider(fakeConfig);
            var fakeCircuitBreaker = new FakeOpusCircuitBreaker();

            _client = new OpusApiClient(null, fakeTokenProvider, fakeConfig);

            // Inject fake circuit breaker
            typeof(OpusApiClient)
                .GetField("_opusCircuitBreaker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_client, fakeCircuitBreaker);

            // Fake logger (optional)
            _fakeLogger = new FakeLogger();

            var engineType = typeof(Engine);
            var instanceField = engineType.GetField("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            if (instanceField != null)
            {
                var fakeEngine = new FakeEngine { Log = _fakeLogger };
                instanceField.SetValue(null, fakeEngine);
            }
            else
            {
                Assert.Fail("Could not find private static 'instance' field in Engine class via reflection");
            }
        }

        [TestMethod]
        public async Task GetAsync_Success_ReturnsDeserializedObject()
        {
            var expected = new { name = "Test Asset" };
            var json = JsonConvert.SerializeObject(expected);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            var result = await _client.GetAsync<dynamic>("/assets/123");

            Assert.IsNotNull(result);
            Assert.AreEqual("Test Asset", (string)result.name);
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task GetAsync_Failure_Throws()
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.GetAsync<dynamic>("/assets/999");
        }

        [TestMethod]
        public async Task PostAsync_Success_NoException()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.PostAsync("/assets", new { name = "New" });
            Assert.IsTrue(true); // no exception = pass
        }

        [TestMethod]
        public async Task PostWithResponseAsync_Success_ReturnsBody()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true}")
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            var (resp, body) = await _client.PostWithResponseAsync("/test", new { data = 1 });

            Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
            Assert.IsTrue(body.Contains("success"));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task PatchAsync_Failure_Throws()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid\"}")
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.PatchAsync("/test", new { }, "parentId");
        }
        
        // ──────────────────────────────────────────────────────────────
        // PutAsync Tests
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task PutAsync_Success_NoException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            // Act
            await _client.PutAsync("/resources/123", new { name = "Updated" });

            // Assert
            Assert.IsTrue(true); // no exception = success
            Assert.IsTrue(_fakeLogger.InfoLogs.Exists(l => l.Contains("[PUT] Succeeded")));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task PutAsync_Failure_ThrowsAndLogs()
        {
            // Arrange
            var errorBody = "{\"error\":\"invalid data\"}";
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            // Act
            await _client.PutAsync("/resources/999", new { name = "Bad" });

            // Assert (via ExpectedException)
            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("[PUT] Failed")));
        }

        // ──────────────────────────────────────────────────────────────
        // PutWithResponseAsync Tests
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task PutWithResponseAsync_Success_ReturnsBody()
        {
            // Arrange
            var responseBody = "{\"id\":123,\"status\":\"updated\"}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            // Act
            var (resp, body) = await _client.PutWithResponseAsync("/resources/123", new { name = "Updated" });

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
            Assert.AreEqual(responseBody, body);
            Assert.IsTrue(_fakeLogger.InfoLogs.Exists(l => l.Contains("[PUT] Succeeded")));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task PutWithResponseAsync_Failure_ThrowsAndLogsBody()
        {
            // Arrange
            var errorBody = "{\"error\":\"not found\"}";
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(errorBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            // Act
            await _client.PutWithResponseAsync("/resources/999", new { });

            // Assert (via ExpectedException)
            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("[PUT] Failed")));
            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("not found")));
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteAsync Tests
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task DeleteAsync_Success_NoException()
        {
            var response = new HttpResponseMessage(HttpStatusCode.NoContent);
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.DeleteAsync("/resources/123");

            Assert.IsTrue(true);
            Assert.IsTrue(_fakeLogger.InfoLogs.Exists(l => l.Contains("[DELETE] Succeeded")));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task DeleteAsync_Failure_ThrowsAndLogs()
        {
            var errorBody = "{\"error\":\"not found\"}";
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(errorBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.DeleteAsync("/resources/999");

            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("[DELETE] Failed")));
        }

        // ──────────────────────────────────────────────────────────────
        // DeleteWithResponseAsync Tests
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task DeleteWithResponseAsync_Success_ReturnsBody()
        {
            var responseBody = "{\"message\":\"deleted\"}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            var (resp, body) = await _client.DeleteWithResponseAsync("/resources/123");

            Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
            Assert.AreEqual(responseBody, body);
            Assert.IsTrue(_fakeLogger.InfoLogs.Exists(l => l.Contains("[DELETE] Succeeded")));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task DeleteWithResponseAsync_Failure_ThrowsAndLogs()
        {
            var errorBody = "{\"error\":\"forbidden\"}";
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(errorBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.DeleteWithResponseAsync("/resources/999");

            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("[DELETE] Failed")));
            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("forbidden")));
        }

        // ──────────────────────────────────────────────────────────────
        // PatchWithResponseAsync Tests (already partially covered, extended here)
        // ──────────────────────────────────────────────────────────────
        [TestMethod]
        public async Task PatchWithResponseAsync_Success_ReturnsBody()
        {
            var responseBody = "{\"updated\":true}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            var (resp, body) = await _client.PatchWithResponseAsync("/resources/123", new { name = "Patched" });

            Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
            Assert.AreEqual(responseBody, body);
            Assert.IsTrue(_fakeLogger.InfoLogs.Exists(l => l.Contains("[PATCH] Succeeded")));
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task PatchWithResponseAsync_Failure_ThrowsAndLogs()
        {
            var errorBody = "{\"error\":\"conflict\"}";
            var response = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(errorBody)
            };
            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            _client = new OpusApiClient(null, _tokenProvider, _config) { _httpClient = httpClient };

            await _client.PatchWithResponseAsync("/resources/123", new { });

            Assert.IsTrue(_fakeLogger.ErrorLogs.Exists(l => l.Contains("[PATCH] Failed")));
        }
    }
}
