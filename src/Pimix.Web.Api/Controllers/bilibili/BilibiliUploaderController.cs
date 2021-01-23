using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Pimix.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliUploader.ModelId)]
    public class
        BilibiliUploaderController : KifaDataController<BilibiliUploader, KifaServiceJsonClient<BilibiliUploader>> {
    }
}
