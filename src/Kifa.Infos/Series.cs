using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kifa.Infos;

public class Season : WithFormatInfo {
    public int Id { get; set; }
    public string? Title { get; set; }
    public Date? AirDate { get; set; }
    public string? Overview { get; set; }
    public List<Episode>? Episodes { get; set; }

    public string? PatternId { get; set; }
    public int? SeasonIdWidth { get; set; }
    public int? EpisodeIdWidth { get; set; }
}

public class Episode : WithFormatInfo {
    public int Id { get; set; }
    public string? Title { get; set; }
    public Date? AirDate { get; set; }
    public string? Overview { get; set; }

    public string? PatternId { get; set; }
    public int? SeasonIdWidth { get; set; }
    public int? EpisodeIdWidth { get; set; }
}

// Special episode.
//
// There are multiple ways to put a special episode. Some examples:
//   Season + Episode
//     半沢直樹 (2013)/Season 2 2020年版 (2020)/半沢直樹 S02E00 狙われた半沢直樹のパスワード
//   Season + Collection + (Episode)
//     Friends (1993)/Season 1 (1993)/Extras/Uncut
//   Collection?
//     关于我转生变成史莱姆这档事 (2019)/转生史莱姆日记 (2020)/转生史莱姆日记 S01E01 xx
public class SpecialEpisode : Episode {
    // Alternative season for the special episode.
    public string? Season { get; set; }

    // The special collection this belongs to. Like Extras.
    public string? Collection { get; set; }

    // Alternative episode for the special episode. It can also be like 13.5 or 13.1.
    public string? Episode { get; set; }
}

public interface Formattable : WithFormatInfo {
    string? Format(Season season, Episode episode);
    (Season Season, Episode Episode) Parse(string formatted);
}

public interface WithFormatInfo {
    string? PatternId { get; set; }
    int? SeasonIdWidth { get; set; }
    int? EpisodeIdWidth { get; set; }
}

static class Helper {
    static readonly List<(Regex pattern, MatchEvaluator replacer)> BasePatterns = new() {
        (new Regex(@"/"), _ => "／")
    };

    static readonly Dictionary<Language, List<(Regex pattern, MatchEvaluator replacer)>>
        LanguagePatterns = new() {
            [Language.Japanese] = new List<(Regex pattern, MatchEvaluator replacer)> {
                (new Regex(@" *\([ぁ-ヿ]+\) *"), _ => ""),
                (new Regex(@" *\[[ぁ-ヿ]+\] *"), _ => ""),
                (new Regex(@"\""(.+)\"""), match => $"“{match.Groups[1].Value}”"),
                (new Regex(@" *\((.+)\) *"), match => $"（{match.Groups[1].Value}）"),
                (new Regex(@", "), _ => "、"),
                (new Regex(@"\? *"), _ => "？"),
                (new Regex(@"! *"), _ => "！"),
                (new Regex(@"… *"), _ => "…")
            },
            [Language.English] = new List<(Regex pattern, MatchEvaluator replacer)> {
                (new Regex(@" \((\d+)\)"), match => $" - Part {match.Groups[1].Value}"),
                (new Regex(@""""), _ => "'")
            }
        };

    public static string NormalizeTitle(string title, Language? language = null) {
        if (string.IsNullOrEmpty(title)) {
            return title;
        }

        language ??= Language.English;

        title = BasePatterns.Aggregate(title,
            (current, pattern) => pattern.pattern.Replace(current, pattern.replacer));

        return LanguagePatterns
            .GetValueOrDefault(language, new List<(Regex pattern, MatchEvaluator replacer)>())
            .Aggregate(title,
                (current, pattern) => pattern.pattern.Replace(current, pattern.replacer));
    }
}
