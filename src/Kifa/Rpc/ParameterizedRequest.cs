using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using NLog;

namespace Kifa.Rpc;

public abstract class ParameterizedRequest {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public abstract string UrlPattern { get; }

    public virtual HttpMethod Method { get; } = HttpMethod.Get;

    public virtual Dictionary<string, string> Headers { get; } = new();

    // Headers for multi part content's parts. Key'ed by dataKey.
    public virtual Dictionary<string, Dictionary<string, string>> PartHeaders { get; } = new();

    // Different types of content
    public virtual List<KeyValuePair<string, string>>? FormContent => null;

    public virtual List<(string dataKey, string name, string fileName)>? ExtraMultipartContent {
        get;
    }

    protected virtual Dictionary<string, string> parameters { get; set; } = new();
    protected virtual Dictionary<string, byte[]> byteParameters { get; set; } = new ();

    public HttpRequestMessage GetRequest() {
        var address = UrlPattern.Format(parameters);
        Logger.Trace($"{Method} {address}");

        var request = new HttpRequestMessage(Method, address);

        foreach (var (headerName, value) in Headers.Where(h => !h.Key.StartsWith("Content-"))) {
            request.Headers.Add(headerName, value.Format(parameters));
        }

        if (ExtraMultipartContent != null) {
            var multipartContent = new MultipartFormDataContent();
            request.Content = multipartContent;
            if (FormContent != null) {
                foreach (var (name, value) in FormContent) {
                    multipartContent.Add(new StringContent(value.Format(parameters)),
                        name.Format(parameters));
                }
            }

            foreach (var (dataKey, name, fileName) in ExtraMultipartContent) {
                var content = new ByteArrayContent(byteParameters[dataKey]);
                if (PartHeaders.ContainsKey(dataKey)) {
                    foreach (var (headerName, value) in PartHeaders[dataKey]) {
                        content.Headers.Add(headerName.Format(parameters),
                            value.Format(parameters));
                    }
                }

                multipartContent.Add(content, name.Format(parameters), fileName.Format(parameters));
            }
        } else if (FormContent != null) {
            request.Content = new FormUrlEncodedContent(FormContent.Select(item
                => new KeyValuePair<string?, string?>(item.Key, item.Value.Format(parameters))));
        }

        if (request.Content != null) {
            foreach (var (headerName, value) in Headers.Where(h => h.Key.StartsWith("Content-"))) {
                request.Content.Headers.Add(headerName, value.Format(parameters));
            }
        }

        return request;
    }
}