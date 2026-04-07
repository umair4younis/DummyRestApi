using System;
using System.Globalization;


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
                return false;

            // Remove % sign and any whitespace
            string cleaned = value.Replace("%", "").Trim();

            // Try to parse the cleaned number
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                result = result / 100m;   // Convert from percentage to decimal (4.69% → 0.0469)
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a string like "4.69%" to decimal. Throws exception if invalid.
        /// </summary>
        public static decimal ParsePercentage(string value)
        {
            if (TryParsePercentage(value, out decimal result))
                return result;

            throw new FormatException($"String '{value}' was not recognized as a valid percentage.");
        }
    }
}
