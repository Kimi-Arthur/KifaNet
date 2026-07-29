namespace Kifa.Subtitle.Subcat;

public record SubcatChoice {
    public required Language Language { get; init; }
    public required string OriginalLink { get; init; }
    public string? DownloadLink { get; init; }

    public bool NeedsGeneration => DownloadLink == null;

    public string Title => SubcatClient.ParseSubcatUrl(OriginalLink).Title;

    public override string ToString() => $"[{Language.Code}{(NeedsGeneration ? "*" : "")}] {Title}";
}
