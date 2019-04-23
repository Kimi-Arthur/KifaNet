using System;
using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Infos {
    [DataModel("animes")]
    public class Anime : Formattable {
        public string Id { get; set; }
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
            var episodeIdWidth = episode.EpisodeIdWidth ?? season.EpisodeIdWidth ?? EpisodeIdWidth ?? 2;

            var sid = season.Id.ToString();
            sid = new string('0', Math.Max(seasonIdWidth - sid.Length, 0)) + sid;

            var eid = episode.Id.ToString();
            eid = new string('0', Math.Max(episodeIdWidth - eid.Length, 0)) + eid;

            // season.Title and episode.Title can be empty.
            switch (patternId) {
                case "multi_season":
                    return $"/Anime/{Title} ({AirDate.Year})" +
                           $"/Season {season.Id} {season.Title}".TrimEnd() + $" ({season.AirDate.Year})" +
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
}
