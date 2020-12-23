using Microsoft.AspNetCore.Mvc;
using Pimix.Bilibili;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + BilibiliPlaylist.ModelId)]
    public class
        BilibiliPlaylistController : PimixController<BilibiliPlaylist, PimixServiceJsonClient<BilibiliPlaylist>> {
    }
}
