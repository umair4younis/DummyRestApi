using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Puma.MDE
{
    partial class Program
    {
        private static async Task<string> OpusApiIntegration(bool opusEnabled, string opusAssetCompositionId)
        {
            string userFriendlyMessage = string.Empty;

            if (!opusEnabled)
            {
                userFriendlyMessage = "OPUS integration is currently disabled.";
                Engine.Instance.Log.Warn("[OpusApiIntegration] " + userFriendlyMessage);
                return userFriendlyMessage;
            }

            if (string.IsNullOrWhiteSpace(opusAssetCompositionId))
            {
                userFriendlyMessage = "Asset composition id is missing. Please provide a valid id and try again.";
                Engine.Instance.Log.Error("[OpusApiIntegration] Missing opusAssetCompositionId.");
                return userFriendlyMessage;
            }

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

            if (string.IsNullOrWhiteSpace(opusConfiguration.BaseUrl) ||
                string.IsNullOrWhiteSpace(opusConfiguration.RestUrl) ||
                string.IsNullOrWhiteSpace(opusConfiguration.GraphQlUrl) ||
                string.IsNullOrWhiteSpace(opusConfiguration.TokenUrl))
            {
                userFriendlyMessage = FriendlyConfigurationErrorMessage;
                Engine.Instance.Log.Error("[OpusApiIntegration] Required OPUS configuration values are missing.");
                return userFriendlyMessage;
            }

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

            try
            {
                OpusOperationResult<string> executeResult = await processor.TryExecuteAsync().ConfigureAwait(false);
                if (!executeResult.IsSuccess)
                {
                    Engine.Instance.Log.Error("[OpusApiIntegration] Execute failed: " + executeResult.ErrorMessage);
                    return executeResult.FriendlyMessage;
                }

                var swapId = executeResult.Data;
                if (string.IsNullOrWhiteSpace(swapId))
                {
                    userFriendlyMessage = "Unable to complete OPUS update because no valid swap was returned.";
                    Engine.Instance.Log.Error("[OpusApiIntegration] Processor returned empty swap id.");
                    return userFriendlyMessage;
                }

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

                OpusOperationResult<OpusApiResponse<AssetQuote>> createQuoteResult = await processor.TryCreateSwapQuoteAsync(swapId, quote).ConfigureAwait(false);
                if (!createQuoteResult.IsSuccess)
                {
                    Engine.Instance.Log.Error("[OpusApiIntegration] Create quote failed: " + createQuoteResult.ErrorMessage);
                    return createQuoteResult.FriendlyMessage;
                }

                var createdQuote = createQuoteResult.Data;
                Engine.Instance.Log.Info(createdQuote);

                var swapNominalPatch = new SwapNominalPatch
                {
                    Nominal = new AmountValue { Quantity = OpusWeightUpdateProcessor.SwapNotional, Unit = $"{OpusWeightUpdateProcessor.Currency}", Type = "MONEY" }
                };
                Engine.Instance.Log.Info(swapNominalPatch);

                OpusOperationResult nominalUpdateResult = await processor.TryUpdateSwapNominalAsync(swapId, swapNominalPatch).ConfigureAwait(false);
                if (!nominalUpdateResult.IsSuccess)
                {
                    Engine.Instance.Log.Error("[OpusApiIntegration] Update nominal failed: " + nominalUpdateResult.ErrorMessage);
                    return nominalUpdateResult.FriendlyMessage;
                }

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

                userFriendlyMessage = "OPUS data update completed successfully.";
                return userFriendlyMessage;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[OpusApiIntegration] Integration failed: {ex}");
                userFriendlyMessage = FriendlyUnexpectedErrorMessage;
                return userFriendlyMessage;
            }
        }

    }
}
