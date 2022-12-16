using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kifa.Web.Api.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Kifa.Web.Api;

public class UserFilter : ActionFilterAttribute {
    public static string DefaultUser { get; set; } = "";
    public static Dictionary<string, string> UserFolders { get; set; } = new();

    static readonly Regex NamePattern = new(@"(,|^)CN=([^,]+)(,|$)");

    public override void OnActionExecuting(ActionExecutingContext context) {
        var values =
            context.HttpContext.Request.Headers.GetValueOrDefault("X-SSL-USER", StringValues.Empty);
        var match = NamePattern.Match(values.Count == 0 ? "" : values[0] ?? "");
        var user = match.Success ? match.Groups[1].Value : DefaultUser;
        if (UserFolders.ContainsKey(user)) {
            // The property is essentially thread local, so only affecting current request.
            KifaServiceJsonClient.DataFolder = UserFolders[user];
        }
    }
}
