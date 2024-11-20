using System;
using System.Collections.Generic;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Cloud.MegaNz;

public class MegaNzAccount : DataModel, WithModelId<MegaNzAccount> {
    public static string ModelId => "mega_nz/accounts";

    public string Username { get; set; }

    public string Password { get; set; }

    public long TotalQuota { get; set; }
    public long UsedQuota { get; set; }

    [JsonIgnore]
    public long LeftQuota => TotalQuota - Math.Max(ExpectedQuota, UsedQuota);

    // This value will be filled when reserved.
    // When it is the same as UsedQuota, it can be safely discarded or ignored.
    public long ExpectedQuota { get; set; }

    public Dictionary<string, long> Reservations { get; set; } = new();
}
