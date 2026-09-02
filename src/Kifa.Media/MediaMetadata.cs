using System;

namespace Kifa.Media;

public class MediaMetadata {
    public DateTime? CapturedAt { get; set; }

    // Normalized 3-digit millisecond fraction (e.g. "842", "331"), or null if unavailable.
    public string? SubSecond { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }

    public string? AppPackage { get; set; }
    public bool IsScreenshot { get; set; }

    public TimeSpan? Duration { get; set; }

    public string FormatBaseName(string? sourceTag = null, int? sequence = null) {
        var datePart = CapturedAt?.ToString("yyyyMMdd_HHmmss") ?? "00000000_000000";
        var subsecPart = SubSecond != null ? $".{SubSecond}" : "";
        var tagPart = sourceTag != null ? $"_{sourceTag}" : "";
        var seqPart = sequence != null ? $"_{sequence}" : "";

        return $"{datePart}{subsecPart}{tagPart}{seqPart}";
    }
}
