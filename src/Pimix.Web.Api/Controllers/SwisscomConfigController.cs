using Microsoft.AspNetCore.Mvc;
using Pimix.Cloud.Swisscom;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : PimixController<SwisscomConfig> {
        static readonly SwisscomConfigServiceClient client = new SwisscomConfigJsonServiceClient();
        protected override PimixServiceClient<SwisscomConfig> Client => client;

        [HttpPost("$update_quota")]
        public PimixActionResult UpdateQuota(string id, string accountId) {
            client.UpdateQuota(id, accountId);
            return RestActionResult.SuccessResult;
        }
    }

    public class SwisscomConfigJsonServiceClient : PimixServiceJsonClient<SwisscomConfig>, SwisscomConfigServiceClient {
    }
}
