using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Kifa.Web.Api.Controllers.Bilibili;

[Route("api/" + BilibiliVideo.ModelId)]
public class BilibiliVideoController : KifaDataController<BilibiliVideo,
    KifaServiceJsonClient<BilibiliVideo>> {
}