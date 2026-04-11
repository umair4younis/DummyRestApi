using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS;
using System;
using System.Collections.Generic;


namespace Puma.MDE.Tests
{
    [TestClass]
    public class ProgramSeamTests
    {
        [TestMethod]
        public void BuildGraphQlAssetQuery_Includes_ParentAssetId()
        {
            string query = Program.BuildGraphQlAssetQuery("abc-123");

            Assert.IsTrue(query.Contains("id = abc-123"));
            Assert.IsTrue(query.Contains("assets(range"));
        }

        [TestMethod]
        public void BuildReportHoldings_MapsRows_AndParsesValues()
        {
            var rows = new List<object[]>
            {
                new object[] { "Name1", "BBG1", "10", "25%" },
                new object[] { "Name2", "BBG2", "20", "75%" }
            };

            var holdings = Program.BuildReportHoldings(rows, "EUR", "Index");

            Assert.AreEqual(2, holdings.Count);
            Assert.AreEqual("Name1", holdings[0].Name);
            Assert.AreEqual("BBG2", holdings[1].BbgTicker);
            Assert.AreEqual(0.1m, holdings[0].Nominal);
            Assert.AreEqual(0.75m, holdings[1].MarketWeightPercent);
            Assert.AreEqual("EUR", holdings[0].Currency);
            Assert.AreEqual("Index", holdings[1].AssetType);
        }

        [TestMethod]
        public void BuildDefaultRows_AndDefaultSwapValues_Are_NonEmpty()
        {
            var rows = Program.BuildDefaultFilteredPortfolioRows();
            var swaps = Program.BuildDefaultSwapValues();

            Assert.IsTrue(rows.Count > 0);
            Assert.IsTrue(swaps.Count > 0);
            Assert.IsTrue(swaps.ContainsKey("Swap Notional"));
        }

        [TestMethod]
        public void ApplySwapValues_Updates_Processor_Statics()
        {
            var values = new Dictionary<string, string>
            {
                { "Swap Notional", "1000" },
                { "MtM", "200" },
                { "MTM from Financing", "5%" },
                { "Swap Value", "10%" }
            };

            Program.ApplySwapValues(values);

            Assert.AreEqual(10m, OpusWeightUpdateProcessor.SwapNotional);
            Assert.AreEqual(2m, OpusWeightUpdateProcessor.Mtm);
            Assert.AreEqual(0.05m, OpusWeightUpdateProcessor.MtmFromFinancing);
            Assert.AreEqual(0.1m, OpusWeightUpdateProcessor.SwapValue);
        }

        [TestMethod]
        public void TryEncapculateSwapAccountValue_Handles_Known_And_Unknown_Keys()
        {
            bool known = Program.TryEncapculateSwapAccountValue("Swap Notional", "123");
            bool unknown = Program.TryEncapculateSwapAccountValue("Unknown", "1");

            Assert.IsTrue(known);
            Assert.IsFalse(unknown);
            Assert.AreEqual(123m, OpusWeightUpdateProcessor.SwapNotional);
        }

        [TestMethod]
        public void TryEncapculateSwapAccountValue_InvalidDecimal_ReturnsFalse()
        {
            bool result = Program.TryEncapculateSwapAccountValue("MtM", "not-a-number");

            Assert.IsFalse(result);
        }
    }
}
