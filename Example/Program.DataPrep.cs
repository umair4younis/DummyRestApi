using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;

namespace Puma.MDE.Test
{
    partial class Program
    {

        internal static string BuildGraphQlAssetQuery(string parentAssetId)
        {
            return
                    "{\n" +
                    "  assets(range: {offset: 0, size: 1000} \n" +
                    "  filter: {\n" +
                    "    and: [\n" +
                    $"      {{ expression: \"id = {parentAssetId}\" }}\n" +
                    "    ]\n" +
                    "  }) \n" +
                    "  {\n" +
                    "    edges {\n" +
                    "      node {\n" +
                    "        id\n" +
                    "        name\n" +
                    "        uuid\n" +
                    "        __typename\n" +
                    "      }\n" +
                    "    }\n" +
                    "  }\n" +
                    "}";
        }

                internal static List<object[]> BuildDefaultFilteredPortfolioRows()
        {
            return new List<object[]>
            {
                new object[] { "STOXX EUROPE 600 UTILITIES NR", "SX6R Index", 1085.30, "5.03%" },
                new object[] { "DJ STOXX BANK RETURN", "SX7R Index", 2051.54, "5.87%" },
                new object[] { "STOXX EUROPE 600 HEALTH CARE NR", "SXDR Index", 1793.15, "10.53%" },
                new object[] { "CASH EUR", null, 3.15, "2.13%" },
                new object[] { "DJ STOXX FINANCIAL SERVICES RETURN", "SXFR Index", 784.29, "4.69%" },
                new object[] { "DJ STOXX INSURANCE RETURN", "SXIR Index", 1357.76, "4.65%" },
                new object[] { "DJ STOXX CONSTRUCTION & MAT. RETURN", "SXOR Index", 981.10, "4.83%" },
                new object[] { "STOXX EUROPE 600 PERS. & HOUSEHOLD G.", "SXQR Index", 789.76, "5.01%" },
                new object[] { "UC US CONSUMER STAPLES NR INDEX", "UCGRUCSN Index", 2113.34, "4.53%" },
                new object[] { "UC US FINANCIALS NR INDEX", "UCGRUFNN Index", 4116.80, "9.49%" },
                new object[] { "UC US HEALTH CARE NR INDEX", "UCGRUHCN Index", 4642.18, "10.64%" },
                new object[] { "UC US INDUSTRIALS NR INDEX", "UCGRUINN Index", 3980.36, "9.70%" },
                new object[] { "UC US INFORMATION TECH NR INDEX", "UCGRUITN Index", 9451.63, "25.18%" }
            };
        }

        internal static List<ReportHolding> BuildReportHoldings(IEnumerable<object[]> filteredPortfolioRows, string currency, string assetType)
        {
            var holdings = new List<ReportHolding>();

            if (filteredPortfolioRows == null)
            {
                Engine.Instance.Log.Warn("[BuildReportHoldings] Input portfolio rows are null.");
                return holdings;
            }

            foreach (var pr in filteredPortfolioRows)
            {
                if (pr == null)
                {
                    Engine.Instance.Log.Warn("[BuildReportHoldings] Encountered null portfolio row. Skipping.");
                    continue;
                }

                if (pr.Length < 4)
                {
                    Engine.Instance.Log.Warn("[BuildReportHoldings] Portfolio row has insufficient fields. Skipping.");
                    continue;
                }

                string bbgTicker = pr.GetValue(1)?.ToString();
                if (string.IsNullOrWhiteSpace(bbgTicker))
                {
                    string name = pr.GetValue(0)?.ToString() ?? "N/A";
                    Engine.Instance.Log.Warn("[BuildReportHoldings] Missing BBG ticker. Row skipped. Name: " + name);
                    continue;
                }

                string nominalRaw = pr.GetValue(2)?.ToString();
                string weightRaw = pr.GetValue(3)?.ToString();
                if (!PercentageHelper.TryParsePercentage(nominalRaw, out decimal nominal))
                {
                    Engine.Instance.Log.Warn("[BuildReportHoldings] Invalid nominal percentage. Row skipped. BBG: " + bbgTicker.Trim() + ", Value: " + (nominalRaw ?? "<null>"));
                    continue;
                }

                if (!PercentageHelper.TryParsePercentage(weightRaw, out decimal marketWeightPercent))
                {
                    Engine.Instance.Log.Warn("[BuildReportHoldings] Invalid market weight percentage. Row skipped. BBG: " + bbgTicker.Trim() + ", Value: " + (weightRaw ?? "<null>"));
                    continue;
                }

                holdings.Add(new ReportHolding
                {
                    Name = pr.GetValue(0)?.ToString(),
                    BbgTicker = bbgTicker.Trim(),
                    Nominal = nominal,
                    MarketWeightPercent = marketWeightPercent,
                    Currency = currency,
                    AssetType = ResolveAssetType(
                        pr,
                        pr.GetValue(0)?.ToString(),
                        bbgTicker,
                        assetType)
                });
            }

            return holdings;
        }

        private static string ResolveAssetType(object portfolioRow, string name, string bbgTicker, string defaultAssetType)
        {
            object instrument = GetPropertyValue(portfolioRow, "Instrument");
            bool isRealCash = GetPropertyValue<bool>(instrument, "IsRealCash");
            string instrumentBbg = GetPropertyValue<string>(instrument, "BBG");

            if (isRealCash)
            {
                return "Cash";
            }

            if (!string.IsNullOrWhiteSpace(instrumentBbg) && instrumentBbg.EndsWith(" Index", StringComparison.OrdinalIgnoreCase))
            {
                return "Index";
            }

            // Fallback path for array-based rows used by seam tests.
            if (!string.IsNullOrWhiteSpace(name) && name.IndexOf("CASH", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Cash";
            }

            if (!string.IsNullOrWhiteSpace(bbgTicker) && bbgTicker.Trim().EndsWith(" Index", StringComparison.OrdinalIgnoreCase))
            {
                return "Index";
            }

            return string.IsNullOrWhiteSpace(defaultAssetType)
                ? "Other"
                : defaultAssetType;
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            object value = GetPropertyValue(target, propertyName);
            return value is T typed ? typed : default(T);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            var property = target.GetType().GetProperty(propertyName);
            return property?.GetValue(target, null);
        }

        internal static Dictionary<string, string> BuildDefaultSwapValues()
        {
            return new Dictionary<string, string>
            {
                { "Swap Notional", "36,256,000.0000" },
                { "MtM", "4,347,097.1411" },
                { "MTM from Financing", "-0.0029%" },
                { "Swap Value", "11.9929%" }
            };
        }

        internal static void ApplySwapValues(IDictionary<string, string> swapValues)
        {
            if (swapValues == null)
            {
                Engine.Instance.Log.Warn("[ApplySwapValues] swapValues is null. Nothing to apply.");
                return;
            }

            foreach (var swapValue in swapValues)
            {
                if (swapValue.Key == null)
                {
                    Engine.Instance.Log.Warn("[ApplySwapValues] Encountered swap entry with null key. Skipping.");
                    continue;
                }

                string value = PercentageHelper.TryParsePercentage(swapValue.Value, out decimal weight)
                    ? PercentageHelper.ParsePercentage(swapValue.Value).ToString()
                    : swapValue.Value;

                if (!TryEncapculateSwapAccountValue(swapValue.Key, value))
                {
                    Engine.Instance.Log.Warn("[ApplySwapValues] Unknown swap value key: " + swapValue.Key);
                }
            }
        }

        internal static bool TryEncapculateSwapAccountValue(string propertyName, string propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                Engine.Instance.Log.Warn("[TryEncapculateSwapAccountValue] propertyName is null or empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                Engine.Instance.Log.Warn("[TryEncapculateSwapAccountValue] propertyValue is null or empty for key: " + propertyName);
                return false;
            }

            if (!decimal.TryParse(propertyValue, out decimal parsedValue))
            {
                Engine.Instance.Log.Warn("[TryEncapculateSwapAccountValue] Unable to parse decimal for key: " + propertyName + ", value: " + propertyValue);
                return false;
            }

            switch (propertyName)
            {
                case "Swap Notional":
                    OpusWeightUpdateProcessor.SwapNotional = parsedValue;
                    return true;
                case "MtM":
                    OpusWeightUpdateProcessor.Mtm = parsedValue;
                    return true;
                case "MTM from Financing":
                    OpusWeightUpdateProcessor.MtmFromFinancing = parsedValue;
                    return true;
                case "Swap Value":
                    OpusWeightUpdateProcessor.SwapValue = parsedValue;
                    return true;
                default:
                    return false;
            }
        }
    }
}
