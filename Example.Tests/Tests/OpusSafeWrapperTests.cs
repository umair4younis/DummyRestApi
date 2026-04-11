using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Test;
using Puma.MDE.OPUS.Tests;
using Puma.MDE.OPUS.Utilities;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusSafeWrapperTests
    {
        private sealed class ThrowingTokenProvider : OpusTokenProvider
        {
            public ThrowingTokenProvider(OpusConfiguration configuration)
                : base(configuration)
            {
            }

            public override Task<string> GetAccessTokenAsync()
            {
                throw new HttpRequestException("token service unavailable");
            }
        }

        [TestMethod]
        public async Task OpusApiClient_TryGetAsync_Failure_ReturnsFriendlyResult()
        {
            var config = new FakeOpusConfiguration();
            var tokenProvider = new FakeTokenProvider(config);
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            };
            var client = new OpusApiClient(null, tokenProvider, config)
            {
                _httpClient = new HttpClient(new FakeHttpMessageHandler(response))
            };

            var result = await client.TryGetAsync<dynamic>("/assets/999");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [TestMethod]
        public async Task OpusApiClient_TryPostWithResponseAsync_Success_ReturnsData()
        {
            var config = new FakeOpusConfiguration();
            var tokenProvider = new FakeTokenProvider(config);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}")
            };
            var client = new OpusApiClient(null, tokenProvider, config)
            {
                _httpClient = new HttpClient(new FakeHttpMessageHandler(response))
            };

            var result = await client.TryPostWithResponseAsync("/test", new { value = 1 });

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(HttpStatusCode.OK, result.Data.Item1.StatusCode);
        }

        [TestMethod]
        public async Task OpusGraphQLClient_TryExecuteAsync_GraphQlError_ReturnsFriendlyResult()
        {
            var config = new FakeOpusConfiguration();
            var tokenProvider = new FakeTokenProvider(config);
            var responseJson = JsonConvert.SerializeObject(new
            {
                errors = new[] { new { message = "bad graphql query" } }
            });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            };

            var client = new OpusGraphQLClient(new OpusHttpClientHandler(config), tokenProvider, config)
            {
                _httpClient = new HttpClient(new FakeHttpMessageHandler(response))
            };

            var result = await client.TryExecuteAsync<dynamic>("{ invalid }");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [TestMethod]
        public async Task OpusTokenProvider_TryGetAccessTokenAsync_Failure_ReturnsFriendlyResult()
        {
            var provider = new ThrowingTokenProvider(new FakeOpusConfiguration());

            var result = await provider.TryGetAccessTokenAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [TestMethod]
        public void JsonSerializerSettingsProvider_TryDeserialize_InvalidJson_ReturnsFriendlyResult()
        {
            var result = JsonSerializerSettingsProvider.TryDeserialize<AssetsQueryResponse>("{ invalid json }");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [TestMethod]
        public void PercentageHelper_ParsePercentageSafe_InvalidInput_ReturnsFriendlyResult()
        {
            var result = PercentageHelper.ParsePercentageSafe("not-a-percentage");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [TestMethod]
        public async Task HttpClientExtensions_TryPatchAsync_InvalidUri_ReturnsFriendlyResult()
        {
            using (var client = new HttpClient())
            {
                var result = await client.TryPatchAsync(" ", new { value = 1 });

                Assert.IsFalse(result.IsSuccess);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
            }
        }

        [TestMethod]
        public void OpusHttpClientHandler_TryCreate_NullConfig_ReturnsFriendlyResult()
        {
            var result = OpusHttpClientHandler.TryCreate(null);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.FriendlyMessage));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [TestMethod]
        public void OpusWeightUpdateProcessor_TryBuildBbgFilterQuery_NullInput_ReturnsSuccessWithSafeQuery()
        {
            var config = new FakeOpusConfiguration();
            var tokenProvider = new FakeTokenProvider(config);
            var graphQl = new FakeOpusGraphQLClient(null, tokenProvider, config);
            var apiClient = new FakeOpusApiClient();
            var processor = new OpusWeightUpdateProcessor(graphQl, apiClient);

            var result = processor.TryBuildBbgFilterQuery(null);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            StringAssert.Contains(result.Data, "id = -1");
        }
    }
}
