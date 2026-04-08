using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Tests;
using Puma.MDE.OPUS.Utilities;
using System;
using System.Configuration;

namespace Puma.MDE.Tests
{
    [TestClass]
    public class CoverageHelpersTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppSettings_Get_NullKey_Throws()
        {
            AppSettings.Get(null);
        }

        [TestMethod]
        public void AppSettings_Fallback_Methods_Work_ForMissingKeys()
        {
            string key = "missing_" + Guid.NewGuid().ToString("N");

            Assert.AreEqual("fallback", AppSettings.Get(key, "fallback"));
            Assert.AreEqual(123, AppSettings.GetInt(key, 123));
            Assert.IsTrue(AppSettings.GetBool(key, true));
            Assert.AreEqual(77, AppSettings.GetAs(key, s => int.Parse(s), 77));
            Assert.AreEqual(55, AppSettings.GetAppSettingInt(key, 55));
        }

        [TestMethod]
        [ExpectedException(typeof(ConfigurationErrorsException))]
        public void AppSettings_GetRequired_Missing_Throws()
        {
            string key = "missing_required_" + Guid.NewGuid().ToString("N");
            AppSettings.GetRequired(key);
        }

        [TestMethod]
        public void PercentageHelper_Parse_And_TryParse_Work()
        {
            Assert.IsTrue(PercentageHelper.TryParsePercentage("4.69%", out decimal result));
            Assert.AreEqual(0.0469m, result);
            Assert.AreEqual(0.1m, PercentageHelper.ParsePercentage("10"));
            Assert.IsFalse(PercentageHelper.TryParsePercentage("bad", out _));
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void PercentageHelper_Parse_Invalid_Throws()
        {
            PercentageHelper.ParsePercentage("bad");
        }

        [TestMethod]
        public void OpusConfiguration_Combines_Urls_AsExpected()
        {
            var cfg = new OpusConfiguration
            {
                BaseUrl = "https://example.com/"
            };

            Assert.AreEqual("https://example.com/v3/masterdata/", cfg.RestUrl);
            Assert.AreEqual("https://example.com/unicredit-swap-service/api/", cfg.UnicreditSwapServiceUrl);

            cfg.RestUrl = "custom/path";
            Assert.AreEqual("https://example.com/custom/path/", cfg.RestUrl);

            cfg.GraphQlUrl = "https://example.com/graphql";
            Assert.AreEqual("https://example.com/graphql", cfg.GraphQlUrl);
            Assert.AreEqual("https://example.com/custom/path/", cfg.GetBaseUrlForEndpoint("swaps"));
            Assert.AreEqual("https://example.com", cfg.GetBaseUrlForEndpoint("unicredit-swap-service/api/swaps"));
        }

        [TestMethod]
        public void JsonSerializerSettingsProvider_Serializes_And_Deserializes()
        {
            var payload = new { A = 1, B = (string)null };
            string json = JsonSerializerSettingsProvider.Serialize(payload);

            Assert.IsTrue(json.Contains("\"A\":1"));
            Assert.IsFalse(json.Contains("\"B\""));

            var dto = JsonSerializerSettingsProvider.Deserialize<TestDto>("{\"A\":5}");
            Assert.AreEqual(5, dto.A);
            Assert.IsNull(dto.B);
            Assert.IsNotNull(JsonSerializerSettingsProvider.Settings);
        }

        [TestMethod]
        public void TimeZoneHelper_Returns_NonEmpty_Iana()
        {
            string tz = TimeZoneHelper.GetIanaTimeZone();
            Assert.IsFalse(string.IsNullOrWhiteSpace(tz));
        }

        [TestMethod]
        public void OpisIsoDateTimeConverter_Read_Write_And_CanConvert_Coverage()
        {
            var converter = new OpisIsoDateTimeConverter();
            Assert.IsTrue(converter.CanConvert(typeof(DateTime)));
            Assert.IsTrue(converter.CanConvert(typeof(DateTime?)));
            Assert.IsFalse(converter.CanConvert(typeof(string)));

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);

            DateTime dt = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
            string json = JsonConvert.SerializeObject(dt, settings);
            Assert.IsTrue(json.Contains("2024-01-02T03:04:05.678Z"));

            DateTime parsed = JsonConvert.DeserializeObject<DateTime>("\"2024-01-02T03:04:05.678Z\"", settings);
            Assert.AreEqual(2024, parsed.Year);
            Assert.AreEqual(1, parsed.Month);

            DateTime? parsedNull = JsonConvert.DeserializeObject<DateTime?>("null", settings);
            Assert.IsNull(parsedNull);
        }

        [TestMethod]
        public void OpusHttpClientHandler_Guard_Paths_Are_Covered()
        {
            AssertCompat.Throws<ArgumentNullException>(() => new OpusHttpClientHandler(null));

            var missingCert = new OpusConfiguration
            {
                ProxyUrl = "http://proxy.local",
                ClientCertPath = "",
                ClientCertPassword = "pwd"
            };
            AssertCompat.Throws<ArgumentException>(() => new OpusHttpClientHandler(missingCert));

            var fakeCfg = new FakeOpusConfiguration
            {
                SkipCertificateLoadingForTests = true
            };
            var handler = new OpusHttpClientHandler(fakeCfg);
            Assert.IsNotNull(handler._opusHttpClientHandler);
            Assert.IsTrue(handler._opusHttpClientHandler.UseProxy);
        }

        [TestMethod]
        public void Engine_And_NLogConfigGuard_Basic_Coverage()
        {
            var engine = Engine.Instance;

            Assert.IsFalse(string.IsNullOrWhiteSpace(engine.Application));

            DateTime today = DateTime.Today;
            engine.Today = today;
            Assert.AreEqual(today, engine.Today);

            Assert.IsFalse(string.IsNullOrWhiteSpace(engine.GetHistomvtsName()));

            engine.LogDebugArray("d", new double[] { 1.0, 2.0 });
            engine.LogDebugArray("i", new int[] { 1, 2 });
            engine.LogDebugArray("l", new long[] { 1L, 2L });
            engine.LogDebugValue("k", "v");
            engine.InfoException("i", new Exception("x"));
            engine.WarnException("w", new Exception("x"));
            engine.ErrorException("e", new Exception("x"));
            engine.FatalException("f", new Exception("x"));
            engine.DebugException("d", new Exception("x"));
            engine.LogError("le", new Exception("x"));

            using (var guard = new NLogConfigGuard())
            {
                guard.Dispose();
            }
        }

        public class TestDto
        {
            public int A { get; set; }
            public string B { get; set; }
        }
    }
}
