using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Tests;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Puma.MDE.Tests
{
    [TestClass]
    public class CoverageOpusSupportTests
    {
        [TestMethod]
        public async Task OpusTokenProvider_Returns_Cached_Token_Without_Http_Call()
        {
            var provider = new OpusTokenProvider(new OpusConfiguration());

            typeof(OpusTokenProvider)
                .GetField("_token", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(provider, "cached-token");

            typeof(OpusTokenProvider)
                .GetField("_tokenExpiry", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(provider, DateTime.UtcNow.AddMinutes(5));

            string fromAsync = await provider.GetAccessTokenAsync();
            string fromAsyncAgain = await provider.GetAccessTokenAsync();

            Assert.AreEqual("cached-token", fromAsync);
            Assert.AreEqual("cached-token", fromAsyncAgain);
        }

        [TestMethod]
        public async Task OpusTokenProvider_With_No_TokenUrl_Throws_InvalidOperationException()
        {
            var provider = new OpusTokenProvider(new OpusConfiguration
            {
                ClientId = "id",
                ClientSecret = "secret"
            });

            await AssertCompat.ThrowsAsync<InvalidOperationException>(() => provider.GetAccessTokenAsync());
        }

        [TestMethod]
        public void AmountValue_Constructors_And_Factory_Helpers_Work()
        {
            var direct = new AmountValue(5m, "EUR", "MONEY");
            Assert.AreEqual(5m, direct.Quantity);
            Assert.AreEqual("EUR", direct.Unit);
            Assert.AreEqual("MONEY", direct.Type);

            var p = AmountValue.FromPercent(10m);
            var m = AmountValue.FromMoney(15m, "USD");
            var q = AmountValue.FromPricePerPiece(2m, "CHF");
            var pieces = AmountValue.FromPieces(7m);

            Assert.AreEqual("PERCENT", p.Type);
            Assert.AreEqual("%", p.Unit);
            Assert.AreEqual("MONEY", m.Type);
            Assert.AreEqual("USD", m.Unit);
            Assert.AreEqual("PRICE_PER_PIECE", q.Type);
            Assert.AreEqual("CHF/Pieces", q.Unit);
            Assert.AreEqual("PIECE", pieces.Type);
            Assert.AreEqual("Pieces", pieces.Unit);
        }

        [TestMethod]
        public async Task Fake_Opus_Test_Utilities_Basic_Behavior()
        {
            var logger = new FakeLogger();
            logger.Info("i");
            logger.Warn("w");
            logger.Error("e");
            Assert.AreEqual(1, logger.InfoLogs.Count);
            Assert.AreEqual(1, logger.WarnLogs.Count);
            Assert.AreEqual(1, logger.ErrorLogs.Count);

            var engine = new FakeEngine();
            Assert.IsNotNull(engine.Log);

            var fakeRetry = new FakeRetryPolicy();
            Assert.IsFalse(fakeRetry.IsRetryable(new Exception("x")));

            var fakeHandler = new FakeOpusHttpClientHandler();
            Assert.IsNotNull(fakeHandler._opusHttpClientHandler);
            Assert.IsFalse(fakeHandler._opusHttpClientHandler.UseProxy);

            var fakeBreaker = new FakeOpusCircuitBreaker();
            int value = await fakeBreaker.ExecuteAsync(async () =>
            {
                await Task.Yield();
                return 123;
            });

            int counter = 0;
            await fakeBreaker.ExecuteAsync(async () =>
            {
                await Task.Yield();
                counter++;
            });

            Assert.AreEqual(123, value);
            Assert.AreEqual(1, counter);
        }
    }
}
