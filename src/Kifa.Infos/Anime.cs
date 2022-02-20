using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.Infos {
    public class Anime : DataModel<Anime>, Formattable {
        public const string ModelId = "animes";

        static KifaServiceClient<Anime> client;

        public static KifaServiceClient<Anime> Client =>
            client ??= new KifaServiceRestClient<Anime>();

        public string Title { get; set; }
        public Date AirDate { get; set; }

        public List<AnimeSeason> Seasons { get; set; }
        public List<Episode> Specials { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }

        public string Format(Season season, Episode episode) {
            var patternId = episode.PatternId ?? season.PatternId ?? PatternId;
            var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth ?? 2;
            var episodeIdWidth =
                episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

            var sid = season.Id.ToString().PadLeft(seasonIdWidth, '0');

            var eid = episode.Id.ToString().PadLeft(episodeIdWidth, '0');

            // season.Title and episode.Title can be empty.
            switch (patternId) {
                case "multi_season":
                    return $"/Anime/{Title} ({AirDate.Year})" +
                           $"/Season {season.Id} {season.Title}".TrimEnd() +
                           $" ({season.AirDate.Year})" +
                           $"/{Title} S{sid}E{eid} {episode.Title}".TrimEnd();
                case "single_season":
                    return $"/Anime/{Title} ({AirDate.Year})" +
                           $"/{Title} EP{eid} {episode.Title}".TrimEnd();
                default:
                    return "Unexpected!";
            }
        }
    }

    public class AnimeSeason : Season {
        public string AnidbId { get; set; }
    }

    public interface AnimeServiceClient : KifaServiceClient<Anime> {
        string Format(string id, int seasonId, int episodeId);
    }

    public class AnimeRestServiceClient : KifaServiceRestClient<Anime>, AnimeServiceClient {
        public string Format(string id, int seasonId, int episodeId) {
            var show = Get(id);
            var season = show.Seasons.First(s => s.Id == seasonId);
            var episode = season.Episodes.First(e => e.Id == episodeId);
            return show.Format(season, episode);
        }
    }
}
