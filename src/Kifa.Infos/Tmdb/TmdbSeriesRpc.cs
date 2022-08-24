using System.Collections.Generic;
using Kifa.Rpc;

namespace Kifa.Infos.Tmdb;

public sealed class TmdbSeriesRequest : ParameterizedRequest {
    public override string UrlPattern => "https://api.themoviedb.org/3/tv/{sid}?api_key={api_key}&language={lang}";

    public TmdbSeriesRequest(string sid, Language language, string apiKey) {
        parameters = new Dictionary<string, string> {
            {"sid", sid},
            {"api_key", apiKey},
            {"lang", language.Code}
        };
    }
}

public class TmdbSeriesResponse {
    public bool Adult { get; set; }
    public string BackdropPath { get; set; }
    public List<Creator> CreatedBy { get; set; }
    public List<long> EpisodeRunTime { get; set; }
    public Date FirstAirDate { get; set; }
    public List<Genre> Genres { get; set; }
    public string Homepage { get; set; }
    public long Id { get; set; }
    public bool InProduction { get; set; }
    public List<string> Languages { get; set; }
    public Date LastAirDate { get; set; }
    public EpisodeToAir LastEpisodeToAir { get; set; }
    public string Name { get; set; }
    public EpisodeToAir NextEpisodeToAir { get; set; }
    public List<Network> Networks { get; set; }
    public long NumberOfEpisodes { get; set; }
    public long NumberOfSeasons { get; set; }
    public List<string> OriginCountry { get; set; }
    public string OriginalLanguage { get; set; }
    public string OriginalName { get; set; }
    public string Overview { get; set; }
    public double Popularity { get; set; }
    public string PosterPath { get; set; }
    public List<Network> ProductionCompanies { get; set; }
    public List<ProductionCountry> ProductionCountries { get; set; }
    public List<Season> Seasons { get; set; }
    public List<SpokenLanguage> SpokenLanguages { get; set; }
    public string Status { get; set; }
    public string Tagline { get; set; }
    public string Type { get; set; }
    public double VoteAverage { get; set; }
    public long VoteCount { get; set; }
}

public class Creator
{
    public long Id { get; set; }
    public string CreditId { get; set; }
    public string Name { get; set; }
    public long Gender { get; set; }
    public string ProfilePath { get; set; }
}

public class Genre {
    public long Id { get; set; }
    public string Name { get; set; }
}

public class EpisodeToAir {
    public Date AirDate { get; set; }
    public long EpisodeNumber { get; set; }
    public long Id { get; set; }
    public string Name { get; set; }
    public string Overview { get; set; }
    public string ProductionCode { get; set; }
    public long Runtime { get; set; }
    public long SeasonNumber { get; set; }
    public long ShowId { get; set; }
    public string StillPath { get; set; }
    public double VoteAverage { get; set; }
    public long VoteCount { get; set; }
}

public class Network {
    public long Id { get; set; }
    public string Name { get; set; }
    public string LogoPath { get; set; }
    public string OriginCountry { get; set; }
}

public class ProductionCountry {
    public string Iso3166_1 { get; set; }
    public string Name { get; set; }
}

public class Season {
    public Date AirDate { get; set; }
    public int EpisodeCount { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Overview { get; set; }
    public string PosterPath { get; set; }
    public int SeasonNumber { get; set; }
}

public class SpokenLanguage {
    public string EnglishName { get; set; }
    public string Iso639_1 { get; set; }
    public string Name { get; set; }
}
