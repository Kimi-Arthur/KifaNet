using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class MangaTokenRpc : KifaJsonParameterizedRpc<MangaTokenResponse> {
    protected override string Url
        => "https://manga.bilibili.com/twirp/comic.v1.Comic/ImageToken?device=pc&platform=web";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override string JsonContent => "{\"urls\":\"[{urls}]\"}";

    public MangaTokenRpc(IEnumerable<string> imageIds) {
        Parameters = new () {
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
