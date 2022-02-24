using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Cloud.MegaNz; 

public class MegaNzConfig : DataModel<MegaNzConfig> {
    public const string ModelId = "configs/mega_nz";

    static KifaServiceClient<MegaNzConfig> client;

    public static KifaServiceClient<MegaNzConfig> Client => client ??= new KifaServiceRestClient<MegaNzConfig>();

    public Dictionary<string, AccountInfo> Accounts { get; set; }
}

public class AccountInfo {
    public string Username { get; set; }

    public string Password { get; set; }
}