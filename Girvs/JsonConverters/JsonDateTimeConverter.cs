namespace Girvs.JsonConverters;

public class JsonDateTimeConverter: JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // ISO 8601 可直接由 reader 解析，无需先取字符串再回解析。
        if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTime(out var fastValue))
            return fastValue;

        var text = reader.GetString() ?? "2000-01-01 00:00:00";

        // 兼容 "yyyy-MM-dd HH:mm:ss" 等非 ISO 格式；固定区域性避免宿主 culture 影响解析。
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : DateTime.Parse(text);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss", CultureInfo.InvariantCulture));
    }
}