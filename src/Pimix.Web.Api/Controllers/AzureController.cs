using Microsoft.AspNetCore.Mvc;
using Pimix.Azure;
using Pimix.Service;

namespace Pimix.Web.Api.Controllers {
    [Route("api/azure")]
    public class AzureController : ControllerBase {
        [HttpGet("$update_dns")]
        public PimixActionResult UpdateDomainName(string name, string ip) {
            new DnsClient().ReplaceIp(name, ip);
            return RestActionResult.SuccessResult;
        }
    }
}