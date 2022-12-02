using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Kifa.Rpc;

namespace Kifa.Bilibili.BiliplusApi;

public class BiliplusMangaEpisodeRpc : KifaParameterizedRpc<List<string>> {
    public override string UrlPattern
        => "https://www.biliplus.com/manga/?act=read&mangaid={manga_id}&epid={epid}";

    public override HttpMethod Method { get; } = HttpMethod.Get;

    public BiliplusMangaEpisodeRpc(string mangaId, string epid) {
        parameters = new Dictionary<string, string> {
            { "manga_id", mangaId },
            { "epid", epid }
        };
    }

    static readonly Regex ImageLinkPattern = new(@"bfs/manga/([^@]*)@");

    public override List<string> ParseResponse(HttpResponseMessage responseMessage) {
        var matches = ImageLinkPattern.Matches(responseMessage.GetString());
        return matches.Select(m => m.Groups[1].Value).ToList();
    }
}
