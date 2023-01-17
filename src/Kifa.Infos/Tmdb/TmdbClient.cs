using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

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

    public TmdbSeriesResponse? GetSeries(string tmdbId, Language language)
        => HttpClient.Call(new TmdbSeriesRpc(tmdbId, language, ApiKey));

    public TmdbSeasonResponse? GetSeason(string tmdbId, int seasonNumber, Language language)
        => HttpClient.Call(new TmdbSeasonRpc(tmdbId, seasonNumber, language, ApiKey));

    static readonly List<(Regex Pattern, MatchEvaluator Replacement)> SeasonNameReplacements =
        new() {
            (new Regex(@"Season \d+|Staffel \d+|Stagione \d+|シーズン\d+|第 *[零一二三四五六七八九十百千万0-9]+ *季"),
                _ => ""),
            (new Regex(@"Season \w+:"), _ => ""),
        };

    public static string? NormalizeSeasonTitle(string seasonName) {
        foreach (var (pattern, replacement) in SeasonNameReplacements) {
            seasonName = pattern.Replace(seasonName, replacement);
        }

        seasonName = seasonName.Trim();

        return string.IsNullOrEmpty(seasonName) ? null : seasonName;
    }
}
