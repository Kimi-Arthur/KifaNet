using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Kifa.Web.Api.Controllers.Bilibili;

public class BilibiliUploaderController : KifaDataController<BilibiliUploader,
    KifaServiceJsonClient<BilibiliUploader>> {
}
