using System.Net.Http;

namespace Kifa.Bilibili.BiliplusApi {
    public abstract class BiliplusRpc<TResponse> : JsonRpc<TResponse> {
        public override HttpClient HttpClient { get; } = BiliplusHttpClient.Instance;
    }
}
