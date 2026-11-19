using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace Kifa;

public class GenericJsonConverter(bool ignoreExpandedLinks = false)
    : JsonConverter<JsonSerializable?> {
    // Bridges runtime System.Type into constrained generic parameter T to invoke static virtual T.ReadJson.
    // Could be simplified if C# adds native constraint bridging / type opening:
    // https://github.com/dotnet/csharplang/discussions/6308
    interface JsonReaderAdapter {
        JsonSerializable? Read(JsonReader reader, JsonSerializer serializer);
    }

    class JsonReaderAdapter<T> : JsonReaderAdapter where T : class, JsonSerializable<T> {
        public JsonSerializable? Read(JsonReader reader, JsonSerializer serializer)
            => T.ReadJson(reader, serializer);
    }

    static readonly ConcurrentDictionary<Type, JsonReaderAdapter> Readers = new();

    static JsonReaderAdapter CreateReader(Type objectType)
        => (JsonReaderAdapter) Activator
            .CreateInstance(typeof(JsonReaderAdapter<>).MakeGenericType(objectType))
            .Checked();

    public override void WriteJson(JsonWriter writer, JsonSerializable? value,
        JsonSerializer serializer) {
        serializer.Serialize(writer,
            ignoreExpandedLinks ? value?.ToJson() : value?.ToExpandedJson());
    }

    public override JsonSerializable? ReadJson(JsonReader reader, Type objectType,
        JsonSerializable? existingValue, bool hasExistingValue, JsonSerializer serializer)
        => Readers.GetOrAdd(objectType, CreateReader).Read(reader, serializer);
}
