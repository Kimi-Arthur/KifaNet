using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Pimix.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliVideo.ModelId)]
    public class BilibiliVideoController : PimixController<BilibiliVideo, PimixServiceJsonClient<BilibiliVideo>> {
    }
}