using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Kifa.Web.Api.Extensions;

public static class ControllerExtensions {
    const string OriginalUriHeader = "X-Original-URI";

    public static string ForAction(this ControllerBase controller, string action,
        RouteValueDictionary? values = null) {
        var actionPath = values == null
            ? controller.Url.Action(action)
            : controller.Url.Action(action, values);
        var originalPath = controller.Request.Path;
        var originalFullUrl =
            controller.Request.Headers.GetValueOrDefault(OriginalUriHeader,
                controller.Request.GetDisplayUrl())[0];
        return $"{originalFullUrl.Substring(0, originalFullUrl.IndexOf(originalPath))}{actionPath}";
    }
}
