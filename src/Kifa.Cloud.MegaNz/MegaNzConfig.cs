using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Cloud.MegaNz;

public class MegaNzConfig : DataModel, WithModelId<MegaNzConfig> {
    // TODO(#1): Implement service or remove.
    public static string ModelId => "configs/mega_nz";

    public static KifaServiceClient<MegaNzConfig> Client { get; set; } =
        new KifaServiceRestClient<MegaNzConfig>();

    public Dictionary<string, AccountInfo> Accounts { get; set; }
}

public class AccountInfo {
    public string Username { get; set; }

    public string Password { get; set; }
}
