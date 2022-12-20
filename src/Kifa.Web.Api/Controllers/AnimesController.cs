using System.Linq;
using Kifa.Infos;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers;

public class AnimesController : KifaDataController<Anime, AnimeJsonServiceClient> {
    [HttpGet("$format")]
    public KifaApiActionResult<string?> Format(string id, int seasonId, int episodeId)
        => Client.Format(id, seasonId, episodeId);
}

public class AnimeJsonServiceClient : KifaServiceJsonClient<Anime>, AnimeServiceClient {
    public string? Format(string id, int seasonId, int episodeId) {
        var show = Get(id);

        var season = show?.Seasons?.First(s => s.Id == seasonId);

        if (season == null) {
            return null;
        }

        var episode = season.Episodes?.First(e => e.Id == episodeId);
        return episode == null ? null : show.Format(season, episode);
    }
}
