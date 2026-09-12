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

    public static YouTubeUploader? Get(string id, bool refresh = false) {
        if (string.IsNullOrEmpty(id)) {
            return null;
        }

        var uploader = Client.Get(id, refresh: refresh);
        if (uploader != null) {
            uploader.Id = uploader.RealId;
            return uploader;
        }

        if (!id.StartsWith(VirtualItemPrefix)) {
            uploader = Client.Get(VirtualItemPrefix + id, refresh: refresh);
            if (uploader != null) {
                uploader.Id = uploader.RealId;
                return uploader;
            }
        }

        if (!id.StartsWith("@") && !id.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            uploader = Client.Get("@" + id, refresh: refresh);
            if (uploader != null) {
                uploader.Id = uploader.RealId;
                return uploader;
            }
        }

        return null;
    }

    public string? Name { get; set; }
    public HashSet<string> NameAliases { get; set; } = [];
    public string? ChannelId { get; set; }
    public List<string> Videos { get; set; } = [];

    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(365);

    public string GetUploaderFolder()
        => $"{Name.Checked().NormalizeFileName().Choppable()}.{Id}.youtube";

    public override SortedSet<string> GetVirtualItems() {
        var items = new SortedSet<string>();

        if (!string.IsNullOrEmpty(ChannelId)) {
            items.Add(VirtualItemPrefix + ChannelId);
        }

        var bareHandle = Id?.TrimStart('@');
        if (!string.IsNullOrEmpty(bareHandle) && bareHandle != Id) {
            items.Add(VirtualItemPrefix + bareHandle);
        }

        var allNames = new HashSet<string>(NameAliases);
        if (Name != null) {
            allNames.Add(Name);
        }

        foreach (var name in allNames) {
            if (!string.IsNullOrEmpty(name) && name != Id && name != bareHandle && name != ChannelId) {
                items.Add(VirtualItemPrefix + name);
            }
        }

        return items;
    }

    public override void Fill() {
        var url = GetFetchUrl(Id.Checked());

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

        var data = result.Data;
        ChannelId = data.ChannelID;

        var fetchedName = data.Uploader ?? data.Channel ?? data.Title;
        if (fetchedName != null) {
            if (Name == null) {
                Name = fetchedName;
            } else if (Name != fetchedName) {
                NameAliases.Add(fetchedName);
            }
        }

        Videos = YouTubeVideo.ExtractVideoIds(data.Entries);
    }

    static string GetFetchUrl(string id) {
        if (id.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
            return id;
        }

        if (id.StartsWith("@")) {
            return $"https://www.youtube.com/{id}";
        }

        if (id.StartsWith("UC", StringComparison.OrdinalIgnoreCase)) {
            return $"https://www.youtube.com/channel/{id}";
        }

        return $"https://www.youtube.com/@{id}";
    }
}
