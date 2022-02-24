using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Kifa.Infos;

namespace Kifa.Web.Api.Controllers; 

[Route("api/" + Anime.ModelId)]
public class AnimesController : KifaDataController<Anime, AnimeJsonServiceClient> {
    [HttpGet("$format")]
    public KifaApiActionResult<string> Format(string id, int seasonId, int episodeId) =>
        Client.Format(id, seasonId, episodeId);
}

public class AnimeJsonServiceClient : KifaServiceJsonClient<Anime>, AnimeServiceClient {
    public string Format(string id, int seasonId, int episodeId) {
        var show = Get(id);
        var season = show.Seasons.First(s => s.Id == seasonId);
        var episode = season.Episodes.First(e => e.Id == episodeId);
        return show.Format(season, episode);
    }
}