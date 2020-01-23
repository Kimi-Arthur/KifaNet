using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Pimix {
    public static class HttpExtensions {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static HttpResponseMessage SendWithRetry(this HttpClient client, Func<HttpRequestMessage> request) =>
            Retry.Run(() => client.SendAsync(request()).Result, (ex, index) => {
                if (index >= 5 ||
                    ex is HttpRequestException &&
                    ex.InnerException is SocketException socketException &&
                    socketException.Message == "Device not configured") {
                    throw ex;
                }

                logger.Warn(ex, $"HTTP request failed ({index})");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            });

        public static string GetString(this HttpResponseMessage response) {
            using var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result,
                Encoding.GetEncoding("UTF-8"));
            var data = sr.ReadToEnd();
            logger.Trace("Response ({0:D}): {1}", response.StatusCode, data);
            return data;
        }

        public static JToken GetJToken(this HttpResponseMessage response)
            => JToken.Parse(GetString(response));

        public static JToken FetchJTokenWithRetry(this HttpClient client, Func<HttpRequestMessage> request,
            Func<JToken, bool> validate = null) =>
            Retry.Run(() => {
                var result = client.SendAsync(request()).Result.GetJToken();
                
                if (validate != null && !validate(result)) {
                    throw new ResponseValidationException("Response body does not indicate successful status.");
                }

                return result;
            }, (ex, index) => {
                if (index >= 5 ||
                    ex is HttpRequestException &&
                    ex.InnerException is SocketException socketException &&
                    socketException.Message == "Device not configured") {
                    throw ex;
                }

                logger.Warn(ex, $"HTTP request failed ({index})");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            });

        public static T GetObject<T>(this HttpResponseMessage response)
            => JsonConvert.DeserializeObject<T>(GetString(response), Defaults.JsonSerializerSettings);
    }

    public class ResponseValidationException : Exception {
        public ResponseValidationException(string message) : base(message) {
        }
    }
}
