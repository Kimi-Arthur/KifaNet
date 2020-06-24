using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pimix.Infos {
    public class Season : WithFormatInfo {
        public int Id { get; set; }
        public string Title { get; set; }
        public Date AirDate { get; set; }
        public string Overview { get; set; }
        public List<Episode> Episodes { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }
    }

    public class Episode : WithFormatInfo {
        public int Id { get; set; }
        public string Title { get; set; }
        public Date AirDate { get; set; }
        public string Overview { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }
    }

    public interface Formattable : WithFormatInfo {
        string Format(Season season, Episode episode);
    }

    public interface WithFormatInfo {
        string PatternId { get; set; }
        int? SeasonIdWidth { get; set; }
        int? EpisodeIdWidth { get; set; }
    }

    public static class Helper {
        static List<(Regex pattern, MatchEvaluator replacer)> BasePatterns =
            new List<(Regex pattern, MatchEvaluator replacer)> {(new Regex(@"/"), match => "／")};

        static Dictionary<Language, List<(Regex pattern, MatchEvaluator replacer)>> LanguagePatterns =
            new Dictionary<Language, List<(Regex pattern, MatchEvaluator replacer)>> {
                [Language.Japanese] = new List<(Regex pattern, MatchEvaluator replacer)> {
                    (new Regex(@" *\([ぁ-ヿ]+\) *"), match => ""),
                    (new Regex(@" *\[[ぁ-ヿ]+\] *"), match => ""),
                    (new Regex(@", "), match => "、"),
                    (new Regex(@"\? *"), match => "？"),
                    (new Regex(@"! *"), match => "！"),
                    (new Regex(@"… *"), match => "…")
                },
                [Language.English] = new List<(Regex pattern, MatchEvaluator replacer)> {
                    (new Regex(@" \((\d+)\)"), match => $" - Part {match.Groups[1].Value}"),
                    (new Regex(@""""), match => "'")
                }
            };

        public static string NormalizeTitle(string title, Language language = null) {
            if (string.IsNullOrEmpty(title)) {
                return title;
            }

            language ??= Language.English;

            title = BasePatterns.Aggregate(title,
                (current, pattern) => pattern.pattern.Replace(current, pattern.replacer));

            return LanguagePatterns.GetValueOrDefault(language, new List<(Regex pattern, MatchEvaluator replacer)>())
                .Aggregate(title, (current, pattern) => pattern.pattern.Replace(current, pattern.replacer));
        }
    }
}
