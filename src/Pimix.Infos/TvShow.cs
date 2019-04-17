using System.Collections.Generic;
using System.Linq;
using Pimix.Service;

namespace Pimix.Infos {
    [DataModel("tv_shows")]
    public class TvShow : Formattable {
        public string Id { get; set; }
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

        public string Format(int seasonId, int episodeId) {
            var season = Seasons.First(s => s.Id == seasonId);
            var episode = season.Episodes.First(e => e.Id == episodeId);
            var patternId = episode.PatternId ?? season.PatternId ?? PatternId;
            var seasonIdWidth = episode.SeasonIdWidth ?? season.SeasonIdWidth ?? SeasonIdWidth;
            var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth;

            return "";
        }
    }
}
