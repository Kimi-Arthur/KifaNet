namespace Kifa.Subtitle.Subcat;

public record SubcatChoice {
    public required Language Language { get; init; }
    public required string OriginalLink { get; init; }
    public string? DownloadLink { get; init; }

    public string? SourceLanguage { get; init; }
    public string? Size { get; init; }
    public int DownloadCount { get; init; }
    public int LanguageCount { get; init; }

    public bool NeedsGeneration => DownloadLink == null;

    public string Title => SubcatClient.ParseSubcatUrl(OriginalLink).Title;

    public override string ToString() {
        var sourceLangSegment = string.FormatOrEmpty($" (translated from {SourceLanguage})");
        var sizeSegment = string.FormatOrEmpty($"{Size}, ");
        return
            $"[{Language.Code}{(NeedsGeneration ? "*" : "")}] {Title}{sourceLangSegment} ({sizeSegment}{DownloadCount} downloads, {LanguageCount} languages)";
    }
}
