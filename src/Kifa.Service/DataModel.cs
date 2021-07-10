using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using YamlDotNet.Serialization;

namespace Kifa.Service {
    /// <summary>
    /// When used, specify a public const string field named ModelId.
    /// </summary>
    public abstract class DataModel {
        [YamlMember(Order = -1)]
        public string Id { get; set; }

        [JsonProperty("$metadata")]
        [YamlIgnore]
        public DataMetadata Metadata { get; set; }

        public virtual bool? Fill() => null;

        // Not finished
        public string Compare<TDataModel>(TDataModel other) {
            if (!(this is TDataModel model)) {
                return "<Different type>";
            }

            var myJson = JToken.Parse(ToString());
            var otherJson = JToken.Parse(ToString());
            var diffToken = CompareJToken(myJson, otherJson);
            return diffToken.ToString();
        }

        JToken CompareJToken(JToken myJson, JToken otherJson) {
            var result = new JArray();
            if (myJson.Type != otherJson.Type) {
                var myToken = new JObject();
                myToken["-"] = myJson;
                result.Add(myToken);
                var otherToken = new JObject();
                otherToken["+"] = otherJson;
                result.Add(otherToken);
                return result;
            }

            if (myJson.Type == JTokenType.Array) {
                foreach (var childPair in myJson.Children().Zip(otherJson.Children())) {
                    if (childPair.First != childPair.Second) {
                        if (childPair.First != null) {
                            var myToken = new JObject();
                            myToken["-"] = myJson;
                            result.Add(myToken);
                        }
                    }
                }
            }

            return result;
        }

        public override string ToString() => JsonConvert.SerializeObject(this, Defaults.PrettyJsonSerializerSettings);
    }

    public class DataMetadata {
        public string Id { get; set; }

        // If this one is the source, this field will be populated with all other instances with the data.
        public HashSet<string> Links { get; set; }

        // If this one is the source, this field will be populated with all other instances that can be automatically
        // generated.
        public HashSet<string> VirtualLinks { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }
        public DateTimeOffset? LastRefreshed { get; set; }
    }

    public abstract class TranslatableDataModel<T> : DataModel where T : DataModel {
        public Dictionary<string, T> Translations { get; set; }

        public string DefaultLanguage { get; set; }
    }

    public static class ClonableExtension {
        public static TDataModel Clone<TDataModel>(this TDataModel data) =>
            JsonConvert.DeserializeObject<TDataModel>(
                JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings), Defaults.JsonSerializerSettings);
    }

    public static class MergableExtension {
        public static TDataModel Merge<TDataModel>(this TDataModel data, TDataModel update) {
            var obj = data.Clone();
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(update, Defaults.JsonSerializerSettings), obj,
                Defaults.JsonSerializerSettings);
            return obj;
        }
    }

    public static class TranslatableExtension {
        public static TDataModel GetTranslated<TDataModel>(this TDataModel data, string language)
            where TDataModel : TranslatableDataModel<TDataModel> =>
            language == data.DefaultLanguage ? data : data.Merge(data.Translations[language]);
    }

    public class API {
        public string Method { get; set; }

        public string Url { get; set; }

        public string Data { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public HttpRequestMessage GetRequest(Dictionary<string, string> parameters = null) {
            parameters ??= new Dictionary<string, string>();
            var address = Url.Format(parameters);

            logger.Trace($"{Method} {address}");
            var request = new HttpRequestMessage(new HttpMethod(Method), address);

            foreach (var header in Headers.Where(h => !h.Key.StartsWith("Content-"))) {
                request.Headers.Add(header.Key, header.Value.Format(parameters));
            }

            if (Data != null) {
                request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(Data.Format(parameters)));

                foreach (var header in Headers.Where(h => h.Key.StartsWith("Content-"))) {
                    request.Content.Headers.Add(header.Key, header.Value.Format(parameters));
                }
            }

            return request;
        }
    }
}
