using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Cloud.Swisscom;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts;

[Route("api/" + SwisscomAccount.ModelId)]
public class SwisscomAccountController : KifaDataController<SwisscomAccount,
    SwisscomAccountJsonServiceClient> {
    protected override bool AlwaysAutoRefresh => true;

    [HttpGet("$get_top_accounts")]
    [HttpPost("$get_top_accounts")]
    public KifaApiActionResult<List<SwisscomAccount>> GetTopAccounts() => Client.GetTopAccounts();

    [HttpPost("$reserve_quota")]
    public KifaApiActionResult ReserveQuota([FromBody] ReserveQuotaRequest request)
        => Client.ReserveQuota(request.Id, request.Length);
}

public class ReserveQuotaRequest {
    public string Id { get; set; }
    public long Length { get; set; }
}

public class SwisscomAccountJsonServiceClient : KifaServiceJsonClient<SwisscomAccount>,
    SwisscomAccountServiceClient {
    public List<SwisscomAccount> GetTopAccounts() {
        // 10 MB
        const int limit = 100 << 20;
        var allGoodAccounts = new List<SwisscomAccount>();

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

    public KifaActionResult ReserveQuota(string id, long length) {
        var data = Get(id, true);
        data.ExpectedQuota = Math.Max(data.ExpectedQuota, data.UsedQuota) + length;
        return Set(data);
    }

    public KifaActionResult ClearReserve(string id) {
        var data = Get(id, true);
        data.ExpectedQuota = 0;
        return Set(data);
    }
}
