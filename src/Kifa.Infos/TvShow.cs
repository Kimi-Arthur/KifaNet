using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Infos.Tmdb;
using Kifa.Service;

namespace Kifa.Infos;

public class TvShow : DataModel<TvShow>, Formattable {
    public const string ModelId = "tv_shows";

    const string Part1Suffix = " - Part 1";

    public static TvShowServiceClient Client { get; } = new TvShowRestServiceClient();

    public string? Title { get; set; }
    public Date? AirDate { get; set; }
    public string? Overview { get; set; }
    public string? TvNetwork { get; set; }
    public Region? Region { get; set; }
    public List<string>? Genres { get; set; }
    public string? TmdbId { get; set; }
    public string? TvdbId { get; set; }
    public Language? Language { get; set; }

    public List<Season>? Seasons { get; set; }
    public List<Episode>? Specials { get; set; }

    public string? PatternId { get; set; }
    public int? SeasonIdWidth { get; set; }
    public int? EpisodeIdWidth { get; set; }

    public override bool FillByDefault => true;

    public override DateTimeOffset? Fill() {
        var oldEpisodeCount = Seasons?.Select(s => s.Episodes?.Count ?? 0).Sum() ??
                              0 + Specials?.Count ?? 0;

        if (TmdbId == null || Language?.Code == null) {
            throw new UnableToFillException(
                $"Not enough info to fill TvShow (TmdbId = {TmdbId}, Language = {Language})");
        }

        var tmdb = new TmdbClient();
        var series = tmdb.GetSeries(TmdbId, Language);
        if (series == null) {
            throw new UnableToFillException(
                $"Failed to find series with {TmdbId}, {Language.Code}.");
        }

        Title ??= Id;
        AirDate = series.FirstAirDate;
        TvNetwork = series.Networks[0].Name;
        Region = series.OriginCountry.First();

        Genres = series.Genres.Select(g => g.Name).ToList();
        Overview = series.Overview;

        Specials = null;
        Seasons = new List<Season>();

        foreach (var seasonInfo in series.Seasons) {
            var data = tmdb.GetSeason(TmdbId, seasonInfo.SeasonNumber, Language);

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
                    Title = TmdbClient.NormalizeSeasonTitle(seasonName),
                    Overview = seasonInfo.Overview,
                    Episodes = episodes
                });
            } else {
                Specials = episodes;
            }
        }

        return GetNextEpisodeDate();
    }

    // TODO: Always refresh for now.
    // It should determine how frequent it's published and last updated episode to predict.
    DateTimeOffset? GetNextEpisodeDate() {
        return Date.Zero;
    }

    public string? Format(Season season, Episode episode)
        => Format(season, new List<Episode> {
            episode
        });

    public (Season Season, Episode Episode)? Parse(string formatted) {
        var pattern = PatternId switch {
            "multi_season" =>
                $@"/TV Shows/{Region}/{Title} \({AirDate.Year}\)/Season (\d+) (.* )?(\(\d+\))/{Title} S(?<season_id>\d+)E(?<episode_id>\d+)",
            "single_season" => $@"/TV Shows/{Region}/{Title} \({AirDate.Year}\)/{Title} EP(?<episode_id>\d+)",
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

    public string? Format(Season season, List<Episode> episodes) {
        var episode = episodes.First();
        var patternId = episode.PatternId ?? season.PatternId ?? PatternId;
        var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth ?? 2;
        var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

        var sid = season.Id.ToString().PadLeft(seasonIdWidth, '0');

        var eids = episodes.Select(e => e.Id.ToString())
            .Select(eid => eid.PadLeft(episodeIdWidth, '0'));

        var episodeTitle = GetTitle(episodes.Select(e => e.Title).ToList());

        // season.Title and episode.Title can be empty.
        return patternId switch {
            "multi_season" => $"/TV Shows/{Region}/{Title} ({AirDate.Year})" +
                              $"/Season {season.Id} {season.Title}".TrimEnd() +
                              $" ({season.AirDate.Year})" +
                              $"/{Title} S{sid}{string.Join("", eids.Select(eid => $"E{eid}"))} {episodeTitle}"
                                  .TrimEnd(),
            "single_season" => $"/TV Shows/{Region}/{Title} ({AirDate.Year})" +
                               $"/{Title} {string.Join("", eids.Select(eid => $"EP{eid}"))} {episodeTitle}"
                                   .TrimEnd(),
            _ => null
        };
    }

    static string GetTitle(IReadOnlyList<string> titles)
        => GetSharedTitle(titles) ?? string.Join(" ", titles);

    static string? GetSharedTitle(IReadOnlyList<string> titles) {
        if (titles.Count < 2) {
            return null;
        }

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
}

public interface TvShowServiceClient : KifaServiceClient<TvShow> {
    string Format(string id, int seasonId, int episodeId);
    string Format(string id, int seasonId, List<int> episodeIds);
}

public class TvShowRestServiceClient : KifaServiceRestClient<TvShow>, TvShowServiceClient {
    internal TvShowRestServiceClient() {
    }

    public string Format(string id, int seasonId, int episodeId)
        => Format(id, seasonId, new List<int> {
            episodeId
        });

    public string Format(string id, int seasonId, List<int> episodeIds) {
        var show = Get(id);
        var season = show.Seasons.First(s => s.Id == seasonId);
        var episodes = episodeIds.Select(episodeId => season.Episodes.First(e => e.Id == episodeId))
            .ToList();
        return show.Format(season, episodes);
    }
}
