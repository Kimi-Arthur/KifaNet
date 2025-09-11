using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubeVideo : DataModel, WithModelId<YouTubeVideo> {
    public static string ModelId => "youtube/videos";

    public static KifaServiceClient<YouTubeVideo> Client { get; set; } =
        new KifaServiceRestClient<YouTubeVideo>();

    public string? Title { get; set; }
    public string? Author { get; set; }
    public Date? UploadDate { get; set; }
    public string? Description { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public TimeSpan Duration { get; set; }
    public long Fps { get; set; }
    public long Width { get; set; }
    public long Height { get; set; }
    public string? FormatId { get; set; }
    public string? Thumbnail { get; set; }

    public override DateTimeOffset? Fill() {
        // Set refresh date and change version when we have modifiable data.
        return null;
    }

    public List<string> GetCanonicalNames(string? formatId = null)
        => [$"{Id}.{formatId ?? FormatId}", Id];

    public string GetDesiredName(string? formatId = null)
        => $"{Author.NormalizeFileName()}/{Title.NormalizeFileName()}.{Id}.{formatId ?? FormatId}";
}
