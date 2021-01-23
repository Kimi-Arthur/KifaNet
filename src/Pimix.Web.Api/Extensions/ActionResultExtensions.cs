using Kifa.Service;
using Microsoft.AspNetCore.Mvc;
using Pimix.Web.Api.Controllers;

namespace Pimix.Web.Api.Extensions {
    public static class ActionResultExtensions {
        public static IActionResult And(this KifaActionResult result, ActionResult response) =>
            result.Status == KifaActionStatus.OK ? response : ((PimixActionResult) result).Convert();
    }
}
