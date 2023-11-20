using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubeVideo : DataModel, WithModelId<YouTubeVideo> {
    public static string ModelId => "youtube/videos";

    public static KifaServiceClient<YouTubeVideo> Client { get; set; } =
        new KifaServiceRestClient<YouTubeVideo>();

    public string? Author { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public long Fps { get; set; }
    public long Height { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Thumbnail { get; set; }
    public string? Title { get; set; }
    public Date? UploadDate { get; set; }
    public long Width { get; set; }

    public override DateTimeOffset? Fill() {
        // Set refresh date and change version when we have modifiable data.
        return null;
    }
}
