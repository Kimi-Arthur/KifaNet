using System;
using System.Collections.Generic;

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
        public DateTime AirDate { get; set; }
        public string Overview { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }
    }

    public interface WithFormatInfo {
        string PatternId { get; set; }
        int? SeasonIdWidth { get; set; }
        int? EpisodeIdWidth { get; set; }
    }
}
