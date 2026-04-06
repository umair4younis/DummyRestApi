using Newtonsoft.Json;
using System;


namespace Example.OPUS.Utilities
{
    /// <summary>
    /// Ensures DateTime is serialized as ISO 8601 with milliseconds and 'Z' (UTC).
    /// </summary>
    public class OpisIsoDateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return null;
            return DateTime.Parse(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            DateTime dt = (value is DateTime ? (DateTime)value : ((DateTime?)value).Value).ToUniversalTime();

            // Format: yyyy-MM-ddTHH:mm:ss.fffZ
            string formatted = dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            writer.WriteValue(formatted);
        }
    }
}
