using System.Collections.Generic;
using System.Linq;

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

    public string ToOneLineString(bool allFields = false) {
        if (IsBitExactMatch) {
            return "Bit-Exact: Files are 100% bit-exact identical.";
        }

        var diffsToShow = IsContentMatch || allFields ? AllDifferences : [];
        var diffSummary = diffsToShow.Count > 0
            ? string.Join(", ", diffsToShow.Select(FormatDiff))
            : "";

        if (IsContentMatch) {
            var levelDesc = MatchLevel switch {
                ContentMatchLevel.BitstreamMatch => "Bitstream Match",
                ContentMatchLevel.DecodedMatch => "Decoded Match",
                _ => "Content Match"
            };

            return diffsToShow.Count > 0
                ? $"{levelDesc}: {diffSummary}"
                : $"{levelDesc}: All metadata fields match.";
        }

        var mismatchedStreams = Streams.Where(s => !s.IsMatch)
            .Select(s => $"Stream #{s.Index} [{s.StreamType}]")
            .ToList();

        var mismatchDesc = mismatchedStreams.Count > 0
            ? $"Mismatched {string.Join(", ", mismatchedStreams)}"
            : "Media streams differ";

        return diffsToShow.Count > 0
            ? $"No Match: {mismatchDesc}; Diffs: {diffSummary}"
            : $"No Match: {mismatchDesc}";
    }

    static string FormatDiff(MetadataFieldDifference diff) {
        var v1 = diff.File1Value != null ? $"\"{diff.File1Value}\"" : "(missing)";
        var v2 = diff.File2Value != null ? $"\"{diff.File2Value}\"" : "(missing)";
        return $"{diff.FullName}: {v1} vs {v2}";
    }
}
