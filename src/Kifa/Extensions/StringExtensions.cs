using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kifa;

public static class StringExtensions {
    static readonly Regex NumberPattern = new(@"\d+", RegexOptions.ECMAScript);

    static readonly Dictionary<string, long> SymbolMap = "KMGTPEZY".Select(x => x.ToString())
        .Prepend("").Select((value, index) => (value, factor: 1L << (10 * index)))
        .ToDictionary(item => item.value, item => item.factor);

    static readonly Dictionary<string, string> SafeCharacterMapping = new() {
        ["/"] = "／", // Must come first fot NormalizeFilePath.
        ["\\"] = "＼",
        [": "] = "：",
        ["|"] = "｜",
        ["?"] = "？",
        ["*"] = "＊",
        ["<"] = "＜",
        [">"] = "＞"
    };

    public static string Format(this string format, Dictionary<string, string> parameters) {
        if (format == null) {
            throw new ArgumentNullException(nameof(format));
        }

        var result = format;

        if (parameters == null) {
            return result;
        }

        foreach (var p in parameters) {
            result = result.Replace("{" + p.Key + "}", p.Value);
        }

        return result;
    }

    public static string Format(this string format, params object[] args) {
        if (format == null) {
            throw new ArgumentNullException(nameof(format));
        }

        return string.Format(format, args);
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
               SymbolMap.GetValueOrDefault(match.Groups[2].Value, 0);
    }

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

    public static string NormalizeFileName(this string fileName)
        => SafeCharacterMapping.Aggregate(fileName.Normalize(NormalizationForm.FormC).Trim(),
                (current, mapping) => current.Replace(mapping.Key, mapping.Value))
            .RemoveUnnecessarySpaces();

    public static string NormalizeFilePath(this string fileName)
        => string.Join("/", fileName.Split('/').Select(NormalizeFileName));

    static readonly Regex MultipleSpacesPattern = new(" +");

    static string RemoveUnnecessarySpaces(this string text)
        => MultipleSpacesPattern.Replace(text.Trim(), " ");

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

    public static string JoinBy(this IEnumerable<string> values, string separator = "")
        => string.Join(separator, values);

    public static string NormalizeWikiTitle(this string title) => title.Replace(" ", "_");
}
