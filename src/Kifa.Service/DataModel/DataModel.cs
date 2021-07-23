using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public abstract class DataModel<TDataModel> : DataModel where TDataModel : DataModel {
        [JsonProperty("$translations")]
        public Dictionary<string, TDataModel> Translations { get; set; }
    }
}
