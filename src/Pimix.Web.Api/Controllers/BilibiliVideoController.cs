using Microsoft.AspNetCore.Mvc;
using Pimix.Bilibili;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + BilibiliVideo.ModelId)]
    public class BilibiliVideoController : PimixController<BilibiliVideo, PimixServiceJsonClient<BilibiliVideo>> {
    }
}
