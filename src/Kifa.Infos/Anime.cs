using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Infos.Tmdb;
using Kifa.Service;

namespace Kifa.Infos;

public class Anime : DataModel, WithModelId<Anime>, Formattable, WithFormatInfo, ItemProvider {
    public static string ModelId => "animes";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<Anime> {
        string? Format(string id, int seasonId, int episodeId);
    }

    public class RestServiceClient : KifaServiceRestClient<Anime>, ServiceClient {
        public string? Format(string id, int seasonId, int episodeId)
            => Call<string?>("format", new Dictionary<string, string> {
                { "id", id },
                { "seasonId", seasonId.ToString() },
                { "episodeId", episodeId.ToString() }
            });
    }

    #endregion

    public override bool FillByDefault => true;

    static readonly Language DefaultLanguage = Language.Japanese;

    public string? Title { get; set; }
    public Date? AirDate { get; set; }
    public string? TmdbId { get; set; }
    public Language? Language { get; set; }
    public List<Season>? Seasons { get; set; }
    public List<Episode>? Specials { get; set; }

    public string? PatternId { get; set; }
    public int? SeasonIdWidth { get; set; }
    public int? EpisodeIdWidth { get; set; }

    public string? Format(Season season, Episode episode, string? version = null) {
        var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth ?? 2;
        var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

        var sid = season.Id.ToString().PadLeft(seasonIdWidth, '0');

        var eid = episode.Id.ToString().PadLeft(episodeIdWidth, '0');

        var baseFolder = GetBaseFolder(version);
        // season.Title and episode.Title can be empty.
        return PatternId switch {
            "multi_season" => baseFolder + $"/Season {season.Id} {season.Title}".TrimEnd() +
                              $" ({season.AirDate.Year})" +
                              $"/{Title} S{sid}E{eid} {episode.Title}".TrimEnd(),
            "single_season" => baseFolder + $"/{Title} EP{eid} {episode.Title}".TrimEnd(),
            _ => null
        };
    }

    public (Season Season, Episode Episode)? Parse(string formatted, string? version = null) {
        var baseFolder = GetBaseFolder(version);
        var pattern = PatternId switch {
            "multi_season" =>
                $@"{Regex.Escape(baseFolder)}/Season (\d+) (.* )?(\(\d+\))/{Title} S(?<season_id>\d+)E(?<episode_id>\d+)",
            "single_season" => $@"{Regex.Escape(baseFolder)}/{Title} EP(?<episode_id>\d+)",
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

    string GetBaseFolder(string? version = null)
        => $"/Anime/{Title} ({AirDate.Checked().Year}){string.FormatOrEmpty($" [{version}]")}";

    public override DateTimeOffset? Fill() {
        if (TmdbId == null) {
            throw new UnableToFillException($"Not enough info to fill Anime (TmdbId = {TmdbId})");
        }

        Language ??= DefaultLanguage;

        var tmdb = new TmdbClient();
        var series = tmdb.GetSeries(TmdbId, Language);
        if (series == null) {
            throw new UnableToFillException($"Failed to find series with {TmdbId}.");
        }

        Title ??= Id;
        AirDate = series.FirstAirDate;

        Specials = null;
        Seasons = new List<Season>();

        foreach (var seasonInfo in series.Seasons) {
            var data = tmdb.GetSeason(TmdbId, seasonInfo.SeasonNumber, Language);

            var episodes = data.Episodes.Select(episode => new Episode {
                Id = episode.EpisodeNumber,
                Title = Helper.NormalizeTitle(episode.Name, language: Language),
                AirDate = episode.AirDate,
                Overview = episode.Overview
            }).ToList();

            if (seasonInfo.SeasonNumber > 0) {
                var seasonName = Helper.NormalizeTitle(seasonInfo.Name, prefix: Title,
                    language: Language);
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

        return null;
    }

    public static ItemInfoList? GetItems(string[] spec, string? version = null) {
        if (spec[0] != "Anime" || spec.Length is < 2 or > 4) {
            return null;
        }

        var id = spec[1];
        var requestedSeasonId = spec.Length > 2 ? int.Parse(spec[2]) : (int?) null;
        var requestedEpisodeId = spec.Length > 3 ? int.Parse(spec[3]) : (int?) null;
        var anime = Client.Get(id).Checked();
        return new ItemInfoList {
            Info = anime,
            Items = anime.Seasons.Checked()
                .Where(season => requestedSeasonId == null || season.Id == requestedSeasonId)
                .SelectMany(season => season.Episodes.Checked(),
                    (season, episode) => (Season: season, Episode: episode))
                .Where(item => requestedEpisodeId == null || requestedEpisodeId == item.Episode.Id)
                .Select(item => new ItemInfo {
                    EpisodeId = item.Episode.Id,
                    SeasonId = item.Season.Id,
                    Path = anime.Format(item.Season, item.Episode, version).Checked()
                }).ToList()
        };
    }
}
