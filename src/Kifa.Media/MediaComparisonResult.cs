using System.Collections.Generic;

namespace Kifa.Media;

public class MediaComparisonResult {
    public string File1Path { get; set; } = "";
    public string File2Path { get; set; } = "";
    public long File1Size { get; set; }
    public long File2Size { get; set; }

    // 0) Whether they match bit by bit
    public bool IsBitExactMatch { get; set; }
    public string? File1Sha256 { get; set; }
    public string? File2Sha256 { get; set; }

    // 1) Whether stream/content matches
    public bool IsContentMatch { get; set; }
    public ContentMatchLevel MatchLevel { get; set; } = ContentMatchLevel.NoMatch;
    public List<StreamComparisonResult> Streams { get; set; } = [];

    // 2) If they match, what fields don't
    public List<MetadataFieldDifference> Differences { get; set; } = [];

    // All metadata differences regardless of whether content matches
    public List<MetadataFieldDifference> AllDifferences { get; set; } = [];
}
