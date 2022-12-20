using Microsoft.AspNetCore.Mvc;
using Kifa.Bilibili;

namespace Kifa.Web.Api.Controllers.Bilibili;

public class BilibiliVideoController : KifaDataController<BilibiliVideo,
    KifaServiceJsonClient<BilibiliVideo>> {
}
