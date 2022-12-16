using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kifa.Web.Api.Extensions;
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
        var values =
            context.HttpContext.Request.Headers.GetValueOrDefault("X-SSL-USER", StringValues.Empty);
        var match = NamePattern.Match(values.Count == 0 ? "" : values[0] ?? "");
        var user = match.Success ? match.Groups[2].Value : DefaultUser;
        Logger.Trace($"Found user: {user}");
        if (Configs.ContainsKey(user)) {
            Logger.Trace($"Configuring for user: {user}");

            var config = Configs[user];
            // The property is essentially thread local, so only affecting current request.
            KifaServiceJsonClient.DataFolder = config.DataFolder;
        }
    }
}
