using System.Collections.Generic;
using Kifa.Rpc;

namespace Kifa.Infos.Tmdb;

public sealed class TmdbSeasonRequest : ParameterizedRequest {
    public override string UrlPattern
        => "https://api.themoviedb.org/3/tv/{sid}/season/{season}?api_key={api_key}&language={lang}";

    public TmdbSeasonRequest(string sid, int seasonId, Language language, string apiKey) {
        parameters = new Dictionary<string, string> {
            { "sid", sid },
            { "season", seasonId.ToString() },
            { "lang", language.Code },
            { "api_key", apiKey }
        };
    }
}

public class TmdbSeasonResponse {
    public string Id { get; set; }
    public Date AirDate { get; set; }
    public List<Episode> Episodes { get; set; }
    public string Name { get; set; }
    public string Overview { get; set; }
    public long ResponseId { get; set; }
    public string PosterPath { get; set; }
    public long SeasonNumber { get; set; }
}

public class Episode {
    public Date AirDate { get; set; }
    public int EpisodeNumber { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Overview { get; set; }
    public string ProductionCode { get; set; }
    public long Runtime { get; set; }
    public int SeasonNumber { get; set; }
    public long ShowId { get; set; }
    public string StillPath { get; set; }
    public double VoteAverage { get; set; }
    public long VoteCount { get; set; }
    public List<Crew> Crew { get; set; }
    public List<Crew> GuestStars { get; set; }
}

public class Crew {
    public string Job { get; set; }
    public Department? Department { get; set; }
    public string CreditId { get; set; }
    public bool Adult { get; set; }
    public long Gender { get; set; }
    public long Id { get; set; }
    public Department KnownForDepartment { get; set; }
    public string Name { get; set; }
    public string OriginalName { get; set; }
    public double Popularity { get; set; }
    public string ProfilePath { get; set; }
    public string Character { get; set; }
    public long? Order { get; set; }
}

public enum Department {
    Acting,
    Art,
    Camera,
    CostumeMakeUp,
    Crew,
    Directing,
    Editing,
    Lighting,
    Production,
    Sound,
    VisualEffects,
    Writing
};