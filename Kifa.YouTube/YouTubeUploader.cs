using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using YoutubeDLSharp.Options;

namespace Kifa.YouTube;

public class YouTubeUploader : DataModel, WithModelId<YouTubeUploader> {
    public static string ModelId => "youtube/uploaders";

    public static KifaServiceClient<YouTubeUploader> Client { get; set; } =
        new KifaServiceRestClient<YouTubeUploader>();

    public string? Name { get; set; }
    public List<string> Videos { get; set; } = new();

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        string url;
        if (Id.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            url = Id;
        } else if (Id.StartsWith("@")) {
            url = $"https://www.youtube.com/{Id}/videos";
        } else if (Id.StartsWith("UC", StringComparison.OrdinalIgnoreCase)) {
            url = $"https://www.youtube.com/channel/{Id}/videos";
        } else {
            url = $"https://www.youtube.com/user/{Id}/videos";
        }

        var options = YouTubeVideo.GetOptionSet(flatPlaylist: true);
        var result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(url, overrideOptions: options)
            .GetAwaiter().GetResult();

        if (!result.Success || result.Data == null) {
            if (!Id.StartsWith("http") && !Id.StartsWith("@") && !Id.StartsWith("UC")) {
                url = $"https://www.youtube.com/@{Id}/videos";
                result = YouTubeVideo.YoutubeDL.RunVideoDataFetch(url, overrideOptions: options)
                    .GetAwaiter().GetResult();
            }

            if (!result.Success || result.Data == null) {
                throw new DataNotFoundException(
                    $"Failed to retrieve videos for uploader ({Id}): {string.Join("\n", result.ErrorOutput)}");
            }
        }

        Name = result.Data.Uploader ?? result.Data.Channel ?? result.Data.Title;
        Videos = result.Data.Entries?
            .Select(e => e.ID)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList() ?? new List<string>();

        return DateTimeOffset.UtcNow + TimeSpan.FromDays(365);
    }
}
