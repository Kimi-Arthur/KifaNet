using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;

namespace Kifa.Bilibili;

public class BilibiliManga : DataModel {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient NoAuthClient = new HttpClient();

    public const string ModelId = "bilibili/mangas";

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

    public override DateTimeOffset? Fill() {
        var data =
            NoAuthClient.SendWithRetry<BilibiliMangaResponse>(new BilibiliMangaRequest(Id[2..]))!
                .Data;

        Title = data.Title;
        Authors = data.AuthorName;
        Styles = data.Styles;
        Description = data.Evaluate;

        var newEpisodes = data.EpList.Select(ep => new BilibiliMangaEpisode {
            Id = ep.Ord,
            Epid = ep.Id.ToString(),
            Title = ep.Title.Trim(),
            ShortTitle = ep.ShortTitle.Trim(),
            Cover = ep.Cover,
            PageCount = ep.ImageCount,
            Size = ep.Size
        }).OrderBy(ep => ep.Id).ToList();

        for (var i = 0; i < Episodes.Count; i++) {
            JsonConvert.PopulateObject(
                JsonConvert.SerializeObject(newEpisodes[i], Defaults.JsonSerializerSettings),
                Episodes[i], Defaults.JsonSerializerSettings);
        }

        Episodes.AddRange(newEpisodes.Skip(Episodes.Count));

        return DateTimeOffset.Now + TimeSpan.FromDays(7);
    }
}

public class BilibiliMangaEpisode {
    public double Id { get; set; }

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

    public void FillPages() {
        // TODO
    }
}

public class BilibiliMangaPage {
    public int Id { get; set; }

    public string? ImageId { get; set; }
}
