using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubeUploaderVideos : DataModel, WithModelId<YouTubeUploaderVideos> {
    public static string ModelId => "youtube/uploader_videos";

    public static KifaServiceClient<YouTubeUploaderVideos> Client { get; set; } =
        new KifaServiceRestClient<YouTubeUploaderVideos>();

    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(1);

    public List<string> Videos { get; set; } = [];

    public override void Fill() {
        var uploader = YouTubeUploader.Get(Id.Checked());
        var url = YouTubeUploader.GetFetchUrl(uploader?.Id ?? Id.Checked());

        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
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

        Videos = YouTubeVideo.ExtractVideoIds(result.Data.Entries);
    }
}
