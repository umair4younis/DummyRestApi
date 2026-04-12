using Puma.MDE.OPUS;
using Puma.MDE.OPUS.Models;
using Puma.MDE.OPUS.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Puma.MDE.Test
{
    partial class Program
    {
        private static async Task<OpusOperationResult> OpusApiIntegration(bool opusEnabled, string opusAssetCompositionId)
        {
            const string IntegrationContextFailureMessage = "The OPUS integration flow stopped before finishing all update steps.";
            const string IntegrationContextSuccessMessage = "Process of updating Asset Compositions and Swaps is completed in OPUS API.";
            using (OpusMessageTrailContext.BeginScope())
            {
                if (!opusEnabled)
                {
                    string message = "OPUS integration is currently disabled.";
                    Engine.Instance.Log.Warn("[OpusApiIntegration] " + message);

                    OpusOperationResult disabledResult = OpusOperationResult.Failure(message, message);
                    OpusMessageTrailContext.PrefixCompletedBeforeTrail(disabledResult);
                    disabledResult.AddFriendlyContext(IntegrationContextFailureMessage);
                    return disabledResult;
                }

                if (string.IsNullOrWhiteSpace(opusAssetCompositionId))
                {
                    string message = "Asset composition id is missing. Please provide a valid id and try again.";
                    Engine.Instance.Log.Error("[OpusApiIntegration] Missing opusAssetCompositionId.");

                    OpusOperationResult missingAssetResult = OpusOperationResult.Failure(message, "Missing opusAssetCompositionId.");
                    OpusMessageTrailContext.PrefixCompletedBeforeTrail(missingAssetResult);
                    missingAssetResult.AddFriendlyContext(IntegrationContextFailureMessage);
                    return missingAssetResult;
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
                    Engine.Instance.Log.Error("[OpusApiIntegration] Required OPUS configuration values are missing.");

                    OpusOperationResult configurationResult = OpusOperationResult.Failure(FriendlyConfigurationErrorMessage, "Required OPUS configuration values are missing.");
                    OpusMessageTrailContext.PrefixCompletedBeforeTrail(configurationResult);
                    configurationResult.AddFriendlyContext(IntegrationContextFailureMessage);
                    return configurationResult;
                }

                Engine.Instance.Log.Info(opusConfiguration);

                OpusHttpClientHandler opusHttpClientHandler = new OpusHttpClientHandler(opusConfiguration);
                Engine.Instance.Log.Info(opusHttpClientHandler);

                OpusTokenProvider opusTokenProvider = new OpusTokenProvider(opusConfiguration);
                Engine.Instance.Log.Info(opusTokenProvider);

                OpusGraphQLClient opusGraphQLClient = new OpusGraphQLClient(opusHttpClientHandler, opusTokenProvider, opusConfiguration);
                Engine.Instance.Log.Info(opusGraphQLClient);

                OpusApiClient opusApiClient = new OpusApiClient(opusHttpClientHandler, opusTokenProvider, opusConfiguration);
                Engine.Instance.Log.Info(opusApiClient);

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
                        OpusMessageTrailContext.PrefixCompletedBeforeTrail(executeResult);
                        executeResult.AddFriendlyContext(IntegrationContextFailureMessage);
                        return executeResult;
                    }

                    var swapId = executeResult.Data;
                    if (string.IsNullOrWhiteSpace(swapId))
                    {
                        string message = "Unable to complete OPUS update because no valid swap was returned.";
                        Engine.Instance.Log.Error("[OpusApiIntegration] Processor returned empty swap id.");

                        OpusOperationResult missingSwapResult = OpusOperationResult.Failure(message, "Processor returned empty swap id.");
                        OpusMessageTrailContext.PrefixCompletedBeforeTrail(missingSwapResult);
                        missingSwapResult.AddFriendlyContext(IntegrationContextFailureMessage);
                        return missingSwapResult;
                    }

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
                        OpusMessageTrailContext.PrefixCompletedBeforeTrail(createQuoteResult);
                        createQuoteResult.AddFriendlyContext(IntegrationContextFailureMessage);
                        return createQuoteResult;
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
                        OpusMessageTrailContext.PrefixCompletedBeforeTrail(nominalUpdateResult);
                        nominalUpdateResult.AddFriendlyContext(IntegrationContextFailureMessage);
                        return nominalUpdateResult;
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

                    OpusOperationResult successResult = OpusOperationResult.Success("OPUS data update completed successfully.");
                    OpusMessageTrailContext.PrefixCompletedBeforeTrail(successResult);
                    successResult.AddFriendlyContext(IntegrationContextSuccessMessage);
                    return successResult;
                }
                catch (Exception ex)
                {
                    Engine.Instance.Log.Error($"[OpusApiIntegration] Integration failed: {ex}");

                    OpusOperationResult exceptionResult = OpusOperationResult.Failure(FriendlyUnexpectedErrorMessage, ex.Message);
                    OpusMessageTrailContext.PrefixCompletedBeforeTrail(exceptionResult);
                    exceptionResult.AddFriendlyContext(IntegrationContextFailureMessage);
                    return exceptionResult;
                }
            }
        }

    }
}
