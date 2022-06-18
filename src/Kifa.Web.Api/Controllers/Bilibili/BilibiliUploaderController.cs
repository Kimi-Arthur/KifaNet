using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Kifa.Web.Api.Controllers.Bilibili;

[Route("api/" + BilibiliUploader.ModelId)]
public class BilibiliUploaderController : KifaDataController<BilibiliUploader,
    KifaServiceJsonClient<BilibiliUploader>> {
}
