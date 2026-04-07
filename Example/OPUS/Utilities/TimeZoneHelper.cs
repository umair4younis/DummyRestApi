using System;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.Utilities
{
    /// <summary>
    /// Static helper class for timezone conversions, especially between Windows and IANA formats.
    /// OPUS API requires IANA timezone identifiers (e.g. "Europe/Berlin").
    /// </summary>
    public static class TimeZoneHelper
    {
        /// <summary>
        /// Mapping from common Windows timezone names to IANA timezone identifiers.
        /// </summary>
        private static readonly Dictionary<string, string> WindowsToIanaMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Central European Standard Time", "Europe/Berlin" },
            { "W. Europe Standard Time", "Europe/Berlin" },
            { "Romance Standard Time", "Europe/Paris" },
            { "Central Europe Standard Time", "Europe/Berlin" },
            { "UTC", "UTC" },
            { "GMT Standard Time", "Europe/London" },
            { "Greenwich Standard Time", "Europe/London" },
            { "Eastern European Standard Time", "Europe/Bucharest" },
            { "Western European Standard Time", "Europe/Lisbon" },
            { "Pacific Standard Time", "America/Los_Angeles" },
            { "Eastern Standard Time", "America/New_York" },
            { "Central Standard Time", "America/Chicago" },
            { "Mountain Standard Time", "America/Denver" }
            // Add more mappings as needed
        };

        /// <summary>
        /// Returns the IANA timezone identifier for the current system timezone.
        /// Falls back safely to "Europe/Berlin" (most common for European financial systems).
        /// </summary>
        public static string GetIanaTimeZone()
        {
            try
            {
                string windowsTimeZone = TimeZone.CurrentTimeZone.StandardName;

                // Exact match
                if (WindowsToIanaMap.TryGetValue(windowsTimeZone, out string ianaTz))
                {
                    return ianaTz;
                }

                // Partial match (more flexible)
                foreach (var pair in WindowsToIanaMap)
                {
                    if (windowsTimeZone.Contains(pair.Key) || pair.Key.Contains(windowsTimeZone))
                    {
                        return pair.Value;
                    }
                }

                // Safe default for most European users
                return "Europe/Berlin";
            }
            catch (Exception)
            {
                // In case of any unexpected error (very rare), return safe default
                return "Europe/Berlin";
            }
        }
    }
}