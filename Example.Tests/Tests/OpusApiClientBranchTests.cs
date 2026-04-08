using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Exceptions;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Test;
using Puma.MDE.OPUS.Tests;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class OpusApiClientBranchTests
    {
        private FakeOpusConfiguration _config;
        private FakeTokenProvider _tokenProvider;

        [TestInitialize]
        public void Init()
        {
            _config = new FakeOpusConfiguration();
            _tokenProvider = new FakeTokenProvider(_config);
        }

        [TestMethod]
        public async Task GetWithResponseAsync_SetsAuthHeader_AndBuildsFullUrl()
        {
            var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}")
            });

            var client = new OpusApiClient(null, _tokenProvider, _config)
            {
                _httpClient = new HttpClient(handler)
            };

            var tuple = await client.GetWithResponseAsync("/assets/123");

            Assert.AreEqual(HttpStatusCode.OK, tuple.Item1.StatusCode);
            Assert.AreEqual("https://fake-opus.url/assets/123", handler.LastRequest.RequestUri.ToString());
            Assert.IsNotNull(handler.LastRequest.Headers.Authorization);
            Assert.AreEqual("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
            Assert.AreEqual("fake-jwt-token-for-tests", handler.LastRequest.Headers.Authorization.Parameter);
        }

        [TestMethod]
        public async Task PostWithResponseAsync_429_ThrowsApiRateLimitException()
        {
            var handler = new FakeHttpMessageHandler(new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent("{\"error\":\"rate\"}")
            });

            var client = new OpusApiClient(null, _tokenProvider, _config)
            {
                _httpClient = new HttpClient(handler)
            };

            await AssertCompat.ThrowsAsync<ApiRateLimitException>(() =>
                client.PostWithResponseAsync("/swaps", new { name = "n" }));
        }

        [TestMethod]
        public async Task PostWithResponseAsync_500_ThrowsApiServerException()
        {
            var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"server\"}")
            });

            var client = new OpusApiClient(null, _tokenProvider, _config)
            {
                _httpClient = new HttpClient(handler)
            };

            await AssertCompat.ThrowsAsync<ApiServerException>(() =>
                client.PostWithResponseAsync("/swaps", new { name = "n" }));
        }

        [TestMethod]
        public async Task PatchAsync_WithParentAssetId_MapsExpectedHttpErrors()
        {
            var responses = new[]
            {
                new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("auth") },
                new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("missing") },
                new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("rate") },
                new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("server") },
                new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("conflict") }
            };

            var handler = new FakeHttpMessageHandler(responses);
            var client = new OpusApiClient(null, _tokenProvider, _config)
            {
                _httpClient = new HttpClient(handler)
            };

            await AssertCompat.ThrowsAsync<ApiRequestException>(() => client.PatchAsync("/p", new { }, "parent-1"));
            var notFound = await AssertCompat.ThrowsAsync<ApiRequestException>(() => client.PatchAsync("/p", new { }, "parent-2"));
            var rateLimit = await AssertCompat.ThrowsAsync<ApiRateLimitException>(() => client.PatchAsync("/p", new { }, "parent-3"));
            var server = await AssertCompat.ThrowsAsync<ApiRequestException>(() => client.PatchAsync("/p", new { }, "parent-4"));
            var other = await AssertCompat.ThrowsAsync<ApiRequestException>(() => client.PatchAsync("/p", new { }, "parent-5"));

            Assert.IsTrue(notFound.Message.Contains("parent-2"));
            Assert.IsTrue(server.Message.Contains("server"));
            Assert.IsTrue(other.Message.Contains("409"));
            Assert.IsNotNull(rateLimit.ResponseBody);
        }

        [TestMethod]
        public async Task PatchAsync_Standard_EncodesUrl_AndHonorsTimeout()
        {
            var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });

            var httpClient = new HttpClient(handler);
            var client = new OpusApiClient(null, _tokenProvider, _config)
            {
                _httpClient = httpClient
            };

            await client.PatchAsync("/assets/name with space", new { a = 1 }, encodeUrl: true, timeoutMs: 1234);

            string sentUrl = handler.LastRequest.RequestUri.ToString();
            Assert.IsTrue(sentUrl.Contains("name%20with%20space") || sentUrl.Contains("name with space"));
            Assert.AreEqual(TimeSpan.FromMilliseconds(1234), client._httpClient.Timeout);
        }

        [TestMethod]
        public async Task UpdateSwapDeltaAsync_UnexpectedStatus_ThrowsHttpRequestException()
        {
            var handler = new FakeHttpMessageHandler(new HttpResponseMessage((HttpStatusCode)418)
            {
                Content = new StringContent("teapot")
            });

            var client = new OpusApiClient(null, _tokenProvider, _config)
            {
                _httpClient = new HttpClient(handler)
            };

            var delta = new SwapDeltaUpdate
            {
                Members = new List<SwapDeltaMember>
                {
                    new SwapDeltaMember { AssetId = "a1", CurrentPieces = 1, CurrentWeight = 100m }
                }
            };

            await AssertCompat.ThrowsAsync<HttpRequestException>(() =>
                client.UpdateSwapDeltaAsync("/unicredit-swap-service/api/swaps/s1/delta", "s1", delta));
        }
    }
}
