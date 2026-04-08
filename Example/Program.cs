using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Puma.MDE
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static async Task MainAsync(string[] args)
        {
            string parentAssetId = "52288231";
            string query = BuildGraphQlAssetQuery(parentAssetId);

            var filteredPortfolioRows = BuildDefaultFilteredPortfolioRows();
            List<ReportHolding> holdings = BuildReportHoldings(filteredPortfolioRows, "EUR", "Index");
            OpusWeightUpdateProcessor.ReportHoldings = holdings;
            OpusWeightUpdateProcessor.Currency = "EUR";

            Dictionary<string, string> swapValues = BuildDefaultSwapValues();
            ApplySwapValues(swapValues);

            Console.WriteLine("Ready to call the update OPUS API");

            if (!string.IsNullOrEmpty(parentAssetId))
            {
                // FIX: Use await instead of .GetAwaiter().GetResult() to prevent deadlocks
                await OpusApiIntegration(true, parentAssetId);
            }

            Console.WriteLine("Process of updating OPUS Asset Compositions is completed.");
        }

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
            foreach (var pr in filteredPortfolioRows)
            {
                holdings.Add(new ReportHolding
                {
                    Name = pr.GetValue(0).ToString(),
                    BbgTicker = pr.GetValue(1).ToString(),
                    Nominal = PercentageHelper.ParsePercentage(pr.GetValue(2).ToString()),
                    MarketWeightPercent = PercentageHelper.ParsePercentage(pr.GetValue(3).ToString()),
                    Currency = currency,
                    AssetType = assetType
                });
            }

            return holdings;
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
            foreach (var swapValue in swapValues)
            {
                string value = PercentageHelper.TryParsePercentage(swapValue.Value, out decimal weight)
                    ? PercentageHelper.ParsePercentage(swapValue.Value).ToString()
                    : swapValue.Value;

                TryEncapculateSwapAccountValue(swapValue.Key, value);
            }
        }

        private static async Task OpusApiIntegration(bool opusEnabled, string opusAssetCompositionId)
        {
            Engine.Instance.Log.Info("Process of updating Asset Compositions and Swaps is started...");

            OpusConfiguration opusConfiguration = new OpusConfiguration
            {
                TokenUrl = AppSettings.Get("Opus.TokenUrl"),
                BaseUrl = AppSettings.Get("Opus.BaseUrl"),
                RestUrl = AppSettings.Get("Opus.RestUrl"),
                GraphQlUrl = AppSettings.Get("Opus.GraphQLUrl"),
                ProxyUrl = AppSettings.Get("Opus.ProxyUrl"),
                ClientId = AppSettings.Get("Opus.ClientId"),
                ClientSecret = AppSettings.Get("Opus.ClientSecret"),
                ClientCertPath = AppSettings.Get("Opus.ClientCertPath"),
                ClientCertPassword = AppSettings.Get("Opus.ClientCertPassword"),
                GraphQlQuery = AppSettings.Get("Opus.GraphQLQuery")
            };

            Engine.Instance.Log.Info(opusConfiguration);

            OpusHttpClientHandler opusHttpClientHandler = new OpusHttpClientHandler(opusConfiguration);
            Engine.Instance.Log.Info(opusHttpClientHandler);

            OpusTokenProvider opusTokenProvider = new OpusTokenProvider(opusConfiguration);
            Engine.Instance.Log.Info(opusTokenProvider);

            OpusGraphQLClient opusGraphQLClient = new OpusGraphQLClient(opusHttpClientHandler, opusTokenProvider, opusConfiguration);
            Engine.Instance.Log.Info(opusGraphQLClient);
            Console.WriteLine("Result of the call to OPUS Graph QL");
            Console.WriteLine(opusGraphQLClient);

            OpusApiClient opusApiClient = new OpusApiClient(opusHttpClientHandler, opusTokenProvider, opusConfiguration);
            Engine.Instance.Log.Info(opusApiClient);
            Console.WriteLine("Result of the call to OPUS API");
            Console.WriteLine(opusApiClient);

            OpusWeightUpdateProcessor.ParentAssetId = opusAssetCompositionId;
            Engine.Instance.Log.Info(OpusWeightUpdateProcessor.ParentAssetId);

            var processor = new OpusWeightUpdateProcessor(opusGraphQLClient, opusApiClient);
            Engine.Instance.Log.Info(processor);

            // FIX: Use await instead of .GetAwaiter().GetResult() to prevent deadlocks
            var swapId = await processor.ExecuteAsync();
            swapId = "019d2001-e11b-7000-a211-8c654386b53d";
            var quote = new AssetQuote
            {
                ClosingQuoteOfDay = false,
                LastQuoteOfDay = false,
                Time = DateTime.UtcNow,
                TimeZone = TimeZoneHelper.GetIanaTimeZone(),
                Value = new AmountValue { Quantity = OpusWeightUpdateProcessor.Mtm, Unit = $"{OpusWeightUpdateProcessor.Currency}/Pieces", Type = "PRICE_PER_PIECE" }
            };
            Engine.Instance.Log.Info(quote);

            // FIX: Use await instead of .GetAwaiter().GetResult() to prevent deadlocks
            var createdQuote = await processor.CreateSwapQuoteAsync(swapId, quote);
            Engine.Instance.Log.Info(createdQuote);

            var swapNominalPatch = new SwapNominalPatch
            {
                Nominal = new AmountValue { Quantity = OpusWeightUpdateProcessor.SwapNotional, Unit = $"{OpusWeightUpdateProcessor.Currency}", Type = "MONEY" }
            };
            Engine.Instance.Log.Info(swapNominalPatch);

            // FIX: Use await instead of .GetAwaiter().GetResult() to prevent deadlocks
            await processor.UpdateSwapNominalAsync(swapId, swapNominalPatch);

            Engine.Instance.Log.Info("Process of updating Asset Compositions and Swaps are completed in OPUS API.");

            var patch = new SwapPatch
            {
                Nominal = new AmountValue
                {
                    Quantity = OpusWeightUpdateProcessor.SwapNotional,
                    Unit = $"{OpusWeightUpdateProcessor.Currency}",
                    Type = "MONEY"
                },
                AssetAtMarketplaces = new List<AssetAtMarketplaceDetail>
                {
                    new AssetAtMarketplaceDetail
                    {
                        Home = true,
                        QuoteFactor = new AmountValue { Quantity = OpusWeightUpdateProcessor.SwapNotional, Type = "SCALAR" },
                        LotSize = new AmountValue { Quantity = OpusWeightUpdateProcessor.SwapNotional, Unit = "Pieces", Type = "PIECE" },
                        QuoteSource = "",
                        QuoteUnit = $"{OpusWeightUpdateProcessor.Currency}/Pieces",
                        Reference = true
                    }
                }
            };
        }

        internal static bool TryEncapculateSwapAccountValue(string propertyName, string propertyValue)
        {
            switch (propertyName)
            {
                case "Swap Notional":
                    OpusWeightUpdateProcessor.SwapNotional = decimal.Parse(propertyValue);
                    return true;
                case "MtM":
                    OpusWeightUpdateProcessor.Mtm = decimal.Parse(propertyValue);
                    return true;
                case "MTM from Financing":
                    OpusWeightUpdateProcessor.MtmFromFinancing = decimal.Parse(propertyValue);
                    return true;
                case "Swap Value":
                    OpusWeightUpdateProcessor.SwapValue = decimal.Parse(propertyValue);
                    return true;
                default:
                    return false;
            }
        }
    }
}
