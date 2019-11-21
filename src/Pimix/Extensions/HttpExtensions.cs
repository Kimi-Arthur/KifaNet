using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Pimix {
    public static class HttpExtensions {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static HttpResponseMessage SendWithRetry(this HttpClient client, HttpRequestMessage request) =>
            client.SendAsync(request).Result;

        public static string GetString(this HttpResponseMessage response) {
            using var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result,
                Encoding.GetEncoding("UTF-8"));
            var data = sr.ReadToEnd();
            logger.Trace("Response ({0:D}): {1}", response.StatusCode, data);
            return data;
        }

        public static JToken GetJToken(this HttpResponseMessage response)
            => JToken.Parse(GetString(response));

        public static T GetObject<T>(this HttpResponseMessage response)
            => JsonConvert.DeserializeObject<T>(GetString(response), Defaults.JsonSerializerSettings);
    }
}
