using System.Net.Http;

namespace Kifa.Infos.Tmdb;

public class TmdbClient {
    #region public late string ApiKey { get; set; }

    static string? apiKey;

    public static string ApiKey {
        get => Late.Get(apiKey);
        set => Late.Set(ref apiKey, value);
    }

    #endregion

    static readonly HttpClient HttpClient = new();

    public TmdbSeriesResponse? GetSeries(string tmdbId, string language)
        => HttpClient.SendWithRetry<TmdbSeriesResponse>(
            new TmdbSeriesRequest(tmdbId, language, ApiKey));

    public TmdbSeasonResponse? GetSeason(string tmdbId, int seasonNumber, string language)
        => HttpClient.SendWithRetry<TmdbSeasonResponse>(
            new TmdbSeasonRequest(tmdbId, seasonNumber, language, ApiKey));
}
