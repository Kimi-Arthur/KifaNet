using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Infos.Tmdb;

public sealed class TmdbSeasonRpc : KifaJsonParameterizedRpc<TmdbSeasonResponse> {
    protected override string Url
        => "https://api.themoviedb.org/3/tv/{sid}/season/{season}?api_key={api_key}&language={lang}";

    protected override HttpMethod Method => HttpMethod.Get;

    public TmdbSeasonRpc(string sid, int seasonId, Language language, string apiKey) {
        Parameters = new () {
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
    public string? Department { get; set; }
    public string CreditId { get; set; }
    public bool Adult { get; set; }
    public long Gender { get; set; }
    public long Id { get; set; }
    public string? KnownForDepartment { get; set; }
    public string Name { get; set; }
    public string OriginalName { get; set; }
    public double Popularity { get; set; }
    public string ProfilePath { get; set; }
    public string Character { get; set; }
    public long? Order { get; set; }
}
