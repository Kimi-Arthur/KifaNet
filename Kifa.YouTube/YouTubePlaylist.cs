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
    public List<string> Videos { get; set; } = new();

    const int BatchSize = 50;

    public override void Fill(bool deep = false) {
        var playlistUrl = Id.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? Id
            : $"https://www.youtube.com/playlist?list={Id}";

        if (deep || Videos.Count == 0) {
            var (title, author, videos) = FetchPlaylistData(playlistUrl);
            Title = title;
            Author = author;
            Videos = videos;
            return;
        }

        var existingSet = Videos.ToHashSet();
        var newVideos = new List<string>();
        var start = 1;

        while (true) {
            var (title, author, batch) = FetchPlaylistData(playlistUrl, start, BatchSize);
            if (start == 1) {
                Title = title;
                Author = author;
            }

            if (batch.Count == 0) {
                Videos = newVideos;
                break;
            }

            var overlapIndex = batch.FindIndex(v => existingSet.Contains(v));
            if (overlapIndex >= 0) {
                newVideos.AddRange(batch.Take(overlapIndex));
                Videos = [..newVideos, ..Videos];
                break;
            }

            newVideos.AddRange(batch);
            start += BatchSize;
        }
    }

    static (string? title, string? author, List<string> videos) FetchPlaylistData(
        string playlistUrl, int? start = null, int? count = null) {
        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
        if (start != null && count != null) {
            options.PlaylistItems = $"{start}:{start + count - 1}";
        }

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
