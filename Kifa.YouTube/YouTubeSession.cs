using Kifa.Service;

namespace Kifa.YouTube;

public class YouTubeSession : DataModel, WithModelId<YouTubeSession> {
    public static string ModelId => "youtube/sessions";

    public static KifaServiceClient<YouTubeSession> Client { get; set; } =
        new KifaServiceRestClient<YouTubeSession>();

    [ExternalProperty("cookies.txt")]
    public string? Cookies { get; set; }
}
