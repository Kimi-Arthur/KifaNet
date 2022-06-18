using Kifa.Bilibili;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Bilibili;

[Route("api/" + BilibiliBangumi.ModelId)]
public class BilibiliBangumiController : KifaDataController<BilibiliBangumi,
    KifaServiceJsonClient<BilibiliBangumi>> {
}
