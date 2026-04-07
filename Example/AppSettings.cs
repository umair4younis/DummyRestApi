using System;
using System.Configuration;


namespace Puma.MDE
{
    public static class AppSettings
    {
        /// <summary>
        /// Gets string value from appSettings. Returns fallback if key is missing or empty.
        /// </summary>
        public static string Get(string key, string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            string value = ConfigurationManager.AppSettings[key];

            // Trim + treat empty/whitespace-only as missing
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim();
        }

        /// <summary>
        /// Gets string value – throws if key is missing or value is empty/whitespace.
        /// </summary>
        public static string GetRequired(string key)
        {
            string value = Get(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(
                    $"Required appSetting key '{key}' is missing or empty in app.config.");
            }
            return value;
        }

        /// <summary>
        /// Gets integer value with fallback
        /// </summary>
        public static int GetInt(string key, int fallback = 0)
        {
            string raw = Get(key);
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            if (int.TryParse(raw, out int value))
                return value;

            // You can also log/warn here if desired
            return fallback;
        }

        /// <summary>
        /// Gets boolean value with fallback
        /// </summary>
        public static bool GetBool(string key, bool fallback = false)
        {
            string raw = Get(key)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            // common true values
            if (raw == "true" || raw == "1" || raw == "yes" || raw == "on")
                return true;

            // common false values
            if (raw == "false" || raw == "0" || raw == "no" || raw == "off")
                return false;

            return fallback;
        }

        /// <summary>
        /// Gets value and converts it using provided converter function
        /// </summary>
        public static T GetAs<T>(string key, Func<string, T> converter, T fallback = default(T))
        {
            string raw = Get(key);
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            try
            {
                return converter(raw);
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Helper method to safely read integer settings with fallback
        /// </summary>
        public static int GetAppSettingInt(string key, int defaultValue = 0)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"Warning: Key '{key}' not found. Using default: {defaultValue}");
                return defaultValue;
            }

            if (int.TryParse(value, out int result))
            {
                return result;
            }

            Console.WriteLine($"Warning: Key '{key}' has invalid integer value '{value}'. Using default: {defaultValue}");
            return defaultValue;
        }
    }
}
