using Microsoft.AspNetCore.Mvc;
using Pimix.Cloud.Swisscom;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : PimixController<SwisscomConfig, SwisscomConfigJsonServiceClient> {
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

            return RestActionResult.SuccessResult;
        }
    }

    public class SwisscomConfigJsonServiceClient : PimixServiceJsonClient<SwisscomConfig>, SwisscomConfigServiceClient {
    }
}
