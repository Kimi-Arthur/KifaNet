using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using YoutubeDLSharp.Options;

namespace Kifa.YouTube;

public class YouTubePlaylist : DataModel, WithModelId<YouTubePlaylist> {
    public static string ModelId => "youtube/playlists";

    public static KifaServiceClient<YouTubePlaylist> Client { get; set; } =
        new KifaServiceRestClient<YouTubePlaylist>();

    public string? Title { get; set; }
    public string? Author { get; set; }
    public List<string> Videos { get; set; } = new();


    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(365);

    public override void Fill() {
        var playlistUrl = Id.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? Id
            : $"https://www.youtube.com/playlist?list={Id}";

        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
        var result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(playlistUrl, overrideOptions: options)
            .GetAwaiter().GetResult();

        if (!result.Success || result.Data == null) {
            throw new DataNotFoundException(
                $"Failed to find playlist ({Id}): {string.Join("\n", result.ErrorOutput)}");
        }

        Title = result.Data.Title;
        Author = result.Data.Uploader ?? result.Data.Channel;
        Videos = result.Data.Entries?
            .Select(e => e.ID)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList() ?? new List<string>();
    }
}
