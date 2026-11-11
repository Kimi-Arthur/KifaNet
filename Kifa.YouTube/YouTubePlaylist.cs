using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubePlaylist : DataModel, WithModelId<YouTubePlaylist> {
    public static string ModelId => "youtube/playlists";

    public static KifaServiceClient<YouTubePlaylist> Client { get; set; } =
        new KifaServiceRestClient<YouTubePlaylist>();

    public string? Title { get; set; }
    public string? Author { get; set; }
    public List<string> Videos { get; set; } = [];
    public List<string> RemovedVideos { get; set; } = [];

    public override void Fill(bool deep = false) {
        var playlistUrl = Id.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? Id
            : $"https://www.youtube.com/playlist?list={Id}";

        var (title, author, videos) = FetchPlaylistData(playlistUrl);
        Title = title;
        Author = author;
        UpdateVideos(videos);
    }

    public void UpdateVideos(List<string> videos) {
        var currentSet = videos.ToHashSet();
        RemovedVideos = RemovedVideos.Concat(Videos)
            .Where(v => !currentSet.Contains(v))
            .Distinct()
            .ToList();
        Videos = videos;
    }

    static (string? title, string? author, List<string> videos) FetchPlaylistData(string playlistUrl) {
        using var lease = YouTubeAccount.ObtainLease(YouTubeSessionType.Quick);
        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true, cookieFile: lease?.CookieFile);

        var result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(playlistUrl, overrideOptions: options)
            .GetAwaiter().GetResult();

        if (!result.Success || result.Data == null) {
            throw new DataNotFoundException(
                $"Failed to find playlist ({playlistUrl}): {string.Join("\n", result.ErrorOutput)}");
        }

        return (result.Data.Title, result.Data.Uploader ?? result.Data.Channel,
            YouTubeVideo.ExtractVideoIds(result.Data.Entries));
    }
}
