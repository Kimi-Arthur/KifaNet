using Kifa.Service;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Extensions;

public static class ActionResultExtensions {
    public static IActionResult And(this KifaActionResult result, ActionResult response) =>
        result.Status == KifaActionStatus.OK ? response : ((KifaApiActionResult) result).Convert();
}
