using System.Collections.Generic;

namespace Kifa.Web.Api.Users;

public class UserConfig {
    #region public late string DataFolder { get; set; }

    string? dataFolder;

    public string DataFolder {
        get => Late.Get(dataFolder);
        set => Late.Set(ref dataFolder, value);
    }

    #endregion

    public List<string> AllowedEndpoints { get; set; } = new();
}
