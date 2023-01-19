using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Bilibili.BiliplusApi;
using Kifa.Service;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliMangaEpisode : DataModel, WithModelId {
    public static string ModelId => "bilibili/manga_episodes";

    public static KifaServiceClient<BilibiliMangaEpisode> Client { get; set; } =
        new KifaServiceRestClient<BilibiliMangaEpisode>();

    public string MangaId => Id.Split("/")[0];

    public string EpisodeId => Id.Split("/")[1];

    public decimal Index { get; set; }

    public string Title { get; set; } = "";

    public string ShortTitle { get; set; } = "";

    #region public late string Cover { get; set; }

    string? cover;

    public string Cover {
        get => Late.Get(cover);
        set => Late.Set(ref cover, value);
    }

    #endregion

    public long Size { get; set; }

    public int PageCount { get; set; }

    public List<BilibiliMangaPage> Pages { get; set; } = new();

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override DateTimeOffset? Fill() {
        Pages = HttpClients.BiliplusHttpClient
            .Call(new BiliplusMangaEpisodeRpc(MangaId[2..], EpisodeId)).Select((p, index)
                => new BilibiliMangaPage {
                    Id = index + 1,
                    ImageId = p
                }).ToList();
        return DateTimeOffset.Now + TimeSpan.FromDays(365);
    }

    static readonly HttpClient NoAuthClient = new();

    public IEnumerable<(string desiredName, string canonicalName)> GetNames(string prefix) {
        var episodePrefix = $"{prefix}/{Index:000.#} {ShortTitle} {Title}".Trim();
        return Pages.Select(p => (
            $"{episodePrefix}/{p.Id:00}{p.ImageId[p.ImageId.LastIndexOf(".")..]}",
            $"$/{p.ImageId}"));
    }

    public IEnumerable<string> GetDownloadLinks()
        => NoAuthClient.Call(new MangaTokenRpc(Pages.Select(p => p.ImageId)))!.Data.Select(token
            => $"{token.Url}?token={token.Token}");

    public KifaActionResult MarkDoublePages(HashSet<int> ids) {
        var count = 0;
        foreach (var page in Pages) {
            if (ids.Contains(page.Id)) {
                page.DoublePage = true;
                count++;
            }
        }

        if (count != ids.Count) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = $"Only found {count} pages to mark, instead of {ids.Count}."
            };
        }

        Client.Set(this);
        return KifaActionResult.Success;
    }
}

public class BilibiliMangaPage {
    public int Id { get; set; }

    #region public late string ImageId { get; set; }

    string? imageId;

    public string ImageId {
        get => Late.Get(imageId);
        set => Late.Set(ref imageId, value);
    }

    #endregion

    public bool DoublePage { get; set; }
}
