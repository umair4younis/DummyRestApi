using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Puma.MDE.Test
{
    partial class Program
    {
        private const string FriendlyUnexpectedErrorMessage = "Something went wrong while processing OPUS data. Please try again or contact support.";
        private const string FriendlyConfigurationErrorMessage = "OPUS configuration is missing or invalid. Please verify settings and try again.";
        private const string MainContextFailureMessage = "The process returned to the main workflow and stopped before completion.";
        private const string MainContextSuccessMessage = "The process returned to the main workflow after successful completion.";

        internal static string LastUserFriendlyMessage { get; private set; } = string.Empty;
        internal static List<string> LastUserFriendlyMessages { get; private set; } = new List<string>();
        internal static Func<bool, string, Task<OpusOperationResult>> OpusApiIntegrationSeam { get; set; }

        static void Main(string[] args)
        {
            try
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[Program.Main] Unhandled exception: {ex}");
                SetLastUserFriendlyMessages(new List<string>
                {
                    FriendlyUnexpectedErrorMessage,
                    MainContextFailureMessage
                });

                Console.Error.WriteLine(LastUserFriendlyMessage);
            }
            finally
            {
                ShowUserFriendlyMessageBox();
                Console.ReadKey();
            }
        }

        internal static async Task MainAsync(string[] args)
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
                Func<bool, string, Task<OpusOperationResult>> integrationCall = OpusApiIntegrationSeam ?? OpusApiIntegration;
                OpusOperationResult integrationResult = await integrationCall(true, parentAssetId).ConfigureAwait(false);

                if (!integrationResult.IsSuccess)
                {
                    List<string> messageTrail = NormalizeFailureMessageOrdering(integrationResult.FriendlyMessages ?? new List<string>());
                    messageTrail.Add(MainContextFailureMessage);
                    SetLastUserFriendlyMessages(messageTrail);
                }
                else
                {
                    List<string> messageTrail = integrationResult.FriendlyMessages ?? new List<string>();
                    messageTrail.Add(MainContextSuccessMessage);
                    SetLastUserFriendlyMessages(messageTrail);
                }

                if (!string.IsNullOrWhiteSpace(LastUserFriendlyMessage))
                {
                    Console.WriteLine(LastUserFriendlyMessage);
                }
            }

            Console.WriteLine("Process of updating OPUS Asset Compositions is completed.");
        }

        internal static void ResetProgramSeamsForTests()
        {
            OpusApiIntegrationSeam = null;
        }

        internal static void SetLastUserFriendlyMessages(IEnumerable<string> messages)
        {
            LastUserFriendlyMessages = (messages ?? Enumerable.Empty<string>())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .ToList();

            LastUserFriendlyMessage = FormatUserFriendlyMessages(LastUserFriendlyMessages);
        }

        internal static string FormatUserFriendlyMessages(IEnumerable<string> messages)
        {
            var cleanedMessages = (messages ?? Enumerable.Empty<string>())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .ToArray();

            return string.Join(Environment.NewLine + Environment.NewLine, cleanedMessages);
        }

        private static List<string> NormalizeFailureMessageOrdering(IEnumerable<string> messages)
        {
            var ordered = (messages ?? Enumerable.Empty<string>())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .ToList();

            bool IsSuccessLike(string message)
            {
                string lower = message.ToLowerInvariant();
                return lower.Contains("success") || lower.Contains("completed") || lower.Contains("done");
            }

            bool IsFailureLike(string message)
            {
                string lower = message.ToLowerInvariant();
                return lower.Contains("fail")
                    || lower.Contains("error")
                    || lower.Contains("unable")
                    || lower.Contains("cannot")
                    || lower.Contains("invalid")
                    || lower.Contains("not found")
                    || lower.Contains("stopped")
                    || lower.Contains("problem");
            }

            List<string> successLike = ordered.Where(m => IsSuccessLike(m) && !IsFailureLike(m)).ToList();
            List<string> nonSuccess = ordered.Where(m => !(IsSuccessLike(m) && !IsFailureLike(m))).ToList();

            if (successLike.Count == 0)
            {
                return ordered;
            }

            successLike.AddRange(nonSuccess);
            return successLike;
        }

        private static void ShowUserFriendlyMessageBox()
        {
            if (string.IsNullOrWhiteSpace(LastUserFriendlyMessage))
            {
                return;
            }

            MessageBox.Show(
                LastUserFriendlyMessage,
                "OPUS Update Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
