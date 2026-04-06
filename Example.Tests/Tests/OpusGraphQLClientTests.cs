using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Example.OPUS;
using Example.OPUS.Test;
using Example.OPUS.Tests;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Example.Tests
{
    [TestClass]
    public class OpusGraphQLClientTests
    {
        private OpusGraphQLClient _client;
        private FakeTokenProvider _tokenProvider;
        private FakeOpusConfiguration _config;
        private FakeOpusCircuitBreaker _circuitBreaker;

        [TestInitialize]
        public void Setup()
        {
            _config = new FakeOpusConfiguration();
            _tokenProvider = new FakeTokenProvider(_config);
            _circuitBreaker = new FakeOpusCircuitBreaker();
        }

        [TestMethod]
        public async Task ExecuteAsync_Success_ReturnsData()
        {
            var expectedData = new { test = "value" };

            var fullResponse = new
            {
                data = expectedData
                // no "errors" array
            };

            var responseJson = JsonConvert.SerializeObject(fullResponse);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };

            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            var fakeConfig = new FakeOpusConfiguration();

            _client = new OpusGraphQLClient(
                new OpusHttpClientHandler(fakeConfig),   // ← Use real handler with fake config
                _tokenProvider,
                _config)
            {
                _httpClient = httpClient,           // override with test double
                _opusCircuitBreaker = _circuitBreaker
            };

            var result = await _client.ExecuteAsync<dynamic>("{ test }");

            Assert.IsNotNull(result);
            Assert.AreEqual("value", (string)result.test);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task ExecuteAsync_GraphQLError_Throws()
        {
            // Arrange - GraphQL error response
            var errorResponse = new
            {
                errors = new[]
                {
                    new { message = "syntax error" }
                }
            };

            var responseJson = JsonConvert.SerializeObject(errorResponse);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };

            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);
            var fakeConfig = new FakeOpusConfiguration();

            _client = new OpusGraphQLClient(
                new OpusHttpClientHandler(fakeConfig),   // ← Use real handler with fake config
                _tokenProvider,
                _config)
            {
                _httpClient = httpClient,           // override with test double
                _opusCircuitBreaker = _circuitBreaker
            };

            // Act
            await _client.ExecuteAsync<dynamic>("invalid query");
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task ExecuteAsync_HttpFailure_Throws()
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service Unavailable")
            };

            var handler = new FakeHttpMessageHandler(response);
            var httpClient = new HttpClient(handler);

            _client = new OpusGraphQLClient(null, _tokenProvider, _config)
            {
                _httpClient = httpClient,
                _opusCircuitBreaker = _circuitBreaker   // ensure fake breaker is used (bypasses real retry logic)
            };

            await _client.ExecuteAsync<dynamic>("{ test }");
        }
    }
}

