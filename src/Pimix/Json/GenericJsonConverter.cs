using System;
using Newtonsoft.Json;

namespace Pimix {
    public class GenericJsonConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue((value as JsonSerializable).ToJson());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            if (existingValue == null) {
                existingValue = Activator.CreateInstance(objectType);
            }

            (existingValue as JsonSerializable).FromJson((string) reader.Value);
            return existingValue;
        }

        public override bool CanConvert(Type objectType)
            => typeof(JsonSerializable).IsAssignableFrom(objectType);
    }

    public interface JsonSerializable {
        string ToJson();
        void FromJson(string data);
    }
}
