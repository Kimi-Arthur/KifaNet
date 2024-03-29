using System.Collections.Generic;
using System.Linq;
using Kifa.Infos;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers;

public class TvShowsController : KifaDataController<TvShow, TvShowJsonServiceClient> {
    [HttpGet("$format")]
    [HttpPost("$format")]
    public KifaApiActionResult<string>
        Format(string id, int seasonId, int? episodeId, string episodeIds)
        => episodeId == null
            ? Client.Format(id, seasonId, episodeIds.Split(",").Select(int.Parse).ToList())
            : Client.Format(id, seasonId, episodeId.Value);
}

public class TvShowJsonServiceClient : KifaServiceJsonClient<TvShow>, TvShowServiceClient {
    public string Format(string id, int seasonId, int episodeId)
        => Format(id, seasonId, new List<int> {
            episodeId
        });

    public string Format(string id, int seasonId, List<int> episodeIds) {
        var show = Get(id);
        var season = show.Seasons.First(s => s.Id == seasonId);
        var episodes = episodeIds.Select(episodeId => season.Episodes.First(e => e.Id == episodeId))
            .ToList();
        return show.Format(season, episodes);
    }
}
