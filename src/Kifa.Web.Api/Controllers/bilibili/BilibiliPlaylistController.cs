using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Kifa.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliPlaylist.ModelId)]
    public class
        BilibiliPlaylistController : KifaDataController<BilibiliPlaylist, KifaServiceJsonClient<BilibiliPlaylist>> {
    }
}
