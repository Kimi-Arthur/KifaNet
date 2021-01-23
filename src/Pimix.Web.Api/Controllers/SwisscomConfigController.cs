using Microsoft.AspNetCore.Mvc;
using Pimix.Cloud.Swisscom;
using Kifa.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : KifaDataController<SwisscomConfig, SwisscomConfigJsonServiceClient> {
        [HttpPost("$update_quota")]
        public PimixActionResult UpdateQuota(string id, string accountId = null) {
            var config = Client.Get(id);
            if (accountId == null) {
                foreach (var account in config.Accounts.Values) {
                    account.UpdateQuota();
                    Client.Set(config);
                }
            } else {
                config.Accounts[accountId].UpdateQuota();
                Client.Set(config);
            }

            return KifaActionResult.SuccessActionResult;
        }
    }

    public class SwisscomConfigJsonServiceClient : KifaServiceJsonClient<SwisscomConfig>, SwisscomConfigServiceClient {
    }
}
