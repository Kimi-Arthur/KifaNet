using System;
using System.Collections.Generic;
using System.Linq;
using Kifa;

namespace Kifa.Media;

public class MediaComparisonResult {
    public string File1Path { get; set; } = "";
    public string File2Path { get; set; } = "";
    public long File1Size { get; set; }
    public long File2Size { get; set; }

    // File validity & integrity
    public bool File1Valid { get; set; } = true;
    public bool File2Valid { get; set; } = true;
    public List<string> File1Errors { get; set; } = [];
    public List<string> File2Errors { get; set; } = [];

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
        var sections = new List<string>();

        // 1. Content / Match status
        if (IsBitExactMatch) {
            sections.Add("BIT-EXACT".Info());
        } else if (IsContentMatch) {
            var levelDesc = MatchLevel switch {
                ContentMatchLevel.BitstreamMatch => "BITSTREAM MATCH",
                ContentMatchLevel.DecodedMatch => "DECODED MATCH",
                _ => "CONTENT MATCH"
            };
            sections.Add(levelDesc.Info());
        } else {
            var mismatched = Streams.Where(s => !s.IsMatch)
                .Select(s => $"Stream #{s.Index} [{s.StreamType}]")
                .ToList();
            var mismatchDesc = mismatched.Count > 0
                ? $"NO MATCH ({string.Join(", ", mismatched)})"
                : "NO MATCH";
            sections.Add(mismatchDesc.Fatal());
        }

        // 2. Integrity / Validity status
        if (File1Valid && File2Valid) {
            sections.Add("Integrity: Valid".Info());
        } else if (!File1Valid && !File2Valid) {
            var err1 = SummarizeErrors(File1Errors);
            var err2 = SummarizeErrors(File2Errors);
            var errDesc = err1 == err2 ? err1 : $"F1: {err1}; F2: {err2}";
            sections.Add($"Integrity: Both INVALID ({errDesc})".Fatal());
        } else if (!File1Valid) {
            sections.Add($"Integrity: File 1 INVALID ({SummarizeErrors(File1Errors)})".Fatal());
        } else {
            sections.Add($"Integrity: File 2 INVALID ({SummarizeErrors(File2Errors)})".Fatal());
        }

        // 3. Metadata / Details status
        if (IsBitExactMatch) {
            sections.Add("100% bit-exact identical".Info());
        } else if (IsContentMatch) {
            if (AllDifferences.Count == 0) {
                sections.Add("Metadata: Match".Info());
            } else {
                var diffList = AllDifferences.Take(3).Select(FormatDiffSummary).ToList();
                var diffStr = string.Join(", ", diffList);
                if (AllDifferences.Count > 3) {
                    diffStr += ", ...";
                }

                sections.Add($"Diffs ({AllDifferences.Count}): {diffStr}".Warn());
            }
        } else if (allFields) {
            if (AllDifferences.Count == 0) {
                sections.Add("Metadata: Match".Info());
            } else {
                var diffList = AllDifferences.Take(3).Select(FormatDiffSummary).ToList();
                var diffStr = string.Join(", ", diffList);
                if (AllDifferences.Count > 3) {
                    diffStr += ", ...";
                }

                sections.Add($"Diffs ({AllDifferences.Count}): {diffStr}".Warn());
            }
        }

        return string.Join(" | ", sections);
    }

    static string SummarizeErrors(List<string> errors) {
        if (errors.Count == 0) {
            return "Corrupted";
        }

        var summaries = new List<string>();
        foreach (var err in errors) {
            if (err.Contains("EOI", StringComparison.OrdinalIgnoreCase)) {
                if (!summaries.Contains("Missing JPEG EOI")) {
                    summaries.Add("Missing JPEG EOI");
                }
            } else if (err.Contains("CRC mismatch", StringComparison.OrdinalIgnoreCase)) {
                if (!summaries.Contains("CRC mismatch")) {
                    summaries.Add("CRC mismatch");
                }
            } else if (err.Contains("overread", StringComparison.OrdinalIgnoreCase)) {
                if (!summaries.Contains("Truncated bitstream")) {
                    summaries.Add("Truncated bitstream");
                }
            } else if (err.Contains("invalid len", StringComparison.OrdinalIgnoreCase)) {
                if (!summaries.Contains("Invalid segment length")) {
                    summaries.Add("Invalid segment length");
                }
            }
        }

        if (summaries.Count > 0) {
            return string.Join(", ", summaries);
        }

        var first = errors[0];
        return first.Length > 35 ? first[..32] + "..." : first;
    }

    static string FormatDiffSummary(MetadataFieldDifference diff) {
        if (diff.File1Value == null && diff.File2Value != null) {
            return $"{diff.Name} (missing in File 1)";
        }

        if (diff.File1Value != null && diff.File2Value == null) {
            return $"{diff.Name} (missing in File 2)";
        }

        return $"{diff.Name} (\"{Truncate(diff.File1Value)}\" vs \"{Truncate(diff.File2Value)}\")";
    }

    static string Truncate(string? val, int maxLen = 15) {
        if (val == null) {
            return "";
        }

        return val.Length <= maxLen ? val : val[..(maxLen - 3)] + "...";
    }
}
