using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS;
using System.Collections.Specialized;
using System.Reflection;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class CoverageConfigEdgeTests
    {
        [TestMethod]
        public void AppSettings_GetAs_NullConverter_UsesFallback()
        {
            int value = AppSettings.GetAs<int>("Opus.BaseUrl", null, 42);
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public void AppSettings_GetInt_InvalidNumber_UsesFallback()
        {
            int value = AppSettings.GetInt("Opus.BaseUrl", 77);
            Assert.AreEqual(77, value);
        }

        [TestMethod]
        public void AppSettings_GetBool_Covers_True_False_And_Invalid_Branches()
        {
            SetAppSettingForTest("bool_true_key", "yes");
            SetAppSettingForTest("bool_false_key", "off");
            SetAppSettingForTest("bool_invalid_key", "not-bool");

            Assert.IsTrue(AppSettings.GetBool("bool_true_key", false));
            Assert.IsFalse(AppSettings.GetBool("bool_false_key", true));
            Assert.IsTrue(AppSettings.GetBool("bool_invalid_key", true));
        }

        [TestMethod]
        public void OpusConfiguration_EdgeCases_Are_Covered()
        {
            var cfg = new OpusConfiguration();

            cfg.BaseUrl = "https://host/base/";
            Assert.AreEqual("https://host/base", cfg.BaseUrl);

            cfg.RestUrl = null;
            Assert.AreEqual("https://host/base", cfg.RestUrl);

            cfg.RestUrl = "https://other/rest/";
            Assert.AreEqual("https://other/rest", cfg.RestUrl);

            cfg.UnicreditSwapServiceUrl = "custom/swap/";
            Assert.AreEqual("https://host/base/custom/swap/", cfg.UnicreditSwapServiceUrl);

            cfg.UnicreditSwapServiceUrl = "https://svc/swap/api/";
            Assert.AreEqual("https://svc/swap/api", cfg.UnicreditSwapServiceUrl);

            cfg.GraphQlUrl = "graphql";
            Assert.AreEqual("https://host/base/graphql", cfg.GraphQlUrl);

            cfg.GraphQlUrl = "https://host/base/graph/path";
            Assert.AreEqual("https://host/base/graph/path", cfg.GraphQlUrl);

            Assert.AreEqual("https://host/base", cfg.GetBaseUrlForEndpoint("unicredit-swap-service/api/swaps"));
            Assert.AreEqual("https://other/rest", cfg.GetBaseUrlForEndpoint(null));
        }

        private static void SetAppSettingForTest(string key, string value)
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            MakeCollectionWritable(appSettings);
            appSettings[key] = value;
        }

        private static void MakeCollectionWritable(NameValueCollection collection)
        {
            var field = typeof(NameObjectCollectionBase).GetField("_readOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? typeof(NameObjectCollectionBase).GetField("readOnly", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(collection, false);
            }
        }
    }
}
