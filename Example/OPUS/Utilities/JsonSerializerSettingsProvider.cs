using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Puma.MDE.OPUS.Utilities
{
    /// <summary>
    /// Centralized JSON serialization settings for the entire application.
    /// Ensures null values and default values are omitted unless explicitly set.
    /// </summary>
    public static class JsonSerializerSettingsProvider
    {
        private static readonly JsonSerializerSettings _settings;

        static JsonSerializerSettingsProvider()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,           // Omit null properties
                DefaultValueHandling = DefaultValueHandling.Ignore,     // Omit default values (0, false, empty string, etc.)
                Formatting = Formatting.None,                           // Compact JSON (no extra whitespace)
                ContractResolver = new DefaultContractResolver()        // Standard resolver
            };
        }

        /// <summary>
        /// Returns the global serializer settings to use everywhere.
        /// </summary>
        public static JsonSerializerSettings Settings => _settings;

        /// <summary>
        /// Convenience method to serialize any object using global settings.
        /// </summary>
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        /// <summary>
        /// Convenience method to deserialize.
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}
