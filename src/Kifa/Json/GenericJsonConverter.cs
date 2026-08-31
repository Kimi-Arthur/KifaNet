using System;
using Newtonsoft.Json;

namespace Kifa;

public class GenericJsonConverter : JsonConverter<JsonSerializable?> {
    public override void WriteJson(JsonWriter writer, JsonSerializable? value,
        JsonSerializer serializer) {
        writer.WriteValue(value?.ToJson());
    }

    public override JsonSerializable? ReadJson(JsonReader reader, Type objectType,
        JsonSerializable? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.Value == null) {
            return null;
        }

        var value = reader.Value switch {
            DateTime dt => dt.ToUniversalTime().ToString("yyyyMMddHHmmssffffff"),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("yyyyMMddHHmmssffffff"),
            _ => reader.Value.ToString()
        };

        if (value == null) {
            return null;
        }

        return objectType.GetMethod("op_Implicit", new[] { typeof(string) })
                   ?.Invoke(null, new object?[] { value }) as JsonSerializable;
    }
}
