using System;
using Kifa.Web.Api.Controllers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NLog;

namespace Kifa.Web.Api.Extensions;

public static class ControllerExtensions {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
        return $"{originalFullUrl[..originalFullUrl.IndexOf(originalPath)]}{actionPath}";
    }

    public static Type? GetDataControllerType(this Type type) {
        if (type.IsAbstract || type.ContainsGenericParameters) {
            Logger.Debug($"{type} is abstract or contains generic parameters.");
            return null;
        }

        var baseType = type;

        while (baseType != null) {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() ==
                typeof(KifaDataController<,>)) {
                Logger.Debug($"Found {baseType.FullName}");
                return baseType;
            }

            baseType = baseType.BaseType;
        }

        return null;
    }
}
