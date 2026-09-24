using System;
using System.Collections.Generic;
using System.Linq;

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

    public string ToMultiLineString(bool allFields = false) {
        var lines = new List<string>();
        lines.Add("Media Comparison Result");
        lines.Add("=======================");
        lines.Add($"File 1: {File1Path} ({File1Size:N0} bytes)");
        lines.Add($"File 2: {File2Path} ({File2Size:N0} bytes)");
        lines.Add("");

        // 0) File Integrity / Validity
        lines.Add("[0] File Integrity / Validity:");
        if (File1Valid && File2Valid) {
            lines.Add($"    {"VALID".Info()}: Both files passed structural and decoding integrity checks.");
        } else {
            if (!File1Valid) {
                lines.Add($"    {"File 1 INVALID".Fatal()} ({File1Errors.Count} issue(s)):");
                foreach (var err in File1Errors) {
                    lines.Add($"        - {err.Fatal()}");
                }
            } else {
                lines.Add($"    File 1: {"VALID".Info()}");
            }

            if (!File2Valid) {
                lines.Add($"    {"File 2 INVALID".Fatal()} ({File2Errors.Count} issue(s)):");
                foreach (var err in File2Errors) {
                    lines.Add($"        - {err.Fatal()}");
                }
            } else {
                lines.Add($"    File 2: {"VALID".Info()}");
            }
        }

        lines.Add("");

        // 1) Bit-by-bit match
        lines.Add("[1] Bit-by-Bit Match:");
        if (IsBitExactMatch) {
            lines.Add($"    {"MATCH".Info()}: Files are 100% bit-exact identical.");
            lines.Add($"    SHA-256: {File1Sha256}");
        } else {
            lines.Add($"    {"NO MATCH".Warn()}: File binary hashes differ.");
            lines.Add($"    File 1 SHA-256: {File1Sha256}");
            lines.Add($"    File 2 SHA-256: {File2Sha256}");
        }

        lines.Add("");

        // 2) Stream / Content match
        lines.Add("[2] Stream / Content Match:");
        if (IsContentMatch) {
            var levelDesc = MatchLevel switch {
                ContentMatchLevel.BitExact => "Bit-Exact (Identical files)",
                ContentMatchLevel.BitstreamMatch =>
                    "Bitstream Match (Compressed media bitstreams are identical; container/metadata differs)",
                ContentMatchLevel.DecodedMatch =>
                    "Decoded Match (Decoded frames/samples are identical)",
                _ => "Match"
            };
            lines.Add($"    {"MATCH".Info()}: {levelDesc}");
            if (!File1Valid || !File2Valid) {
                lines.Add(
                    $"    {"WARNING".Warn()}: Content matches, but one or more files have integrity/corruption issues (see [0]).");
            }

            foreach (var stream in Streams) {
                var streamDetails = stream.Details != null ? $" ({stream.Details})" : "";
                var matchType = stream.IsBitstreamMatch ? "Bitstream" : "Decoded";
                var hash = stream.IsBitstreamMatch
                    ? stream.File1BitstreamHash
                    : stream.File1DecodedHash;
                lines.Add(
                    $"    - Stream #{stream.Index} [{stream.StreamType}]{streamDetails}: {"MATCH".Info()} ({matchType} SHA-256: {hash})");
            }
        } else {
            lines.Add($"    {"NO MATCH".Fatal()}: Media streams or content differ.");
            foreach (var stream in Streams) {
                var streamDetails = stream.Details != null ? $" ({stream.Details})" : "";
                var status = stream.IsMatch ? "MATCH".Info() : "MISMATCH".Fatal();
                lines.Add(
                    $"    - Stream #{stream.Index} [{stream.StreamType}]{streamDetails}: {status}");
                if (!stream.IsMatch) {
                    if (stream.File1BitstreamHash != null || stream.File2BitstreamHash != null) {
                        lines.Add(
                            $"        File 1 Bitstream: {stream.File1BitstreamHash ?? "(n/a)"}");
                        lines.Add(
                            $"        File 2 Bitstream: {stream.File2BitstreamHash ?? "(n/a)"}");
                    }

                    if (stream.File1DecodedHash != null || stream.File2DecodedHash != null) {
                        lines.Add(
                            $"        File 1 Decoded:   {stream.File1DecodedHash ?? "(n/a)"}");
                        lines.Add(
                            $"        File 2 Decoded:   {stream.File2DecodedHash ?? "(n/a)"}");
                    }
                }
            }
        }

        lines.Add("");

        // 3) Differing Fields
        lines.Add("[3] Differing Fields:");
        var diffsToShow = IsContentMatch || allFields ? AllDifferences : [];

        if (IsBitExactMatch) {
            lines.Add($"    {"None".Info()}. All metadata and binary fields are identical.");
        } else if (!IsContentMatch && !allFields) {
            lines.Add(
                "    Content does not match. (Use --all-fields / -a to see all metadata differences anyway).");
        } else if (diffsToShow.Count == 0) {
            lines.Add($"    {"None".Info()}. All metadata fields match.");
        } else {
            lines.Add($"    Found {$"{diffsToShow.Count} differing field(s)".Warn()}:");
            lines.Add("");
            var grouped = diffsToShow.GroupBy(d => d.Category).OrderBy(g => g.Key);
            foreach (var group in grouped) {
                lines.Add($"    • {group.Key} ({group.Count()}):");
                foreach (var diff in group) {
                    var v1 = diff.File1Value != null ? $"\"{diff.File1Value}\"" : "(missing)".Trace();
                    var v2 = diff.File2Value != null ? $"\"{diff.File2Value}\"" : "(missing)".Trace();
                    lines.Add($"        {diff.Name}:");
                    lines.Add($"            File 1: {v1}");
                    lines.Add($"            File 2: {v2}");
                }

                lines.Add("");
            }

            if (lines.Count > 0 && lines[^1] == "") {
                lines.RemoveAt(lines.Count - 1);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

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
