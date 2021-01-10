using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Pimix.Web.Api {
    public static class ControllerExtensions {
        const string OriginalUriHeader = "X-Original-URI";

        public static string ForAction(this ControllerBase controller, string action) {
            var actionPath = controller.Url.Action(action);
            var originalPath = controller.Request.Path;
            var originalFullUrl =
                controller.Request.Headers.GetValueOrDefault(OriginalUriHeader, controller.Request.GetDisplayUrl())[0];
            return $"{originalFullUrl.Substring(0, originalFullUrl.IndexOf(originalPath))}{actionPath}";
        }
    }
}
