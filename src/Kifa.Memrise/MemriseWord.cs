using System.Collections.Generic;
using System.Net.Http;
using Kifa.Service;

namespace Kifa.Memrise;

public class MemriseWord : DataModel, WithModelId<MemriseWord> {
    public static string ModelId => "memrise/words";

    public static KifaServiceClient<MemriseWord> Client { get; set; } =
        new KifaServiceRestClient<MemriseWord>();

    public Dictionary<string, string> Data { get; set; }

    public List<MemriseAudio>? Audios { get; set; }

    HttpClient httpClient;

    HttpClient HttpClient {
        get {
            if (httpClient == null) {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("cookie", MemriseClient.Cookies);
                httpClient.DefaultRequestHeaders.Add("x-csrftoken", MemriseClient.CsrfToken);
                httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                // httpClient.DefaultRequestHeaders.Add("referer", BaseUrl);
            }

            return httpClient;
        }
    }

    public void FillAudios() {
        if (Audios == null) {
            return;
        }

        foreach (var audio in Audios) {
            if (audio.Md5 != null) {
                continue;
            }

            var response = HttpClient.GetHeaders(audio.Link);
            audio.Size = response.Content.Headers.ContentRange?.Length ?? 0;
            audio.Md5 = response.Headers.ETag?.Tag.ToUpperInvariant()[1..^1];
        }
    }
}

public class MemriseAudio {
    #region public late string Link { get; set; }

    string? link;

    public string Link {
        get => Late.Get(link);
        set => Late.Set(ref link, value);
    }

    #endregion

    public long Size { get; set; }
    public string? Md5 { get; set; }
}
