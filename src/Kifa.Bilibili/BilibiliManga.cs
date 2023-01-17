using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Bilibili.BiliplusApi;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliManga : DataModel, WithModelId {
    public static string ModelId => "bilibili/mangas";

    public static KifaServiceClient<BilibiliManga> Client { get; set; } =
        new KifaServiceRestClient<BilibiliManga>();

    #region public late string Title { get; set; }

    string? title;

    public string Title {
        get => Late.Get(title);
        set => Late.Set(ref title, value);
    }

    #endregion

    public List<string> Authors { get; set; } = new();

    // All covers for the manga. It will be the vertical, horizontal and square ones.
    public List<string> Covers { get; set; } = new();

    public List<string> Styles { get; set; } = new();

    #region public late string Description { get; set; }

    string? description;

    public string Description {
        get => Late.Get(description);
        set => Late.Set(ref description, value);
    }

    #endregion

    public List<BilibiliMangaEpisode> Episodes { get; set; } = new();

    static readonly HttpClient NoAuthClient = new();

    public override DateTimeOffset? Fill() {
        var data = NoAuthClient.Call(new BilibiliMangaRpc(Id[2..]))!.Data;

        Title = data.Title;
        Authors = data.AuthorName;
        Styles = data.Styles;
        Description = data.Evaluate;
        Covers = new() {
            data.VerticalCover,
            data.HorizontalCover,
            data.SquareCover
        };

        var newEpisodes = data.EpList.Select(ep => new BilibiliMangaEpisode {
            Id = ep.Ord,
            Epid = ep.Id.ToString(),
            Title = ep.Title.Trim(),
            ShortTitle = ep.ShortTitle.Trim(),
            Cover = ep.Cover,
            PageCount = ep.ImageCount,
            Size = ep.Size
        }).OrderBy(ep => double.Parse(ep.Id)).ToList();

        for (var i = 0; i < Episodes.Count; i++) {
            JsonConvert.PopulateObject(
                JsonConvert.SerializeObject(newEpisodes[i], KifaJsonSerializerSettings.Default),
                Episodes[i], KifaJsonSerializerSettings.Default);
        }

        Episodes.AddRange(newEpisodes.Skip(Episodes.Count));
        Episodes.ForEach(ep => ep.FillPages(Id));

        return DateTimeOffset.Now + TimeSpan.FromDays(7);
    }

    public IEnumerable<(string desiredName, string canonicalName)>
        GetNames(BilibiliMangaEpisode episode)
        => episode.GetNames($"{Title}-{Id}");

    public IEnumerable<string> GetLinks(BilibiliMangaEpisode episode) => episode.GetDownloadLinks();
}

public class BilibiliMangaEpisode {
    #region public late string Id { get; set; }

    string? id;

    public string Id {
        get => Late.Get(id);
        set => Late.Set(ref id, value);
    }

    #endregion

    #region public late string Epid { get; set; }

    string? epid;

    public string Epid {
        get => Late.Get(epid);
        set => Late.Set(ref epid, value);
    }

    #endregion

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

    public DateTimeOffset LastRefreshed { get; set; } = Date.Zero;

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public void FillPages(string mangaId) {
        if (Pages.Count == PageCount &&
            DateTimeOffset.Now - LastRefreshed <= TimeSpan.FromDays(365)) {
            Logger.Debug(
                $"No need to refresh pages for {mangaId}.{Id}: {epid}. Last refreshed at {LastRefreshed}");
            return;
        }

        Pages = HttpClients.BiliplusHttpClient.Call(new BiliplusMangaEpisodeRpc(mangaId[2..], epid))
            .Select((p, index) => new BilibiliMangaPage {
                Id = index + 1,
                ImageId = p
            }).ToList();
        LastRefreshed = DateTimeOffset.Now;
    }

    static readonly HttpClient NoAuthClient = new();

    public IEnumerable<(string desiredName, string canonicalName)> GetNames(string prefix) {
        var episodePrefix = $"{prefix}/{Id:000.#} {ShortTitle} {Title}".Trim();
        return Pages.Select(p => (
            $"{episodePrefix}/{p.Id:00}{p.ImageId[p.ImageId.LastIndexOf(".")..]}",
            $"$/{p.ImageId}"));
    }

    public IEnumerable<string> GetDownloadLinks()
        => NoAuthClient.Call(new MangaTokenRpc(Pages.Select(p => p.ImageId)))!.Data.Select(token
            => $"{token.Url}?token={token.Token}");
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
}
