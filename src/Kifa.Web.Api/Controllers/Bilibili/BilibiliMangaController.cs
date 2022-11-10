using Kifa.Bilibili;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Bilibili;

[Route("api/" + BilibiliManga.ModelId)]
public class BilibiliMangaController : KifaDataController<BilibiliManga,
    KifaServiceJsonClient<BilibiliManga>> {
}
