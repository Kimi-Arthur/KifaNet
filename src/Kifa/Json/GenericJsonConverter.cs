using System;
using Newtonsoft.Json;

namespace Kifa;

public class GenericJsonConverter : JsonConverter<JsonSerializable?> {
    public override void WriteJson(JsonWriter writer, JsonSerializable? value,
        JsonSerializer serializer) {
        writer.WriteValue(value?.ToJson());
    }

    public override JsonSerializable? ReadJson(JsonReader reader, Type objectType,
        JsonSerializable? existingValue, bool hasExistingValue, JsonSerializer serializer)
        => throw new NotImplementedException();

    // We will rely on type's implicit operator to deserialize.
    public override bool CanRead => false;
}
