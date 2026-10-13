using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Kifa;

public class DateTimeOffsetConverter : JsonConverter {
    public override bool CanConvert(Type objectType)
        => objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

    const string DefaultFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
            return;
        }

        var dto = (DateTimeOffset) value;
        var format = serializer.DateFormatString ?? DefaultFormat;
        writer.WriteValue(dto.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture));
    }

    static readonly string[] Formats = [
        "yyyyMMddHHmmssffffff",
        "yyyyMMddHHmmssfff",
        "yyyyMMddHHmmss",
        "yyyyMMddHHmm",
        "yyyyMMdd",
        "yyyy-MM-ddTHH:mm:ss.ffffffzzz",
        "yyyy-MM-ddTHH:mm:ss.ffffff",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.ffffff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd"
    ];

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return objectType == typeof(DateTimeOffset?) ? null : default(DateTimeOffset);
        }

        if (reader.TokenType == JsonToken.Date) {
            if (reader.Value is DateTime dt) {
                var utc = dt.Kind == DateTimeKind.Local
                    ? dt.ToUniversalTime()
                    : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return new DateTimeOffset(utc, TimeSpan.Zero);
            }

            if (reader.Value is DateTimeOffset dto) {
                return new DateTimeOffset(dto.UtcDateTime, TimeSpan.Zero);
            }
        }

        if (reader.TokenType == JsonToken.String && reader.Value is string s) {
            if (DateTimeOffset.TryParseExact(s, Formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exactParsed)) {
                return exactParsed;
            }

            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)) {
                return parsed;
            }
        }

        throw new JsonSerializationException(
            $"Cannot convert {reader.Value} ({reader.TokenType}) to DateTimeOffset.");
    }
}
