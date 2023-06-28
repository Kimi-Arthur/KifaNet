using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using NLog;

namespace Kifa.Rpc;

public abstract class KifaParameterizedRpc : KifaRpc {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected abstract string Url { get; }

    protected abstract HttpMethod Method { get; }

    protected virtual Dictionary<string, string> Headers => new();

    // Headers for multi part content's parts. Key'ed by dataKey.
    protected virtual Dictionary<string, Dictionary<string, string>> PartHeaders => new();

    // Different types of content
    protected virtual string? JsonContent => null;

    protected virtual List<KeyValuePair<string, string>>? FormContent => null;

    public virtual List<(string dataKey, string name, string fileName)>? ExtraMultipartContent {
        get;
    }

    protected Dictionary<string, string> Parameters { get; set; } = new();
    protected Dictionary<string, byte[]> ByteParameters { get; set; } = new();

    static readonly string ContentHeaderPrefix = "content-";

    public HttpRequestMessage GetRequest() {
        var address = Url.Format(Parameters);
        Logger.Trace($"{Method} {address}");

        var request = new HttpRequestMessage(Method, address);

        foreach (var (headerName, value) in Headers.Where(h
                     => !h.Key.ToLower().StartsWith(ContentHeaderPrefix))) {
            request.Headers.Add(headerName, value.Format(Parameters));
        }

        if (JsonContent != null) {
            var content = new StringContent(JsonContent.Format(Parameters)) {
                Headers = {
                    ContentType = MediaTypeHeaderValue.Parse("application/json; charset=UTF-8")
                }
            };
            request.Content = content;
        } else if (ExtraMultipartContent != null) {
            var multipartContent = new MultipartFormDataContent();
            request.Content = multipartContent;
            if (FormContent != null) {
                foreach (var (name, value) in FormContent) {
                    multipartContent.Add(new StringContent(value.Format(Parameters)),
                        name.Format(Parameters));
                }
            }

            foreach (var (dataKey, name, fileName) in ExtraMultipartContent) {
                var content = new ByteArrayContent(ByteParameters[dataKey]);
                if (PartHeaders.TryGetValue(dataKey, out var header)) {
                    foreach (var (headerName, value) in header) {
                        content.Headers.Add(headerName.Format(Parameters),
                            value.Format(Parameters));
                    }
                }

                multipartContent.Add(content, name.Format(Parameters), fileName.Format(Parameters));
            }
        } else if (FormContent != null) {
            request.Content = new FormUrlEncodedContent(FormContent.Select(item
                => new KeyValuePair<string?, string?>(item.Key, item.Value.Format(Parameters))));
        }

        if (request.Content != null) {
            foreach (var (headerName, value) in Headers.Where(h
                         => h.Key.ToLower().StartsWith(ContentHeaderPrefix))) {
                request.Content.Headers.Add(headerName, value.Format(Parameters));
            }
        }

        return request;
    }
}
