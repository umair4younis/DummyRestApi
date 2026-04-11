using System;
using System.Globalization;
using Puma.MDE.OPUS.Models;


namespace Puma.MDE
{
    public static class PercentageHelper
    {
        /// <summary>
        /// Converts a string like "4.69%" or "4.69" to decimal (e.g. 0.0469)
        /// </summary>
        public static bool TryParsePercentage(string value, out decimal result)
        {
            result = 0m;

            if (string.IsNullOrWhiteSpace(value))
            {
                Engine.Instance.Log.Debug($"[PercentageHelper] TryParsePercentage failed: input is null/empty");
                return false;
            }

            // Remove % sign and any whitespace
            string cleaned = value.Replace("%", "").Trim();

            // Try to parse the cleaned number
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                result = result / 100m;   // Convert from percentage to decimal (4.69% → 0.0469)
                Engine.Instance.Log.Debug($"[PercentageHelper] Parsed '{value}' to {result:F6} decimal");
                return true;
            }

            Engine.Instance.Log.Warn($"[PercentageHelper] TryParsePercentage failed to parse '{value}' (cleaned: '{cleaned}')");
            return false;
        }

        /// <summary>
        /// Converts a string like "4.69%" to decimal. Throws exception if invalid.
        /// </summary>
        public static decimal ParsePercentage(string value)
        {
            Engine.Instance.Log.Debug($"[PercentageHelper] ParsePercentage called with value: '{value}'");
            if (TryParsePercentage(value, out decimal result))
            {
                Engine.Instance.Log.Debug($"[PercentageHelper] ParsePercentage succeeded: {result:F6}");
                return result;
            }

            Engine.Instance.Log.Error($"[PercentageHelper] ParsePercentage failed for '{value}' - invalid format");
            throw new FormatException($"String '{value}' was not recognized as a valid percentage.");
        }

        public static OpusOperationResult<decimal> ParsePercentageSafe(string value)
        {
            try
            {
                return OpusOperationResult<decimal>.SuccessWithData(ParsePercentage(value));
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Error($"[PercentageHelper] ParsePercentageSafe failed: {ex}");
                return OpusOperationResult<decimal>.FailureWithData("Unable to read percentage value.", ex.Message);
            }
        }
    }
}
