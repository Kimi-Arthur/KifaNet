using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace Kifa;

public static class PathExtensions {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxPathSegmentByteCount = 255;
    public const int MaxFileNameByteCount = 250;

    static readonly Dictionary<string, string> SafeCharacterMapping = new() {
        ["/"] = "／",
        ["\\"] = "＼",
        [": "] = "：",
        ["|"] = "｜",
        ["?"] = "？",
        ["*"] = "＊",
        ["<"] = "＜",
        [">"] = "＞",
        ["\n"] = " "
    };

    static readonly Regex MultipleSpacesPattern = new(" +");

    static string RemoveUnnecessarySpaces(this string text)
        => MultipleSpacesPattern.Replace(text.Trim(), " ");

    static string NormalizeSegment(string segment)
        => SafeCharacterMapping.Aggregate(segment.Normalize(NormalizationForm.FormC).Trim(),
                (current, mapping) => current.Replace(mapping.Key, mapping.Value))
            .RemoveUnnecessarySpaces();

    public static readonly char[] ChopMarkers = [.. Enumerable.Range(0, 10).Select(i => (char) i)];

    public static string Choppable(this string text, int priority = 1) => $"{text}{ChopMarkers[priority]}";

    public static string NoChop(this string text) => $"{text}{ChopMarkers[0]}";

    class SegmentPart {
        public string Text { get; set; } = "";
        public int Priority { get; set; }
    }

    public static string NormalizeFileName(this string fileName, int? reservedBytes = null,
        int maxByteCount = MaxFileNameByteCount) {
        if (reservedBytes < 0) {
            throw new ArgumentException(
                $"Reserved bytes '{reservedBytes}' cannot be negative.",
                nameof(reservedBytes));
        }

        if (reservedBytes > maxByteCount) {
            throw new ArgumentException(
                $"Reserved bytes '{reservedBytes}' exceeds maximum byte count of {maxByteCount}.",
                nameof(reservedBytes));
        }

        var maxFileNameByteCount = reservedBytes == null ? -1 : maxByteCount - reservedBytes.Value;
        var parts = new List<SegmentPart>();
        var currentText = new StringBuilder();
        var seenPriorities = new HashSet<int>();

        for (var i = 0; i < fileName.Length; i++) {
            var c = fileName[i];
            var priority = Array.IndexOf(ChopMarkers, c);
            if (priority >= 0) {
                if (priority > 0 && !seenPriorities.Add(priority)) {
                    throw new ArgumentException(
                        $"File name '{fileName}' contains duplicate '\\{priority}' chop marker.",
                        nameof(fileName));
                }

                parts.Add(new SegmentPart {
                    Text = NormalizeSegment(currentText.ToString()),
                    Priority = priority
                });
                currentText.Clear();
            } else {
                currentText.Append(c);
            }
        }

        if (currentText.Length > 0 || parts.Count == 0) {
            parts.Add(new SegmentPart {
                Text = NormalizeSegment(currentText.ToString()),
                Priority = 0
            });
        }

        var choppableParts = parts.Where(p => p.Priority > 0)
            .OrderBy(p => p.Priority)
            .ToList();

        if (choppableParts.Count == 0) {
            var result = string.Concat(parts.Select(p => p.Text));
            if (maxFileNameByteCount >= 0 &&
                Encoding.UTF8.GetByteCount(result) > maxFileNameByteCount) {
                throw new ArgumentException(
                    $"File name '{result}' exceeds maximum byte count of {maxFileNameByteCount} and contains no chop markers.",
                    nameof(fileName));
            }

            return result;
        }

        if (maxFileNameByteCount < 0) {
            return string.Concat(parts.Select(p => p.Text));
        }

        var nonChoppableBytes = parts.Where(p => p.Priority == 0)
            .Sum(p => Encoding.UTF8.GetByteCount(p.Text));
        var availableBytesForChoppable = maxFileNameByteCount - nonChoppableBytes;
        if (availableBytesForChoppable < 0) {
            throw new ArgumentException(
                $"Non-choppable parts ({nonChoppableBytes} bytes) exceed maximum byte count of {maxFileNameByteCount}.",
                nameof(fileName));
        }

        var totalBytes = parts.Sum(p => Encoding.UTF8.GetByteCount(p.Text));
        var excess = totalBytes - maxFileNameByteCount;

        if (excess > 0) {
            foreach (var part in choppableParts) {
                var currentBytes = Encoding.UTF8.GetByteCount(part.Text);
                var targetBytes = currentBytes - excess;
                if (targetBytes > 0) {
                    part.Text = part.Text.ChopEndToByteCount(targetBytes);
                    excess = 0;
                    break;
                }

                part.Text = "";
                excess -= currentBytes;
            }
        }

        if (excess > 0) {
            throw new ArgumentException(
                $"Non-choppable parts exceed maximum byte count of {maxFileNameByteCount}.",
                nameof(fileName));
        }

        return string.Concat(parts.Select(p => p.Text));
    }

    public static string NormalizeFilePath(this string path,
        int reservedFileBytes = 0,
        int reservedFolderBytes = 0) {
        var segments = path.Split('/');
        var normalizedSegments = new string[segments.Length];
        for (var i = 0; i < segments.Length - 1; i++) {
            normalizedSegments[i] =
                segments[i].NormalizeFileName(reservedBytes: reservedFolderBytes,
                    maxByteCount: MaxPathSegmentByteCount);
        }

        if (segments.Length > 0) {
            normalizedSegments[^1] =
                segments[^1].NormalizeFileName(reservedBytes: reservedFileBytes,
                    maxByteCount: MaxFileNameByteCount);
        }

        return string.Join("/", normalizedSegments);
    }

    // Chops the string to fit within maxByteCount UTF-8 bytes along Rune boundaries, appending a trailing '~' (1 byte) if truncated.
    // Returns the original string unmodified if it already fits.
    public static string ChopEndToByteCount(this string str, int maxByteCount = -1) {
        if (maxByteCount < 0 || Encoding.UTF8.GetByteCount(str) <= maxByteCount) {
            return str;
        }

        Logger.Trace($"Chop {str} to {maxByteCount} bytes.");

        // Special case for maxByteCount = 0
        if (maxByteCount == 0) {
            throw new ArgumentException($"{nameof(maxByteCount)} should be <0 or >=1.",
                nameof(maxByteCount));
        }

        var length = 0;
        var charLength = 0;
        foreach (var rune in str.EnumerateRunes()) {
            if (length + rune.Utf8SequenceLength + 1 > maxByteCount) {
                Logger.Debug(
                    $"Chopped {str} to {str[..charLength]} due to byte limit of {maxByteCount}");
                return str[..charLength] + "~";
            }

            length += rune.Utf8SequenceLength;
            charLength += rune.Utf16SequenceLength;
        }

        throw new UnreachableException(
            $"String ({str}) of byte size {length} should be chopped somewhere to {maxByteCount}");
    }
}
