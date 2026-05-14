using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Puma.MDE.OPUS.Models;
using System.Threading.Tasks;


namespace Puma.MDE.Tests
{
    /// <summary>
    /// Tests covering the new mtmFromFinancing and swapValue fields on SwapNominalPatch
    /// and the corresponding PercentAmountValue model.
    /// </summary>
    public partial class OpusWeightUpdateProcessorTests
    {
        // ========================================
        // SwapNominalPatch field serialization tests
        // ========================================

        /// <summary>
        /// Verifies that a fully populated SwapNominalPatch serializes all three root fields
        /// to the exact JSON structure required by the OPUS API.
        /// </summary>
        [TestMethod]
        public void SwapNominalPatch_WithAllFields_SerializesToCorrectJson()
        {
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = new PercentAmountValue { Quantity = 10m, Unit = "%" },
                SwapValue = new PercentAmountValue { Quantity = 12.3m, Unit = "%" }
            };

            string json = JsonConvert.SerializeObject(patch, Formatting.Indented);
            var obj = JObject.Parse(json);

            Assert.IsTrue(obj.ContainsKey("nominal"), "Should contain 'nominal'");
            Assert.IsTrue(obj.ContainsKey("mtmFromFinancing"), "Should contain 'mtmFromFinancing'");
            Assert.IsTrue(obj.ContainsKey("swapValue"), "Should contain 'swapValue'");

            Assert.AreEqual(20000000m, (decimal)obj["nominal"]["quantity"], "nominal.quantity mismatch");
            Assert.AreEqual("EUR", (string)obj["nominal"]["unit"], "nominal.unit mismatch");
            Assert.AreEqual("MONEY", (string)obj["nominal"]["type"], "nominal.type mismatch");

            Assert.AreEqual(10m, (decimal)obj["mtmFromFinancing"]["quantity"], "mtmFromFinancing.quantity mismatch");
            Assert.AreEqual("%", (string)obj["mtmFromFinancing"]["unit"], "mtmFromFinancing.unit mismatch");

            Assert.AreEqual(12.3m, (decimal)obj["swapValue"]["quantity"], "swapValue.quantity mismatch");
            Assert.AreEqual("%", (string)obj["swapValue"]["unit"], "swapValue.unit mismatch");
        }

        /// <summary>
        /// Verifies that when MtmFromFinancing is null it is completely omitted from the serialized JSON.
        /// </summary>
        [TestMethod]
        public void SwapNominalPatch_MtmFromFinancingOmitted_JsonOmitsField()
        {
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = null,
                SwapValue = new PercentAmountValue { Quantity = 12.3m, Unit = "%" }
            };

            string json = JsonConvert.SerializeObject(patch);
            var obj = JObject.Parse(json);

            Assert.IsFalse(obj.ContainsKey("mtmFromFinancing"), "null mtmFromFinancing should be omitted from JSON");
            Assert.IsTrue(obj.ContainsKey("swapValue"), "swapValue should still be present");
        }

        /// <summary>
        /// Verifies that when SwapValue is null it is completely omitted from the serialized JSON.
        /// </summary>
        [TestMethod]
        public void SwapNominalPatch_SwapValueOmitted_JsonOmitsField()
        {
            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = new PercentAmountValue { Quantity = 10m, Unit = "%" },
                SwapValue = null
            };

            string json = JsonConvert.SerializeObject(patch);
            var obj = JObject.Parse(json);

            Assert.IsFalse(obj.ContainsKey("swapValue"), "null swapValue should be omitted from JSON");
            Assert.IsTrue(obj.ContainsKey("mtmFromFinancing"), "mtmFromFinancing should still be present");
        }

        // ========================================
        // PercentAmountValue model tests
        // ========================================

        /// <summary>
        /// Verifies that PercentAmountValue.FromPercent creates an instance with quantity set
        /// and unit defaulting to "%".
        /// </summary>
        [TestMethod]
        public void PercentAmountValue_FromPercent_SetsCorrectFields()
        {
            var pav = PercentAmountValue.FromPercent(42.5m);

            Assert.AreEqual(42.5m, pav.Quantity, "Quantity should match input value");
            Assert.AreEqual("%", pav.Unit, "Unit should default to '%'");
        }

        /// <summary>
        /// Verifies that PercentAmountValue serializes without a 'type' field, since the OPUS API
        /// schema for percentage amounts does not include 'type'.
        /// </summary>
        [TestMethod]
        public void PercentAmountValue_SerializesWithoutTypeField()
        {
            var pav = PercentAmountValue.FromPercent(10m);

            string json = JsonConvert.SerializeObject(pav);
            var obj = JObject.Parse(json);

            Assert.IsFalse(obj.ContainsKey("type"), "PercentAmountValue JSON must NOT contain 'type'");
            Assert.IsTrue(obj.ContainsKey("quantity"), "Should contain 'quantity'");
            Assert.IsTrue(obj.ContainsKey("unit"), "Should contain 'unit'");
        }

        /// <summary>
        /// Verifies that PercentAmountValue.ToString() produces a human-readable representation
        /// containing both quantity and unit.
        /// </summary>
        [TestMethod]
        public void PercentAmountValue_ToString_ContainsQuantityAndUnit()
        {
            var pav = PercentAmountValue.FromPercent(10m);
            string s = pav.ToString();

            StringAssert.Contains(s, "10", "ToString should contain quantity");
            StringAssert.Contains(s, "%", "ToString should contain unit");
        }

        // ========================================
        // UpdateSwapNominalAsync with new fields
        // ========================================

        /// <summary>
        /// Verifies that UpdateSwapNominalAsync succeeds and calls PatchAsync when both
        /// MtmFromFinancing and SwapValue are provided alongside Nominal.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_WithAllNewFields_PatchContainsAllFields()
        {
            var swapId = "all-fields-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = new PercentAmountValue { Quantity = 10m, Unit = "%" },
                SwapValue = new PercentAmountValue { Quantity = 12.3m, Unit = "%" }
            };

            _fakeApi.SetGetAsyncResult(new global::Puma.MDE.OPUS.Models.TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            });
            _fakeApi.SetPatchAsyncResult();

            var result = await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(result.IsSuccess, "Expected success result");
            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should be called");
        }

        /// <summary>
        /// Verifies that UpdateSwapNominalAsync succeeds when only MtmFromFinancing is provided
        /// (SwapValue omitted), and that PatchAsync is still called.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_WithMtmFromFinancingOnly_CallsPatch()
        {
            var swapId = "mtm-only-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = new PercentAmountValue { Quantity = 10m, Unit = "%" },
                SwapValue = null
            };

            _fakeApi.SetGetAsyncResult(new global::Puma.MDE.OPUS.Models.TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            });
            _fakeApi.SetPatchAsyncResult();

            var result = await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(result.IsSuccess, "Expected success with only MtmFromFinancing");
            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should be called");
        }

        /// <summary>
        /// Verifies that UpdateSwapNominalAsync succeeds when only SwapValue is provided
        /// (MtmFromFinancing omitted), and that PatchAsync is still called.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_WithSwapValueOnly_CallsPatch()
        {
            var swapId = "swapvalue-only-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = null,
                SwapValue = new PercentAmountValue { Quantity = 12.3m, Unit = "%" }
            };

            _fakeApi.SetGetAsyncResult(new global::Puma.MDE.OPUS.Models.TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            });
            _fakeApi.SetPatchAsyncResult();

            var result = await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsTrue(result.IsSuccess, "Expected success with only SwapValue");
            Assert.IsTrue(_fakeApi.PatchAsyncCalled, "PatchAsync should be called");
        }

        /// <summary>
        /// Verifies that UpdateSwapNominalAsync returns failure and does NOT call PatchAsync
        /// when MtmFromFinancing has a negative quantity.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_NegativeMtmFromFinancing_ReturnsFailure()
        {
            var swapId = "negative-mtm-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                MtmFromFinancing = new PercentAmountValue { Quantity = -5m, Unit = "%" }
            };

            _fakeApi.SetGetAsyncResult(new global::Puma.MDE.OPUS.Models.TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            });

            var result = await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsFalse(result.IsSuccess, "Expected failure for negative mtmFromFinancing");
            Assert.IsFalse(_fakeApi.PatchAsyncCalled, "PatchAsync should NOT be called for invalid input");
        }

        /// <summary>
        /// Verifies that UpdateSwapNominalAsync returns failure and does NOT call PatchAsync
        /// when SwapValue has a negative quantity.
        /// </summary>
        [TestMethod]
        public async Task UpdateSwapNominalAsync_NegativeSwapValue_ReturnsFailure()
        {
            var swapId = "negative-swapval-swap";

            var patch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = 20000000m, Unit = "EUR", Type = "MONEY" },
                SwapValue = new PercentAmountValue { Quantity = -1m, Unit = "%" }
            };

            _fakeApi.SetGetAsyncResult(new global::Puma.MDE.OPUS.Models.TotalReturnSwapResponse
            {
                Uuid = swapId,
                Nominal = new AmountValue { Quantity = 10000000m }
            });

            var result = await _processor.TryUpdateSwapNominalAsync(swapId, patch);

            Assert.IsFalse(result.IsSuccess, "Expected failure for negative swapValue");
            Assert.IsFalse(_fakeApi.PatchAsyncCalled, "PatchAsync should NOT be called for invalid input");
        }
    }
}
