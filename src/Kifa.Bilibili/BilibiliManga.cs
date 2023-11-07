using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Bilibili;

public class BilibiliManga : DataModel, WithModelId<BilibiliManga> {
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

    public List<Link<BilibiliMangaEpisode>> Episodes { get; set; } = new();

    static readonly HttpClient NoAuthClient = new();

    public override DateTimeOffset? Fill() {
        var data = NoAuthClient.Call(new MangaRpc(Id[2..]))!.Data;

        Title = data.Title;
        Authors = data.AuthorName;
        Styles = data.Styles;
        Description = data.Evaluate;
        Covers = new() {
            data.VerticalCover,
            data.HorizontalCover,
            data.SquareCover
        };

        foreach (var ep in data.EpList) {
            BilibiliMangaEpisode.Client.Update(new BilibiliMangaEpisode {
                Index = ep.Ord,
                Id = $"{Id}/{ep.Id}",
                Title = ep.Title.Trim(),
                ShortTitle = ep.ShortTitle.Trim(),
                Cover = ep.Cover,
                PageCount = ep.ImageCount,
                Size = ep.Size
            });
        }

        var newEpisodes = BilibiliMangaEpisode.Client
            .Get(data.EpList.Select(ep => $"{Id}/{ep.Id}").ToList()).ExceptNull()
            .OrderBy(ep => ep.Index).ToList();

        for (var i = 0; i < Episodes.Count; i++) {
            if (newEpisodes[i].Id != Episodes[i].Id) {
                throw new UnableToFillException("Episode ids mismatch unexpectedly.");
            }

            JsonConvert.PopulateObject(
                JsonConvert.SerializeObject(newEpisodes[i], KifaJsonSerializerSettings.Default),
                Episodes[i], KifaJsonSerializerSettings.Default);
        }

        Episodes.AddRange(newEpisodes.Skip(Episodes.Count)
            .Select(ep => (Link<BilibiliMangaEpisode>) ep));

        return DateTimeOffset.Now + TimeSpan.FromDays(7);
    }

    public IEnumerable<(string desiredName, string canonicalName)>
        GetNames(BilibiliMangaEpisode episode)
        => episode.GetNames($"{Title}-{Id}");

    public IEnumerable<string> GetLinks(BilibiliMangaEpisode episode) => episode.GetDownloadLinks();
}
