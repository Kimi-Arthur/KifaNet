using Kifa.Bilibili;
using Microsoft.AspNetCore.Mvc;

namespace Pimix.Web.Api.Controllers.bilibili {
    [Route("api/" + BilibiliBangumi.ModelId)]
    public class BilibiliBangumiController : PimixController<BilibiliBangumi, PimixServiceJsonClient<BilibiliBangumi>> {
    }
}
