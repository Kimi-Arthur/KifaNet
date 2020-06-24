using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Pimix.Infos;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + TvShow.ModelId)]
    public class TvShowsController : PimixController<TvShow> {
        static readonly TvShowServiceClient client = new TvShowJsonServiceClient();

        protected override PimixServiceClient<TvShow> Client => client;

        [HttpGet("$format")]
        [HttpPost("$format")]
        public PimixActionResult<string> Format(string id, int seasonId, int? episodeId, string episodeIds) =>
            episodeId == null
                ? client.Format(id, seasonId, episodeIds.Split(",").Select(int.Parse).ToList())
                : client.Format(id, seasonId, episodeId.Value);
    }

    public class TvShowJsonServiceClient : PimixServiceJsonClient<TvShow>, TvShowServiceClient {
        public string Format(string id, int seasonId, int episodeId) {
            return Format(id, seasonId, new List<int> {episodeId});
        }

        public string Format(string id, int seasonId, List<int> episodeIds) {
            var show = Get(id);
            var season = show.Seasons.First(s => s.Id == seasonId);
            var episodes = episodeIds.Select(episodeId => season.Episodes.First(e => e.Id == episodeId)).ToList();
            return show.Format(season, episodes);
        }
    }
}
