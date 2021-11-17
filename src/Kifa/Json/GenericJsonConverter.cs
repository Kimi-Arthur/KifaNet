using System;
using Newtonsoft.Json;

namespace Kifa {
    public class GenericJsonConverter : JsonConverter<JsonSerializable?> {
        public override void WriteJson(JsonWriter writer, JsonSerializable? value, JsonSerializer serializer) {
            writer.WriteValue(value?.ToJson());
        }

        public override JsonSerializable? ReadJson(JsonReader reader, Type objectType, JsonSerializable? existingValue,
            bool hasExistingValue, JsonSerializer serializer) {
            existingValue ??= Activator.CreateInstance(objectType) as JsonSerializable;
            if (existingValue == null) {
                return null;
            }

            var readerValue = (string?) reader.Value;
            if (readerValue != null) {
                existingValue.FromJson(readerValue);
            }

            return existingValue;
        }
    }

    public interface JsonSerializable {
        string ToJson();
        void FromJson(string data);
    }
}
