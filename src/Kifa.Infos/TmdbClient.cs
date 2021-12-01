using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Service;

namespace Kifa.Infos.Tmdb {
    public class TmdbClient {
        public static string ApiKey { get; set; }
        public static APIList Apis { get; set; }

        static HttpClient client = new HttpClient();

        public TmdbSeries GetSeries(string tmdbId, string language) {
            var request = Apis.Series.GetRequest(new Dictionary<string, string> {
                ["sid"] = tmdbId,
                ["lang"] = language,
                ["api_key"] = ApiKey
            });

            return client.GetObject<TmdbSeries>(request);
        }

        public TmdbSeason GetSeason(string tmdbId, long seasonNumber, string language) {
            var request = Apis.Season.GetRequest(new Dictionary<string, string> {
                ["sid"] = tmdbId,
                ["season"] = seasonNumber.ToString(),
                ["lang"] = language,
                ["api_key"] = ApiKey
            });

            return client.GetObject<TmdbSeason>(request);
        }
    }

    public class APIList {
        public Api Languages { get; set; }
        public Api Season { get; set; }
        public Api Series { get; set; }
    }

    public class TmdbSeries {
        public object BackdropPath { get; set; }
        public List<CreatedBy> CreatedBy { get; set; }
        public List<long> EpisodeRunTime { get; set; }
        public Date FirstAirDate { get; set; }
        public List<Genre> Genres { get; set; }
        public Uri Homepage { get; set; }
        public long Id { get; set; }
        public bool InProduction { get; set; }
        public List<string> Languages { get; set; }
        public Date LastAirDate { get; set; }
        public LastEpisodeToAir LastEpisodeToAir { get; set; }
        public string Name { get; set; }
        public object NextEpisodeToAir { get; set; }
        public List<Network> Networks { get; set; }
        public long NumberOfEpisodes { get; set; }
        public long NumberOfSeasons { get; set; }
        public List<Region> OriginCountry { get; set; }
        public string OriginalLanguage { get; set; }
        public string OriginalName { get; set; }
        public string Overview { get; set; }
        public double Popularity { get; set; }
        public string PosterPath { get; set; }
        public List<object> ProductionCompanies { get; set; }
        public List<Season> Seasons { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public double VoteAverage { get; set; }
        public long VoteCount { get; set; }
    }

    public class CreatedBy {
        public long Id { get; set; }
        public string CreditId { get; set; }
        public string Name { get; set; }
        public long Gender { get; set; }
        public object ProfilePath { get; set; }
    }

    public class Genre {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class LastEpisodeToAir {
        public Date AirDate { get; set; }
        public long EpisodeNumber { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string ProductionCode { get; set; }
        public long SeasonNumber { get; set; }
        public long ShowId { get; set; }
        public object StillPath { get; set; }
        public long VoteAverage { get; set; }
        public long VoteCount { get; set; }
    }

    public class Network {
        public string Name { get; set; }
        public long Id { get; set; }
        public string LogoPath { get; set; }
        public Region OriginCountry { get; set; }
    }

    public class Season {
        public Date AirDate { get; set; }
        public long EpisodeCount { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string PosterPath { get; set; }
        public int SeasonNumber { get; set; }
    }

    public class TmdbSeason {
        public string Id { get; set; }
        public Date AirDate { get; set; }
        public List<Episode> Episodes { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public long TmdbSeasonId { get; set; }
        public string PosterPath { get; set; }
        public long SeasonNumber { get; set; }
    }

    public class Episode {
        public Date AirDate { get; set; }
        public int EpisodeNumber { get; set; }
        public long Id { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string ProductionCode { get; set; }
        public long SeasonNumber { get; set; }
        public long ShowId { get; set; }
        public string StillPath { get; set; }
        public double VoteAverage { get; set; }
        public long VoteCount { get; set; }
        public List<Crew> Crew { get; set; }
        public List<GuestStar> GuestStars { get; set; }
    }

    public class Crew {
        public long Id { get; set; }
        public string CreditId { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Job { get; set; }
        public long? Gender { get; set; }
        public string ProfilePath { get; set; }
    }

    public class GuestStar {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CreditId { get; set; }
        public string Character { get; set; }
        public long Order { get; set; }
        public long Gender { get; set; }
        public string ProfilePath { get; set; }
    }
}
