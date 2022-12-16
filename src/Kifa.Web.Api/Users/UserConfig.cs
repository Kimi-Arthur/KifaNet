using System.Collections.Generic;

namespace Kifa.Web.Api.Users;

public class UserConfig {
    // A mapping from namespace prefix to the root data folder, excluding the model id part.
    // A value of null means using the default data folder.
    public Dictionary<string, string> DataFolders { get; set; } = new();

    public List<string> AllowedNamespaces { get; set; } = new();
}
