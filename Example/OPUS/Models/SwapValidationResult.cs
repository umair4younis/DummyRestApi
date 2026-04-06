using System;
using System.Collections.Generic;


namespace Example.OPUS.Models
{
    /// <summary>
    /// Result of swap validation containing success/failure status, warnings, 
    /// and the full strongly-typed response from the OPUS API.
    /// Updated to match the new flat TotalReturnSwapResponse structure 
    /// (no longer uses .Data list or pagination).
    /// </summary>
    public class SwapValidationResult
    {
        public bool IsValid { get; set; }
        public string SwapId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// The full response returned by GetTotalReturnSwapAsync
        /// </summary>
        public TotalReturnSwapResponse SwapResponse { get; set; }

        /// <summary>
        /// Current notional value extracted from the Nominal field
        /// </summary>
        public decimal? CurrentNotional { get; set; }

        /// <summary>
        /// Number of quotes (currently not present in the root response - set to 0 or handle when quotes are added)
        /// </summary>
        public int? QuoteCount { get; set; }

        /// <summary>
        /// Timestamp of the most recent quote (not present in current model)
        /// </summary>
        public DateTime? LastQuoteTime { get; set; }

        /// <summary>
        /// Creates a successful validation result with the full response data
        /// </summary>
        public static SwapValidationResult Success(string swapId, TotalReturnSwapResponse response)
        {
            if (response == null)
                return Failure(swapId, "Received null response from OPUS");

            var result = new SwapValidationResult
            {
                IsValid = true,
                SwapId = swapId,
                SwapResponse = response,
                CurrentNotional = response.Nominal?.Quantity,
                Warnings = new List<string>()
            };

            // QuoteCount and LastQuoteTime are not available in the current TotalReturnSwapResponse model
            // Set defaults and add warning for transparency
            result.QuoteCount = 0;
            result.Warnings.Add("Quote validation skipped - quotes not present in current TotalReturnSwapResponse model.");

            return result;
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static SwapValidationResult Failure(string swapId, string errorMessage)
        {
            return new SwapValidationResult
            {
                IsValid = false,
                SwapId = swapId,
                ErrorMessage = errorMessage ?? "Validation failed",
                Warnings = new List<string>()
            };
        }

        /// <summary>
        /// Convenience property to check if there are any warnings
        /// </summary>
        public bool HasWarnings => Warnings != null && Warnings.Count > 0;

        /// <summary>
        /// Returns a formatted summary string useful for logging
        /// </summary>
        public string GetSummary()
        {
            string notionalInfo = CurrentNotional.HasValue
                ? $"Notional={CurrentNotional.Value:N2}"
                : "Notional=N/A";

            return $"Swap {SwapId}: Valid={IsValid}, " +
                   $"{notionalInfo}, " +
                   $"Quotes={QuoteCount ?? 0}, " +
                   $"Warnings={Warnings.Count}, " +
                   $"Error={(string.IsNullOrEmpty(ErrorMessage) ? "None" : ErrorMessage)}";
        }
    }
}