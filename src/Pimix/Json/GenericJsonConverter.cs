using System;
using Newtonsoft.Json;

namespace Pimix {
    public class GenericJsonConverter : JsonConverter<JsonSerializable> {
        public override void WriteJson(JsonWriter writer, JsonSerializable value, JsonSerializer serializer) {
            writer.WriteValue(value.ToJson());
        }

        public override JsonSerializable ReadJson(JsonReader reader, Type objectType, JsonSerializable existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) {
            existingValue ??= Activator.CreateInstance(objectType) as JsonSerializable;
            if (existingValue == null) {
                return null;
            }

            existingValue.FromJson((string) reader.Value);
            return existingValue;
        }
    }

    public interface JsonSerializable {
        string ToJson();
        void FromJson(string data);
    }
}
