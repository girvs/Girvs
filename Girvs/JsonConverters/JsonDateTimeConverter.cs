using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Girvs.JsonConverters
{
    public class JsonDateTimeConverter: JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString() ?? "2000-01-01 00:00:00");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"));
        }
    }
}