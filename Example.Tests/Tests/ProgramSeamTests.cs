using Microsoft.VisualStudio.TestTools.UnitTesting;
using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Utilities;
using Puma.MDE.Test;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


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

        [TestMethod]
        public void FormatUserFriendlyMessages_JoinsWithEmptyLine_InOrder()
        {
            var messages = new List<string>
            {
                "First failure point message",
                "Caller context message",
                "Main method context message"
            };

            string formatted = Program.FormatUserFriendlyMessages(messages);

            string expected = "First failure point message"
                + Environment.NewLine + Environment.NewLine
                + "Caller context message"
                + Environment.NewLine + Environment.NewLine
                + "Main method context message";

            Assert.AreEqual(expected, formatted);
        }

        [TestMethod]
        public void SetLastUserFriendlyMessages_StoresListAndFormattedMessage()
        {
            Program.SetLastUserFriendlyMessages(new[]
            {
                "  Deepest failure message  ",
                "",
                "Main flow message"
            });

            Assert.AreEqual(2, Program.LastUserFriendlyMessages.Count);
            Assert.AreEqual("Deepest failure message", Program.LastUserFriendlyMessages[0]);
            Assert.AreEqual("Main flow message", Program.LastUserFriendlyMessages[1]);
            Assert.IsTrue(Program.LastUserFriendlyMessage.Contains(Environment.NewLine + Environment.NewLine));
        }

        [TestMethod]
        public async Task MainAsync_WithForcedIntegrationFailure_PreservesFailureTrailOrder()
        {
            var forcedResult = OpusOperationResult.Failure("Deepest failure point message", "Forced failure");
            forcedResult.AddFriendlyContext("Integration-level failure context message");

            Program.OpusApiIntegrationSeam = (enabled, assetId) => Task.FromResult(forcedResult);

            try
            {
                await Program.MainAsync(new string[0]);
            }
            finally
            {
                Program.ResetProgramSeamsForTests();
            }

            Assert.AreEqual(3, Program.LastUserFriendlyMessages.Count);
            Assert.AreEqual("Deepest failure point message", Program.LastUserFriendlyMessages[0]);
            Assert.AreEqual("Integration-level failure context message", Program.LastUserFriendlyMessages[1]);
            Assert.AreEqual("The process returned to the main workflow and stopped before completion.", Program.LastUserFriendlyMessages[2]);
        }

        [TestMethod]
        public async Task MainAsync_WithForcedIntegrationSuccess_PreservesSuccessTrailOrder()
        {
            var forcedResult = OpusOperationResult.Success("Deepest success message");
            forcedResult.AddFriendlyContext("Integration-level success context message");

            Program.OpusApiIntegrationSeam = (enabled, assetId) => Task.FromResult(forcedResult);

            try
            {
                await Program.MainAsync(new string[0]);
            }
            finally
            {
                Program.ResetProgramSeamsForTests();
            }

            Assert.AreEqual(3, Program.LastUserFriendlyMessages.Count);
            Assert.AreEqual("Deepest success message", Program.LastUserFriendlyMessages[0]);
            Assert.AreEqual("Integration-level success context message", Program.LastUserFriendlyMessages[1]);
            Assert.AreEqual("The process returned to the main workflow after successful completion.", Program.LastUserFriendlyMessages[2]);
        }

        [TestMethod]
        public async Task MainAsync_WithFailureContainingCompletedSteps_PlacesCompletedMessagesBeforeFailureTrail()
        {
            var forcedResult = OpusOperationResult.Failure("Deepest failure point message", "Forced failure");
            forcedResult.AddFriendlyContext("Completed step A message");
            forcedResult.AddFriendlyContext("Completed step B message");
            forcedResult.AddFriendlyContext("Integration-level failure context message");

            Program.OpusApiIntegrationSeam = (enabled, assetId) => Task.FromResult(forcedResult);

            try
            {
                await Program.MainAsync(new string[0]);
            }
            finally
            {
                Program.ResetProgramSeamsForTests();
            }

            Assert.AreEqual(5, Program.LastUserFriendlyMessages.Count);
            Assert.AreEqual("Completed step A message", Program.LastUserFriendlyMessages[0]);
            Assert.AreEqual("Completed step B message", Program.LastUserFriendlyMessages[1]);
            Assert.AreEqual("Deepest failure point message", Program.LastUserFriendlyMessages[2]);
            Assert.AreEqual("Integration-level failure context message", Program.LastUserFriendlyMessages[3]);
            Assert.AreEqual("The process returned to the main workflow and stopped before completion.", Program.LastUserFriendlyMessages[4]);
        }

        [TestMethod]
        public void PrefixCompletedBeforeTrail_PutsEndpointSuccessBeforeErrorTrail()
        {
            using (OpusMessageTrailContext.BeginScope())
            {
                OpusMessageTrailContext.AddCompletedEndpointSuccess("Asset composition endpoint call completed successfully.");

                var result = OpusOperationResult.Failure("Cannot update delta for swap 123: swap not found.", "swap not found");
                result.AddFriendlyContext("The OPUS integration flow stopped before finishing all update steps.");

                OpusMessageTrailContext.PrefixCompletedBeforeTrail(result);

                Assert.AreEqual(3, result.FriendlyMessages.Count);
                Assert.AreEqual("Asset composition endpoint call completed successfully.", result.FriendlyMessages[0]);
                Assert.AreEqual("Cannot update delta for swap 123: swap not found.", result.FriendlyMessages[1]);
                Assert.AreEqual("The OPUS integration flow stopped before finishing all update steps.", result.FriendlyMessages[2]);
            }
        }
    }
}
