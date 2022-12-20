using Kifa.Bilibili;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Bilibili;

public class BilibiliMangaController : KifaDataController<BilibiliManga,
    KifaServiceJsonClient<BilibiliManga>> {
}
