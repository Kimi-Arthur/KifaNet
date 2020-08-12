using System;
using System.Collections.Generic;
using System.Linq;
using Pimix.Infos.Tmdb;
using Pimix.Service;

namespace Pimix.Infos {
    public class TvShow : DataModel, Formattable {
        public const string ModelId = "tv_shows";

        const string Part1Suffix = " - Part 1";

        static Dictionary<Language, Func<int, string>> StandardSeasonNames =
            new Dictionary<Language, Func<int, string>> {
                [Language.German] = s => $"Staffel {s}",
                [Language.English] = s => $"Season {s}",
                [Language.Japanese] = s => $"シーズン{s}",
                [Language.Chinese] = s => $"第{GetChineseNumber(s)}季"
            };

        static TvShowServiceClient client;

        public static TvShowServiceClient Client => client ??= new TvShowRestServiceClient();

        public string Title { get; set; }
        public Date AirDate { get; set; }
        public string Overview { get; set; }
        public string TvNetwork { get; set; }
        public Region Region { get; set; }
        public List<string> Genres { get; set; }
        public string TmdbId { get; set; }
        public string TvdbId { get; set; }
        public Language Language { get; set; }

        public List<Season> Seasons { get; set; }
        public List<Episode> Specials { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }

        public override void Fill() {
            var tmdb = new TmdbClient();
            var series = tmdb.GetSeries(TmdbId, Language.Code);
            Title ??= Id;
            AirDate = series.FirstAirDate;
            TvNetwork = series.Networks[0].Name;
            Region = series.OriginCountry.First();

            Genres = series.Genres.Select(g => g.Name).ToList();
            Overview = series.Overview;

            Specials = null;
            Seasons = new List<Season>();

            foreach (var seasonInfo in series.Seasons) {
                var data = tmdb.GetSeason(TmdbId, seasonInfo.SeasonNumber, Language.Code);

                var episodes = data.Episodes.Select(episode => new Episode {
                    Id = episode.EpisodeNumber,
                    Title = Helper.NormalizeTitle(episode.Name, Language),
                    AirDate = episode.AirDate,
                    Overview = episode.Overview
                }).ToList();

                if (seasonInfo.SeasonNumber > 0) {
                    var seasonName = Helper.NormalizeTitle(seasonInfo.Name);
                    Seasons.Add(new Season {
                        AirDate = seasonInfo.AirDate,
                        Id = seasonInfo.SeasonNumber,
                        Title = IsStandardSeasonName(seasonName, seasonInfo.SeasonNumber, Language) ? null : seasonName,
                        Overview = seasonInfo.Overview,
                        Episodes = episodes
                    });
                } else {
                    Specials = episodes;
                }
            }
        }

        static bool IsStandardSeasonName(string seasonName, int seasonNumber, Language language) {
            return seasonName == StandardSeasonNames.GetValueOrDefault(language, s => "")(seasonNumber);
        }

        public string Format(Season season, Episode episode) {
            return Format(season, new List<Episode> {episode});
        }

        public string Format(Season season, List<Episode> episodes) {
            var episode = episodes.First();
            var patternId = episode.PatternId ?? season.PatternId ?? PatternId;
            var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth ?? 2;
            var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

            var sid = season.Id.ToString();
            sid = new string('0', Math.Max(seasonIdWidth - sid.Length, 0)) + sid;

            var eids = episodes.Select(e => e.Id.ToString())
                .Select(eid => new string('0', Math.Max(episodeIdWidth - eid.Length, 0)) + eid);

            var episodeTitle = GetTitle(episodes.Select(e => e.Title).ToList());

            // season.Title and episode.Title can be empty.
            switch (patternId) {
                case "multi_season":
                    return $"/TV Shows/{Region}/{Title} ({AirDate.Year})" +
                           $"/Season {season.Id} {season.Title}".TrimEnd() + $" ({season.AirDate.Year})" +
                           $"/{Title} S{sid}{string.Join("", eids.Select(eid => $"E{eid}"))} {episodeTitle}".TrimEnd();
                case "single_season":
                    return $"/TV Shows/{Region}/{Title} ({AirDate.Year})" +
                           $"/{Title} {string.Join("", eids.Select(eid => $"EP{eid}"))} {episodeTitle}".TrimEnd();
                default:
                    return "Unexpected!";
            }
        }

        static string GetTitle(IReadOnlyList<string> titles) => GetSharedTitle(titles) ?? string.Join(" ", titles);

        static string GetSharedTitle(IReadOnlyList<string> titles) {
            var firstTitle = titles.First();
            if (!firstTitle.EndsWith(Part1Suffix)) {
                return null;
            }

            var sharedTitle = firstTitle.Substring(0, firstTitle.Length - Part1Suffix.Length);

            for (var i = 1; i < titles.Count; i++) {
                if (titles[i] != $"{sharedTitle} - Part {i + 1}") {
                    return null;
                }
            }

            return sharedTitle;
        }

        static List<string> ChineseNumbers = new List<string> {
            "零",
            "一",
            "二",
            "三",
            "四",
            "五",
            "六",
            "七",
            "八",
            "九",
            "十",
        };

        static string GetChineseNumber(int number) {
            return ChineseNumbers[number];
        }
    }

    public interface TvShowServiceClient : PimixServiceClient<TvShow> {
        string Format(string id, int seasonId, int episodeId);
        string Format(string id, int seasonId, List<int> episodeIds);
    }

    public class TvShowRestServiceClient : PimixServiceRestClient<TvShow>, TvShowServiceClient {
        public string Format(string id, int seasonId, int episodeId) {
            return Format(id, seasonId, new List<int> {episodeId});
        }

        public string Format(string id, int seasonId, List<int> episodeIds) {
            var show = Get(id);
            var season = show.Seasons.First(s => s.Id == seasonId);
            var episodes = episodeIds.Select(episodeId => season.Episodes.First(e => e.Id == episodeId)).ToList();
            return show.Format(season, episodes);
        }
    }
}
