using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi {
    public abstract class BilibiliRpc<TResponse> : JsonRpc<TResponse> {
        public override HttpClient HttpClient { get; set; } = BilibiliVideo.GetBilibiliClient();
    }
}
