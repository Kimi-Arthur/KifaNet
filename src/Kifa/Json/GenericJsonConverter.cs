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
        var value = (string?) reader.Value;
        if (value == null) {
            return null;
        }

        return existingValue ??
               objectType.GetMethod("op_Implicit", new[] { typeof(string) })
                   ?.Invoke(null, new object?[] { value }) as JsonSerializable;
    }
}
