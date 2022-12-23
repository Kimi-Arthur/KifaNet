using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Cloud.Swisscom;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts;

public class SwisscomAccountQuotaController : KifaDataController<SwisscomAccountQuota,
    SwisscomAccountQuotaJsonServiceClient> {
    [HttpGet("$get_top_accounts")]
    [HttpPost("$get_top_accounts")]
    public KifaApiActionResult<List<SwisscomAccountQuota>> GetTopAccounts()
        => Client.GetTopAccounts();

    [HttpPost("$reserve_quota")]
    public KifaApiActionResult ReserveQuota([FromBody] ReserveQuotaRequest request)
        => Client.ReserveQuota(request.Id, request.Path, request.Length);
}

public class ReserveQuotaRequest {
    public string Id { get; set; }
    public string Path { get; set; }
    public long Length { get; set; }
}

public class SwisscomAccountQuotaJsonServiceClient : KifaServiceJsonClient<SwisscomAccountQuota>,
    SwisscomAccountQuota.ServiceClient {
    public List<SwisscomAccountQuota> GetTopAccounts() {
        // 10 MB
        const int limit = 100 << 20;
        var allGoodAccounts = new List<SwisscomAccountQuota>();

        foreach (var account in List().Values) {
            if (account.TotalQuota > 0 && account.LeftQuota < limit) {
                continue;
            }

            if (account.LeftQuota >= limit) {
                allGoodAccounts.Add(account);
            }
        }

        return allGoodAccounts.OrderBy(a => -a.LeftQuota).ToList();
    }

    public KifaActionResult ReserveQuota(string id, string path, long length) {
        var data = Get(id);
        data.Reservations.Add(path, length);
        data.ExpectedQuota = Math.Max(data.ExpectedQuota, data.UsedQuota) + length;
        return Update(data);
    }

    public KifaActionResult ClearReserve(string id) {
        var data = Get(id);
        data.ExpectedQuota = 0;
        return Update(data);
    }
}
