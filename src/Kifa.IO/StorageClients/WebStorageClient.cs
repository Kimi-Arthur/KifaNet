using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using NLog;

namespace Kifa.IO.StorageClients;

public class WebStorageClient : StorageClient {
    readonly HttpClient httpClient = GetHttpClient();
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Dictionary<string, string> ProxyMap { get; set; } = new();

    static HttpClient GetHttpClient() {
        var client = new HttpClient(new HttpClientHandler {
            Proxy = new AutoSwitchWebProxy(ProxyMap)
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
        return client;
    }

    public override void Dispose() {
        httpClient.Dispose();
    }

    public string Protocol { get; set; }

    public override long Length(string path)
        => httpClient.GetContentLength(GetUrl(path)) ?? throw new FileNotFoundException();

    public override void Delete(string path) {
        Logger.Warn($"Deleting {path} is ignored.");
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    public override Stream OpenRead(string path) {
        var url = GetUrl(path);
        return new SeekableReadStream(Length(path), (buffer, bufferOffset, offset, count) => {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
            var content = httpClient.Send(request).Content;
            var memoryStream = new MemoryStream(buffer, bufferOffset, count);
            content.CopyToAsync(memoryStream).Wait();
            return (int) memoryStream.Position;
        });
    }

    public override void Write(string path, Stream stream) {
        throw new NotImplementedException();
    }

    public override string Type => Protocol;
    public override string Id => "";

    string GetUrl(string path) => $"{Protocol}:{path}";
}
