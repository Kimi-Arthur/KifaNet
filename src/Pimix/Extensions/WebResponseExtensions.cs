using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Pimix {
    public static class WebResponseExtensions {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Dictionary<string, string> EncodingNameFixes =
            new Dictionary<string, string> {["utf8"] = "UTF-8"};

        static string GetString(WebResponse response) {
            var resp = response as HttpWebResponse;
            var encodingName = resp.ContentEncoding;
            if (string.IsNullOrEmpty(encodingName)) {
                encodingName = resp.CharacterSet;
            }

            if (string.IsNullOrEmpty(encodingName)) {
                encodingName = "UTF-8";
            }

            // Wrongly encoding name handling.
            encodingName = EncodingNameFixes.GetValueOrDefault(encodingName, encodingName);

            using (var sr = new StreamReader(resp.GetResponseStream(),
                Encoding.GetEncoding(encodingName))) {
                var data = sr.ReadToEnd();
                logger.Trace("Response ({0:D}): {1}", resp.StatusCode, data);
                return data;
            }
        }

        public static JToken GetJToken(this WebResponse response)
            => JToken.Parse(GetString(response));

        public static T GetObject<T>(this WebResponse response)
            => JsonConvert.DeserializeObject<T>(GetString(response), Defaults.JsonSerializerSettings);

        public static Dictionary<string, object> GetDictionary(this WebResponse response)
            => response.GetObject<Dictionary<string, object>>();

        static string GetString(HttpResponseMessage response) {
            using (var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result,
                Encoding.GetEncoding("UTF-8"))) {
                var data = sr.ReadToEnd();
                logger.Trace("Response ({0:D}): {1}", response.StatusCode, data);
                return data;
            }
        }

        public static JToken GetJToken(this HttpResponseMessage response)
            => JToken.Parse(GetString(response));

        public static T GetObject<T>(this HttpResponseMessage response)
            => JsonConvert.DeserializeObject<T>(GetString(response), Defaults.JsonSerializerSettings);
    }
}
