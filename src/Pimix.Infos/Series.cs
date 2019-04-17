using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pimix.Infos {
    public class Season : Formattable {
        public int Id { get; set; }
        public string Title { get; set; }
        public Date AirDate { get; set; }
        public string Overview { get; set; }
        public List<Episode> Episodes { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }
    }

    public class Episode : Formattable {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime AirDate { get; set; }
        public string Overview { get; set; }

        public string PatternId { get; set; }
        public int? SeasonIdWidth { get; set; }
        public int? EpisodeIdWidth { get; set; }
    }

    public enum Language {
        [EnumMember(Value = "en")]
        English,

        [EnumMember(Value = "zh")]
        Chinese,

        [EnumMember(Value = "ja")]
        Japanese
    }

    public enum Region {
        [EnumMember(Value = "United States")]
        UnitedStates,
        China,
        Japan,

        [EnumMember(Value = "United Kingdom")]
        UnitedKingdom
    }

    public interface Formattable {
        string PatternId { get; set; }
        int? SeasonIdWidth { get; set; }
        int? EpisodeIdWidth { get; set; }
    }
}
