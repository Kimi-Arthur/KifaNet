using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa;

public static class HttpExtensions {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string GetString(this HttpResponseMessage response) {
        using var sr = new StreamReader(response.Content.ReadAsStreamAsync().Result,
            Encoding.GetEncoding("UTF-8"));
        var data = sr.ReadToEnd();
        Logger.Trace($"Response ({response.StatusCode:D}): {data}");
        return data;
    }

    public static JToken GetJToken(this HttpResponseMessage response)
        => JToken.Parse(GetString(response));

    static T? GetObject<T>(this HttpResponseMessage response)
        => JsonConvert.DeserializeObject<T>(GetString(response), Defaults.JsonSerializerSettings);

    public static T? GetObject<T>(this HttpClient client, HttpRequestMessage request) {
        Logger.Trace(request);
        if (request.Content != null) {
            Logger.Trace($"Content: {request.Content.ReadAsStringAsync().Result}");
        }

        using var response = client.Send(request);
        return response.GetObject<T>();
    }

    public static HttpResponseMessage GetHeaders(this HttpClient client, string url) {
        Logger.Trace($"Get headers for {url}...");
        return client.SendWithRetry(() => {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, 0);
            return request;
        });
    }

    public static long? GetContentLength(this HttpClient client, string url) {
        Logger.Trace($"Get content length of {url}...");
        return GetHeaders(client, url).Content.Headers.ContentRange?.Length;
    }

    public static HttpResponseMessage SendWithRetry(this HttpClient client,
        Func<HttpRequestMessage> request)
        => Retry.Run(() => client.Send(request()).EnsureSuccessStatusCode(), (ex, index) => {
            if (index >= 5 || ex is HttpRequestException &&
                ex.InnerException is SocketException socketException &&
                socketException.Message == "Device not configured") {
                throw ex;
            }

            Logger.Warn(ex, $"HTTP request failed ({index})");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        });

    public static JToken FetchJToken(this HttpClient client, Func<HttpRequestMessage> request,
        Func<JToken, bool>? validate = null)
        => Retry.Run(() => {
            var result = client.SendAsync(request()).Result.GetJToken();

            if (validate != null && !validate(result)) {
                throw new InvalidResponseException(
                    "Response body does not indicate successful status.");
            }

            return result;
        }, (ex, index) => {
            if (index >= 5 || ex is HttpRequestException &&
                ex.InnerException is SocketException socketException &&
                socketException.Message == "Device not configured") {
                throw ex;
            }

            Logger.Warn(ex, $"HTTP request failed ({index})");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        });
}

public class InvalidResponseException : Exception {
    public InvalidResponseException(string message) : base(message) {
    }
}
