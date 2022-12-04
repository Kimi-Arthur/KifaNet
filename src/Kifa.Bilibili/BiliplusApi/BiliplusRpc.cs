using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BiliplusApi;

public abstract class BiliplusRpc<TResponse> : JsonRpc<TResponse> {
    public override HttpClient HttpClient { get; set; } = HttpClients.BilibiliHttpClient;
}
