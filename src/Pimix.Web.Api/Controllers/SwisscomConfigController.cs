using Microsoft.AspNetCore.Mvc;
using Kifa.Cloud.Swisscom;
using Kifa.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : KifaDataController<SwisscomConfig, KifaServiceJsonClient<SwisscomConfig>> {
    }
}
