using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Pimix.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliPlaylist.ModelId)]
    public class
        BilibiliPlaylistController : PimixController<BilibiliPlaylist, PimixServiceJsonClient<BilibiliPlaylist>> {
    }
}