using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using NLog;
using Pimix;

namespace Kifa.Service {
    /// <summary>
    /// When used, specify a public const string field named ModelId.
    /// </summary>
    public abstract class DataModel {
        public string Id { get; set; }

        [JsonProperty("$metadata")]
        public DataMetadata Metadata { get; set; }

        public virtual bool? Fill() => null;

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