using System;
using Newtonsoft.Json;

namespace Kifa;

public interface JsonSerializable {
    string ToJson();

    object ToExpandedJson() => ToJson();
}

public interface JsonSerializable<TSelf> : JsonSerializable
    where TSelf : class, JsonSerializable<TSelf> {
    static abstract implicit operator TSelf?(string? value);

    static virtual TSelf? ReadJson(JsonReader reader, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return null;
        }

        if (reader.Value == null) {
            reader.Skip();
            return null;
        }

        return reader.Value switch {
            DateTime dt => dt.ToUniversalTime().ToString("yyyyMMddHHmmssffffff"),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("yyyyMMddHHmmssffffff"),
            _ => reader.Value.ToString()
        };
    }
}
