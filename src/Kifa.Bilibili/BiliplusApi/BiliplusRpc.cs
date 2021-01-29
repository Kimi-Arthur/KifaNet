using System.Net.Http;
using Pimix;

namespace Kifa.Bilibili.BiliplusApi {
    public abstract class BiliplusRpc<TResponse> : JsonRpc<TResponse> {
        public override HttpClient HttpClient { get; } = BilibiliVideo.GetBiliplusClient();
    }
}
