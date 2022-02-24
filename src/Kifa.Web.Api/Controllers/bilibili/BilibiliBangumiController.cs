using Kifa.Bilibili;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.bilibili; 

[Route("api/" + BilibiliBangumi.ModelId)]
public class BilibiliBangumiController : KifaDataController<BilibiliBangumi, KifaServiceJsonClient<BilibiliBangumi>> {
}