using System.Net.Http;
using Pimix;

namespace Kifa.Bilibili.BilibiliApi {
    public abstract class BilibiliRpc<TResponse> : JsonRpc<TResponse> {
        public override HttpClient HttpClient { get; } = BilibiliVideo.GetBilibiliClient();
    }
}