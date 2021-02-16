using Kifa.Cloud.Swisscom;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : KifaDataController<SwisscomConfig, KifaServiceJsonClient<SwisscomConfig>> {
    }
}
