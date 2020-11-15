using Microsoft.AspNetCore.Mvc;
using Pimix.Cloud.Swisscom;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : PimixController<SwisscomConfig> {
        static readonly SwisscomConfigJsonServiceClient client = new SwisscomConfigJsonServiceClient();
        protected override PimixServiceClient<SwisscomConfig> Client => client;
    }

    public class SwisscomConfigJsonServiceClient : PimixServiceJsonClient<SwisscomConfig>, SwisscomConfigServiceClient {
    }
}
