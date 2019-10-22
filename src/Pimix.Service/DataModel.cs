using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using NLog;

namespace Pimix.Service {
    public abstract class DataModel {
        public string Id { get; set; }

        public virtual void Fill() {
        }

        public override string ToString() => JsonConvert.SerializeObject(this, Defaults.PrettyJsonSerializerSettings);
    }

    public static class ClonableExtension {
        public static TDataModel Clone<TDataModel>(this TDataModel data) =>
            JsonConvert.DeserializeObject<TDataModel>(JsonConvert.SerializeObject(data));
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
                request.Content =
                    new ByteArrayContent(Encoding.UTF8.GetBytes(Data.Format(parameters)));

                foreach (var header in Headers.Where(h => h.Key.StartsWith("Content-"))) {
                    request.Content.Headers.Add(header.Key, header.Value.Format(parameters));
                }
            }

            return request;
        }
    }
}
