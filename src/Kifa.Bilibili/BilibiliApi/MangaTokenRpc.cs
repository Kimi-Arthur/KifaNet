using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class MangaTokenRpc : KifaJsonParameterizedRpc<MangaTokenResponse> {
    public override string UrlPattern
        => "https://manga.bilibili.com/twirp/comic.v1.Comic/ImageToken?device=pc&platform=web";

    public override HttpMethod Method => HttpMethod.Post;

    public override string JsonContent => "{\"urls\":\"[{urls}]\"}";

    public MangaTokenRpc(List<string> imageIds) {
        parameters = new Dictionary<string, string> {
            { "urls", string.Join(",", imageIds.Select(id => $"\\\"/bfs/manga/{id}\\\"")) }
        };
    }
}

public class MangaTokenResponse {
    public long Code { get; set; }
    public string Msg { get; set; }
    public List<MangaImageLink> Data { get; set; }
}

public partial class MangaImageLink {
    public string Url { get; set; }
    public string Token { get; set; }
}
