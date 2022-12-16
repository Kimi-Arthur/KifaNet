using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Web.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using NLog;

namespace Kifa.Web.Api.Users;

public class UserFilter : ActionFilterAttribute {
    public static string DefaultUser { get; set; } = "";
    public static Dictionary<string, UserConfig> Configs { get; set; } = new();

    static readonly Regex NamePattern = new(@"(,|^)CN=([^,]+)(,|$)");

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void OnActionExecuting(ActionExecutingContext context) {
        if (context.Result != null) {
            return;
        }

        var values =
            context.HttpContext.Request.Headers.GetValueOrDefault("X-SSL-USER", StringValues.Empty);
        var match = NamePattern.Match(values.Count == 0 ? "" : values[0] ?? "");
        var user = match.Success ? match.Groups[2].Value : DefaultUser;
        Logger.Trace($"Configuring for user {user}");
        if (Configs.TryGetValue(user, out var config)) {
            Logger.Trace($"Found config for {user}.");

            var controllerName = context.Controller.GetType().ToString();
            Logger.Trace($"Configuring for controller {controllerName}.");

            if (!config.AllowedNamespaces.Any(ns => controllerName.StartsWith(ns))) {
                Logger.Warn($"Controller {controllerName} not allowed for user {user}.");
                context.Result = new NotFoundResult();
                return;
            }

            // The property is essentially thread local, so only affecting current request.
            KifaServiceJsonClient.DataFolder =
                config.DataFolder ?? KifaServiceJsonClient.DefaultDataFolder;
        }
    }
}
