using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.Infos;

public class Anime : DataModel<Anime>, Formattable {
    public const string ModelId = "animes";

    static KifaServiceClient<Anime> client;

    public static KifaServiceClient<Anime> Client => client ??= new KifaServiceRestClient<Anime>();

    public string? Title { get; set; }
    public Date? AirDate { get; set; }
    public string? TmdbId { get; set; }
    public List<Season>? Seasons { get; set; }
    public List<Episode>? Specials { get; set; }

    public string? PatternId { get; set; }
    public int? SeasonIdWidth { get; set; }
    public int? EpisodeIdWidth { get; set; }

    public string? Format(Season season, Episode episode) {
        var patternId = episode.PatternId ?? season.PatternId ?? PatternId;
        var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth ?? 2;
        var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

        var sid = season.Id.ToString().PadLeft(seasonIdWidth, '0');

        var eid = episode.Id.ToString().PadLeft(episodeIdWidth, '0');

        // season.Title and episode.Title can be empty.
        return patternId switch {
            "multi_season" => $"/Anime/{Title} ({AirDate.Year})" +
                              $"/Season {season.Id} {season.Title}".TrimEnd() +
                              $" ({season.AirDate.Year})" +
                              $"/{Title} S{sid}E{eid} {episode.Title}".TrimEnd(),
            "single_season" => $"/Anime/{Title} ({AirDate.Year})" +
                               $"/{Title} EP{eid} {episode.Title}".TrimEnd(),
            _ => null
        };
    }
}

public interface AnimeServiceClient : KifaServiceClient<Anime> {
    string? Format(string id, int seasonId, int episodeId);
}

public class AnimeRestServiceClient : KifaServiceRestClient<Anime>, AnimeServiceClient {
    public string? Format(string id, int seasonId, int episodeId)
        => Call<string?>("format", new Dictionary<string, string> {
            { "id", id },
            { "seasonId", seasonId.ToString() },
            { "episodeId", episodeId.ToString() }
        });
}
