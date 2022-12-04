using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Kifa.Rpc;
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
        response.Dispose();
        return data;
    }

    public static JToken GetJToken(this HttpResponseMessage response)
        => JToken.Parse(GetString(response));

    public static T? GetObject<T>(this HttpResponseMessage response, bool camelCase = false)
        => JsonConvert.DeserializeObject<T>(GetString(response),
            camelCase ? KifaJsonSerializerSettings.CamelCase : KifaJsonSerializerSettings.Default);

    public static T? GetObject<T>(this HttpClient client, HttpRequestMessage request) {
        Logger.Trace(request);
        if (request.Content != null) {
            Logger.Trace($"Content: {request.Content.ReadAsStringAsync().Result}");
        }

        return client.Send(request).GetObject<T>();
    }

    public static HttpResponseMessage GetHeaders(this HttpClient client, string url) {
        Logger.Trace($"Get headers for {url}...");

        // Using HEAD won't be enough as sometimes the server only returns some information (e.g.
        // size) when requesting with a GET method.
        return client.SendWithRetry(() => {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, 0);
            return request;
        });
    }

    public static long? GetContentLength(this HttpClient client, string url) {
        Logger.Trace($"Get content length of {url}...");
        var length =
            client.SendWithRetry(() => new HttpRequestMessage(HttpMethod.Head, url)).Content.Headers
                .ContentLength ?? GetHeaders(client, url).Content.Headers.ContentRange?.Length;
        Logger.Trace($"{url}: {length}");
        return length;
    }

    public static JToken FetchJToken(this HttpClient client, Func<HttpRequestMessage> request,
        Func<JToken, bool>? validate = null)
        => Retry.Run(() => {
            var response = client.Send(request());
            response.EnsureSuccessStatusCode();
            return response.GetJToken();
        }, HandleHttpException, validate == null
            ? null
            : (token, index) => {
                if (validate(token)) {
                    return true;
                }

                if (index >= 5) {
                    throw new HttpRequestException($"Failed to get desired response: {token}");
                }

                Logger.Warn($"Failed to get desired response ({index}): {token}");
                return false;
            });

    public static HttpResponseMessage SendWithRetry(this HttpClient client, string url,
        HttpStatusCode? expectedStatusCode = null)
        => client.SendWithRetry(() => new HttpRequestMessage(HttpMethod.Get, url),
            expectedStatusCode);

    public static HttpResponseMessage SendWithRetry(this HttpClient client,
        Func<HttpRequestMessage> request, HttpStatusCode? expectedStatusCode = null)
        => Retry.Run(() => {
            var response = client.Send(request());
            if (expectedStatusCode != null && expectedStatusCode == response.StatusCode) {
                return response;
            }

            return response.EnsureSuccessStatusCode();
        }, HandleHttpException);

    public static TResponse? Call<TResponse>(this HttpClient client, KifaRpc<TResponse> rpc,
        HttpStatusCode? expectedStatusCode = null)
        => rpc.ParseResponse(client.SendWithRetry(rpc.GetRequest, expectedStatusCode));

    static void HandleHttpException(Exception ex, int index) {
        if (index >= 5 || ex is HttpRequestException {
                InnerException: SocketException {
                    Message: "Device not configured"
                }
            }) {
            throw ex;
        }

        Logger.Warn(ex, $"HTTP request failed ({index})");
        Thread.Sleep(TimeSpan.FromSeconds(5));
    }
}
