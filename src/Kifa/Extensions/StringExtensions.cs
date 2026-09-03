using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace Kifa;

public static class StringExtensions {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly Regex NumberPattern = new(@"\d{1,8}", RegexOptions.ECMAScript);

    const string SizeSymbols = "KMGTPEZY";

    static readonly Dictionary<string, long> SizeSymbolMap = SizeSymbols.Select(x => x.ToString())
        .Prepend("").Select((value, index) => (value, factor: 1L << (10 * index)))
        .ToDictionary(item => item.value, item => item.factor);

    public static string Format(this string format, params (string Key, object Value)[] parameters)
        => parameters.Aggregate(format,
            (current, p) => current.Replace("{" + p.Key + "}", p.Value.ToString()));

    public static string Format(this string format, params (string Key, string Value)[] parameters)
        => parameters.Aggregate(format,
            (current, p) => current.Replace("{" + p.Key + "}", p.Value));

    extension(string) {
        [return: NotNullIfNotNull(nameof(defaultString))]
        public static string? FormatOr(FormattableString formattableString,
            string? defaultString = null)
            => formattableString.GetArguments().Any(arg => arg == null)
                ? defaultString
                : formattableString.ToString();

        public static string FormatOrEmpty(FormattableString formattableString)
            => FormatOr(formattableString, "");
    }

    // Remove all characters including and after the last split.
    public static string RemoveAfter(this string s, string split) {
        var index = s.LastIndexOf(split);
        return index < 0 ? s : s[..index];
    }

    public static long ParseSizeString(this string data) {
        if (string.IsNullOrEmpty(data)) {
            throw new ArgumentNullException(nameof(data));
        }

        var match = new Regex(@"^(\d+)([^B])B?$").Match(data.ToUpper());

        return long.Parse(match.Groups[1].Value) *
               SizeSymbolMap.GetValueOrDefault(match.Groups[2].Value, 0);
    }

    public static string ToSizeString(this long? size) {
        if (size == null) {
            return "?B";
        }

        return size.Checked().ToSizeString();
    }

    public static string ToSizeString(this long size) {
        var index = Math.Log2(size.Checked()).RoundDown() / 10 - 1;
        if (index < 0) {
            return $"{size}B";
        }

        var symbol = SizeSymbols[index];
        return $"{size * 1.0 / SizeSymbolMap[symbol.ToString()]:0.0}{symbol}B";
    }

    public static string ToSizeString(this int? size) => ToSizeString((long?) size);

    public static string ToSizeString(this int size) => ToSizeString((long) size);

    public static byte[] ParseHexString(this string hexString) {
        if (hexString == null || hexString.Length % 2 == 1) {
            throw new ArgumentException("Not hex string");
        }

        var hexBytes = new byte[hexString.Length / 2];
        for (var i = 0; i < hexString.Length; i += 2) {
            hexBytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }

        return hexBytes;
    }

    public static TimeSpan ParseTimeSpanString(this string timeSpanString) {
        if (string.IsNullOrEmpty(timeSpanString)) {
            return TimeSpan.Zero;
        }

        if (timeSpanString.EndsWith("hr")) {
            return TimeSpan.FromHours(
                double.Parse(timeSpanString.Substring(0, timeSpanString.Length - 2)));
        }

        if (timeSpanString.EndsWith("min")) {
            return TimeSpan.FromMinutes(
                double.Parse(timeSpanString.Substring(0, timeSpanString.Length - 3)));
        }

        if (timeSpanString.EndsWith("s")) {
            return TimeSpan.FromSeconds(
                double.Parse(timeSpanString.Substring(0, timeSpanString.Length - 1)));
        }

        return TimeSpan.Parse(timeSpanString);
    }

    public static DateTimeOffset ParseDateTimeOffset(this string dateTimeOffsetString,
        TimeZoneInfo timeZone) {
        var dateTime = DateTime.Parse(dateTimeOffsetString);
        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }

    public static string GetNaturalSortKey(this string path)
        => path.Contains("/$/")
            ? path
            : NumberPattern.Replace(path, m => $"{long.Parse(m.Value):D8}");

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

    public static string NormalizeFileName(this string fileName,
        int maxFileNameByteCount = MaxFileNameByteCount) {
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
        int maxFileNameByteCount = MaxFileNameByteCount,
        int maxPathSegmentByteCount = MaxPathSegmentByteCount) {
        var segments = path.Split('/');
        var normalizedSegments = new string[segments.Length];
        for (var i = 0; i < segments.Length - 1; i++) {
            normalizedSegments[i] =
                segments[i].NormalizeFileName(maxFileNameByteCount: maxPathSegmentByteCount);
        }

        if (segments.Length > 0) {
            normalizedSegments[^1] =
                segments[^1].NormalizeFileName(maxFileNameByteCount: maxFileNameByteCount);
        }

        return string.Join("/", normalizedSegments);
    }

    public static byte[] FromBase64(this string text) => Convert.FromBase64String(text);

    public static string ToBase64(this string text)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    public static bool ContainsSequence(this string text, string search) {
        var index = 0;
        foreach (var _ in text.Where(ch => ch == search[index])) {
            index++;
            if (index == search.Length) {
                return true;
            }
        }

        return false;
    }

    public static string JoinBy(this IEnumerable<string?> values, string separator = "")
        => string.Join(separator, values);

    public static string JoinBy(this IEnumerable<string?> values, char separator)
        => string.Join(separator, values);

    public static string NormalizeWikiTitle(this string title) => title.Replace(" ", "_");

    public static string ChopPrefix(this string source, string prefix)
        => source.StartsWith(prefix) ? source[prefix.Length..] : source;

    // Chops the string to fit within maxByteCount UTF-8 bytes along Rune boundaries, appending a trailing '+' (1 byte) if truncated.
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
                return str[..charLength] + "+";
            }

            length += rune.Utf8SequenceLength;
            charLength += rune.Utf16SequenceLength;
        }

        throw new UnreachableException(
            $"String ({str}) of byte size {length} should be chopped somewhere to {maxByteCount}");
    }
}
