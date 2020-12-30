using Microsoft.AspNetCore.Mvc;
using Pimix.Bilibili;

namespace Pimix.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliUploader.ModelId)]
    public class
        BilibiliUploaderController : PimixController<BilibiliUploader, PimixServiceJsonClient<BilibiliUploader>> {
    }
}
