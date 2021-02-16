using Microsoft.AspNetCore.Mvc;
using Kifa.Azure;
using Kifa.Service;

namespace Kifa.Web.Api.Controllers {
    [Route("api/azure")]
    public class AzureController : ControllerBase {
        [HttpGet("$update_dns")]
        public PimixActionResult UpdateDomainName(string name, string ip) {
            new DnsClient().ReplaceIp(name, ip);
            return KifaActionResult.SuccessActionResult;
        }
    }
}
