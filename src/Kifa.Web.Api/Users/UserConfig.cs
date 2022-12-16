using System.Collections.Generic;

namespace Kifa.Web.Api.Users;

public class UserConfig {
    public string? DataFolder { get; set; }

    public List<string> AllowedNamespaces { get; set; } = new();
}
