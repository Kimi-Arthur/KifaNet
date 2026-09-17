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
    public List<string> Shorts { get; set; } = [];
    public List<string> Streams { get; set; } = [];

    public override void Fill(bool deep = false) {
        var uploader = YouTubeUploader.Resolve(Id.Checked());
        var baseUrl = YouTubeUploader.GetFetchUrl(uploader?.Id ?? Id.Checked()).TrimEnd('/');

        Videos = FetchSection($"{baseUrl}/videos", Videos, deep);
        Shorts = FetchSection($"{baseUrl}/shorts", Shorts, deep);
        Streams = FetchSection($"{baseUrl}/streams", Streams, deep);
    }

    const int BatchSize = 50;

    List<string> FetchSection(string sectionUrl, List<string> currentList, bool deep) {
        if (deep || currentList.Count == 0) {
            return FetchSectionData(sectionUrl);
        }

        var existingSet = currentList.ToHashSet();
        var newVideos = new List<string>();
        var start = 1;

        while (true) {
            var batch = FetchSectionData(sectionUrl, start, BatchSize);
            if (batch.Count == 0) {
                return newVideos;
            }

            var overlapIndex = batch.FindIndex(v => existingSet.Contains(v));
            if (overlapIndex >= 0) {
                newVideos.AddRange(batch.Take(overlapIndex));
                return [.. newVideos, .. currentList];
            }

            newVideos.AddRange(batch);
            start += BatchSize;
        }
    }

    List<string> FetchSectionData(string url, int? start = null, int? count = null) {
        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
        if (start != null && count != null) {
            options.PlaylistItems = $"{start}:{start + count - 1}";
        }

        var result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(url, overrideOptions: options)
            .GetAwaiter().GetResult();

        if (!result.Success || result.Data == null) {
            var error = string.Join("\n", result.ErrorOutput);
            if (error.Contains("does not have a", StringComparison.OrdinalIgnoreCase)) {
                return [];
            }

            if (!Id.StartsWith("http") && !Id.StartsWith("@") && !Id.StartsWith("UC")) {
                var tab = url.Split('/').Last();
                url = $"https://www.youtube.com/user/{Id}/{tab}";
                result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(url, overrideOptions: options)
                    .GetAwaiter().GetResult();
                if (result.Success && result.Data != null) {
                    return YouTubeVideo.ExtractVideoIds(result.Data.Entries);
                }

                error = string.Join("\n", result.ErrorOutput);
                if (error.Contains("does not have a", StringComparison.OrdinalIgnoreCase)) {
                    return [];
                }
            }

            if (url.EndsWith("/videos")) {
                throw new DataNotFoundException(
                    $"Failed to retrieve videos for uploader ({Id}) from {url}: {error}");
            }

            return [];
        }

        return YouTubeVideo.ExtractVideoIds(result.Data.Entries);
    }
}
