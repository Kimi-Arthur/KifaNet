using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Subtitle.Subcat;

public class SubcatUploadResponse {
    public string? Echo { get; set; }
    public string? Url { get; set; }
}

public sealed class SubcatUploadRpc : KifaJsonParameterizedRpc<SubcatUploadResponse> {
    protected override string Url => $"{SubcatClient.UrlPrefix}/upload_subtitles.php";

    protected override HttpMethod Method => HttpMethod.Post;

    protected override List<KeyValuePair<string, string>> FormContent
        => [
            new("filename", "{filename}"),
            new("content", "{content}"),
            new("language", "{language}"),
            new("orig_language", "{orig_language}")
        ];

    public SubcatUploadRpc(string filename, string content, string language,
        string originalLanguage = "auto") {
        Parameters = new() {
            { "filename", filename },
            { "content", content },
            { "language", language },
            { "orig_language", originalLanguage }
        };
    }
}
