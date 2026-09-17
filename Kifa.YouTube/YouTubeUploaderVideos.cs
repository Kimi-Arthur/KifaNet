using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubeUploaderVideos : DataModel, WithModelId<YouTubeUploaderVideos> {
    public static string ModelId => "youtube/uploader_videos";

    public static KifaServiceClient<YouTubeUploaderVideos> Client { get; set; } =
        new KifaServiceRestClient<YouTubeUploaderVideos>();

    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(1);

    public List<string> Videos { get; set; } = [];

    const int BatchSize = 50;

    public override void Fill(bool deep = false) {
        var uploader = YouTubeUploader.Get(Id.Checked());
        var url = YouTubeUploader.GetFetchUrl(uploader?.Id ?? Id.Checked());

        if (deep || Videos.Count == 0) {
            Videos = FetchVideos(url);
            return;
        }

        var existingSet = Videos.ToHashSet();
        var newVideos = new List<string>();
        var start = 1;

        while (true) {
            var batch = FetchVideos(url, start, BatchSize);
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

    List<string> FetchVideos(string url, int? start = null, int? count = null) {
        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
        if (start != null && count != null) {
            options.PlaylistItems = $"{start}:{start + count - 1}";
        }

        var result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(url, overrideOptions: options)
            .GetAwaiter().GetResult();

        if (!result.Success || result.Data == null) {
            if (!Id.StartsWith("http") && !Id.StartsWith("@") && !Id.StartsWith("UC")) {
                url = $"https://www.youtube.com/user/{Id}";
                result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(url, overrideOptions: options)
                    .GetAwaiter().GetResult();
            }

            if (!result.Success || result.Data == null) {
                throw new DataNotFoundException(
                    $"Failed to retrieve videos for uploader ({Id}): {string.Join("\n", result.ErrorOutput)}");
            }
        }

        return YouTubeVideo.ExtractVideoIds(result.Data.Entries);
    }
}
