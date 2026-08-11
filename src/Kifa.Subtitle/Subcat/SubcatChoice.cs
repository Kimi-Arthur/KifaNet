using System.Linq;

namespace Kifa.Subtitle.Subcat;

public record SubcatChoice {
    public required string OriginalLink { get; init; }
    public string? OriginalContent { get; set; }
    public string? Preview { get; init; }

    public string? SourceLanguage { get; init; }
    public string? Size { get; init; }
    public int DownloadCount { get; init; }
    public int LanguageCount { get; init; }

    public string Title => SubcatClient.ParseSubcatUrl(OriginalLink).Title;

    public override string ToString() {
        var sourceLangSegment = string.FormatOrEmpty($" (from {SourceLanguage})");
        var sizeSegment = string.FormatOrEmpty($"{Size}, ");
        var previewSegment = "";
        if (!string.IsNullOrEmpty(Preview)) {
            var indentedLines = Preview.Split('\n').Select(line => $"\t{line}");
            previewSegment = "\n" + string.Join("\n", indentedLines);
        }

        return
            $"{Title}{sourceLangSegment} ({sizeSegment}{DownloadCount} downloads): {OriginalLink}{previewSegment}";
    }
}
