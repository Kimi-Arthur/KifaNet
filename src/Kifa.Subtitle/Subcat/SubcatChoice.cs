using System;

namespace Kifa.Subtitle.Subcat;

public record SubcatChoice {
    public string? Id { get; init; }
    public required string Title { get; init; }
    public required Language Language { get; init; }
    public bool NeedsGeneration { get; init; }

    public string DownloadLink
        => $"{SubcatClient.UrlPrefix}/subs/{string.FormatOrEmpty($"{Id}/")}{Uri.EscapeDataString(Title)}-{SubcatClient.GetSubcatLanguage(Language)}.srt";

    public string GenerateLink
        => $"{SubcatClient.UrlPrefix}/subs/{string.FormatOrEmpty($"{Id}/")}{Uri.EscapeDataString(Title)}-orig.srt";

    public override string ToString()
        => $"[{Language.Code}{(NeedsGeneration ? "*" : "")}] {Title}";
}
