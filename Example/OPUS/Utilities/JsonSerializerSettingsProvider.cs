using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using Puma.MDE.OPUS.Models;


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

        public static OpusOperationResult<string> TrySerialize(object value)
        {
            try
            {
                return OpusOperationResult<string>.SuccessWithData(Serialize(value));
            }
            catch (Exception ex)
            {
                Puma.MDE.Engine.Instance.Log.Error("[JsonSerializerSettingsProvider.TrySerialize] Failed: " + ex.ToString());
                return OpusOperationResult<string>.FailureWithData("Unable to serialize OPUS data.", ex.Message);
            }
        }

        public static OpusOperationResult<T> TryDeserialize<T>(string json)
        {
            try
            {
                return OpusOperationResult<T>.SuccessWithData(Deserialize<T>(json));
            }
            catch (Exception ex)
            {
                Puma.MDE.Engine.Instance.Log.Error("[JsonSerializerSettingsProvider.TryDeserialize] Failed: " + ex.ToString());
                return OpusOperationResult<T>.FailureWithData("Unable to read OPUS response data.", ex.Message);
            }
        }
    }
}
