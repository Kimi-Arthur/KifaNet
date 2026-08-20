using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kifa.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using NLog;

namespace Kifa.Web.Api;

public class UserFilter : ActionFilterAttribute {
    public static string DefaultUser { get; set; } = "";

    // The map from user to its data configs.
    // The config is a mapping from namespace prefix to the root data folder.
    // A value of null means using the default data folder.
    public static Dictionary<string, Dictionary<string, string?>> Configs { get; set; } = new();

    static readonly Regex NamePattern = new(@"(,|^)CN=([^,]+)(,|$)");

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string GetUser(HttpRequest request) {
        var values = request.Headers.GetValueOrDefault("X-SSL-USER", StringValues.Empty);
        var match = NamePattern.Match(values.Count == 0 ? "" : values[0] ?? "");
        return match.Success ? match.Groups[2].Value : DefaultUser;
    }

    public override void OnActionExecuting(ActionExecutingContext context) {
        if (context.Result != null) {
            return;
        }

        var user = GetUser(context.HttpContext.Request);
        Logger.Trace($"Configuring for user {user}");
        if (Configs.TryGetValue(user, out var config)) {
            Logger.Trace($"Found config for {user}.");

            // The property is essentially thread local, so it only affects current request.
            KifaServiceJsonClient.DataFolders = config;
            return;
        }

        throw new DataModelNotFoundException($"No config found for user {user}.");
    }
}
