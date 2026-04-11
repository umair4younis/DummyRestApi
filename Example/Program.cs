using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Puma.MDE
{
    partial class Program
    {
        private const string FriendlyUnexpectedErrorMessage = "Something went wrong while processing OPUS data. Please try again or contact support.";
        private const string FriendlyConfigurationErrorMessage = "OPUS configuration is missing or invalid. Please verify settings and try again.";

        internal static string LastUserFriendlyMessage { get; private set; } = string.Empty;

        static void Main(string[] args)
        {
            try
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[Program.Main] Unhandled exception: {ex}");
                LastUserFriendlyMessage = FriendlyUnexpectedErrorMessage;
                Console.Error.WriteLine(LastUserFriendlyMessage);
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
                LastUserFriendlyMessage = await OpusApiIntegration(true, parentAssetId).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(LastUserFriendlyMessage))
                {
                    Console.WriteLine(LastUserFriendlyMessage);
                }
            }

            Console.WriteLine("Process of updating OPUS Asset Compositions is completed.");
        }
    }
}

