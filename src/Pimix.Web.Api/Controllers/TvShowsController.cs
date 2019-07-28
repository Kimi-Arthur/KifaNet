using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Pimix.Infos;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    public class TvShowsController : PimixController<TvShow> {
        static readonly TvShowServiceClient client = new TvShowJsonServiceClient();

        protected override PimixServiceClient<TvShow> Client => client;

        [HttpGet("$format")]
        public PimixActionResult<string> Format(string id, int seasonId, int episodeId)
            => client.Format(id, seasonId, episodeId);
    }

    public class TvShowJsonServiceClient : PimixServiceJsonClient<TvShow>,
        TvShowServiceClient {
        public string Format(string id, int seasonId, int episodeId) {
            var show = Get(id);
            var season = show.Seasons.First(s => s.Id == seasonId);
            var episode = season.Episodes.First(e => e.Id == episodeId);
            return show.Format(season, episode);
        }
    }
}
