using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Infos;

public class Series : DataModel, WithModelId<Series>, Formattable, WithFormatInfo, ItemProvider {
    public static string ModelId => "series";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<Series> {
        string? Format(string id, int seasonId, int episodeId);
    }

    public class RestServiceClient : KifaServiceRestClient<Series>, ServiceClient {
        public string? Format(string id, int seasonId, int episodeId)
            => Call<string?>("format", new Dictionary<string, string> {
                { "id", id },
                { "seasonId", seasonId.ToString() },
                { "episodeId", episodeId.ToString() }
            });
    }

    #endregion

    public static HashSet<string> KnownCategories { get; set; } =
        ["Gaming", "Tales", "Food", "News", "Technology"];
    // Id should be like /Gaming/黑桐谷歌/漫威蜘蛛侠2

    public List<Season>? Seasons { get; set; }
    public List<Episode>? Specials { get; set; }

    public string? PatternId { get; set; }
    public int? SeasonIdWidth { get; set; }
    public int? EpisodeIdWidth { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    string Title => Id.Checked().Split("/").Last();

    public string? Format(Season season, Episode episode, string? version = null) {
        var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth ?? 2;
        var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

        var sid = season.Id.ToString().PadLeft(seasonIdWidth, '0');

        var eid = episode.Id.ToString().PadLeft(episodeIdWidth, '0');

        // season.Title and episode.Title can be empty.
        return PatternId switch {
            "multi_season" => $"{Id}/Season {season.Id} {season.Title}".TrimEnd() +
                              $"/{Title} S{sid}E{eid} {episode.Title}".TrimEnd(),
            "single_season" => $"{Id}/{Title} EP{eid} {episode.Title}".TrimEnd(),
            _ => null
        };
    }

    public (Season Season, Episode Episode)? Parse(string formatted) {
        var pattern = PatternId switch {
            "multi_season" =>
                $@"{Id}/Season (\d+)( .*)?/{Title} S(?<season_id>\d+)E(?<episode_id>\d+)",
            "single_season" => $@"{Id}/{Title} EP(?<episode_id>\d+)",
            _ => null
        };

        if (pattern == null) {
            return null;
        }

        var match = Regex.Match(formatted, pattern);
        if (match.Success) {
            var seasonId = match.Groups["season_id"].Success
                ? int.Parse(match.Groups["season_id"].Value)
                : 1;

            var episodeId = match.Groups["episode_id"].Success
                ? int.Parse(match.Groups["episode_id"].Value)
                : -1;

            if (episodeId < 0) {
                return null;
            }

            var season = Seasons.First(s => s.Id == seasonId);
            var episode = season.Episodes.First(e => e.Id == episodeId);
            return (season, episode);
        }

        return null;
    }

    public static ItemInfoList? GetItems(string[] spec, string? version = null) {
        if (!KnownCategories.Contains(spec[0])) {
            return null;
        }

        var numberedSegments = spec.Reverse().TakeWhile(s => int.TryParse(s, out _))
            .Select(int.Parse).Reverse().ToList();

        if (numberedSegments.Count > 2) {
            throw new ArgumentOutOfRangeException(
                $"Number of number segments at the end of the spec exceeds the maximum number of 2, is {numberedSegments.Count}");
        }

        var id = $"/{spec[..^numberedSegments.Count].JoinBy('/')}";
        var requestedSeasonId = numberedSegments.Count > 0 ? numberedSegments[0] : (int?) null;
        var requestedEpisodeId = numberedSegments.Count > 1 ? numberedSegments[1] : (int?) null;
        var series = Client.Get(id).Checked();
        return new ItemInfoList {
            Info = series,
            Items = series.Seasons.Checked()
                .Where(season => requestedSeasonId == null || season.Id == requestedSeasonId)
                .SelectMany(season => season.Episodes.Checked(),
                    (season, episode) => (Season: season, Episode: episode))
                .Where(item => requestedEpisodeId == null || requestedEpisodeId == item.Episode.Id)
                .Select(item => new ItemInfo {
                    EpisodeId = item.Episode.Id,
                    SeasonId = item.Season.Id,
                    Path = series.Format(item.Season, item.Episode, version).Checked()
                }).ToList()
        };
    }
}

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

public interface Formattable {
    string? Format(Season season, Episode episode, string? version = null);
    (Season Season, Episode Episode)? Parse(string formatted);
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
                (new Regex(@" *\([ぁ-ヿ･-ﾟ]+\) *"), _ => ""),
                (new Regex(@" *\[[ぁ-ヿ･-ﾟ]+\] *"), _ => ""),
                (new Regex(@"([０-９Ａ-Ｚａ-ｚ])"),
                    match => $"{(char) (match.Groups[1].Value[0] - 0xFF00 + 0x0020)}"),
                (new Regex(@"""(.+)"""), match => $"“{match.Groups[1].Value}”"),
                (new Regex(@" *\((.+)\) *"), match => $"（{match.Groups[1].Value}）"),
                (new Regex(@", "), _ => "、"),
                (new Regex(@"[\?] *"), _ => "？"),
                (new Regex(@"[!] *"), _ => "！"),
                (new Regex(@"･"), _ => "・"),
                (new Regex(@"([…？！]) *"), match => $"{match.Groups[1].Value}"),
                (new Regex(@"　"), _ => " "), // Full-width space
            },
            [Language.English] = new List<(Regex pattern, MatchEvaluator replacer)> {
                (new Regex(@" \((\d+)\)"), match => $" - Part {match.Groups[1].Value}"),
                (new Regex(@""""), _ => "'")
            },
            [Language.Chinese] = new List<(Regex pattern, MatchEvaluator replacer)> {
                (new Regex(@"第\d+集"), _ => ""),
            }
        };

    public static string NormalizeTitle(string title, string? prefix = null,
        Language? language = null) {
        if (string.IsNullOrEmpty(title)) {
            return title;
        }

        language ??= Language.English;

        title = BasePatterns.Aggregate(title,
            (current, pattern) => pattern.pattern.Replace(current, pattern.replacer));

        var name = LanguagePatterns
            .GetValueOrDefault(language, new List<(Regex pattern, MatchEvaluator replacer)>())
            .Aggregate(title,
                (current, pattern) => pattern.pattern.Replace(current, pattern.replacer));
        if (prefix != null && name.StartsWith(prefix)) {
            name = name[prefix.Length..];
        }

        return name.Trim();
    }
}
