using Microsoft.AspNetCore.Mvc;
using Pimix.Bilibili;

namespace Pimix.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliVideo.ModelId)]
    public class BilibiliVideoController : PimixController<BilibiliVideo, PimixServiceJsonClient<BilibiliVideo>> {
    }
}
