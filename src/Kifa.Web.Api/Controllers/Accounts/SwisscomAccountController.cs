using System.Collections.Generic;
using System.Linq;
using Kifa.Cloud.Swisscom;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts {
    [Route("api/" + SwisscomAccount.ModelId)]
    public class SwisscomAccountController : KifaDataController<SwisscomAccount, SwisscomAccountJsonServiceClient> {
        protected override bool AlwaysAutoRefresh => true;

        [HttpGet("$get_top_accounts")]
        [HttpPost("$get_top_accounts")]
        public KifaApiActionResult<List<SwisscomAccount>> GetTopAccounts() => Client.GetTopAccounts();
    }

    public class
        SwisscomAccountJsonServiceClient : KifaServiceJsonClient<SwisscomAccount>, SwisscomAccountServiceClient {
        public List<SwisscomAccount> GetTopAccounts() {
            // 10 MB
            const int limit = 100 << 20;
            var allGoodAccounts = new List<SwisscomAccount>();

            foreach (var account in List().Values) {
                if (account.TotalQuota > 0 && account.LeftQuota < limit) {
                    continue;
                }

                account.Fill();
                Set(account);
                if (account.LeftQuota >= limit) {
                    allGoodAccounts.Add(account);
                }
            }

            return allGoodAccounts.OrderBy(a => -a.LeftQuota).ToList();
        }
    }
}
