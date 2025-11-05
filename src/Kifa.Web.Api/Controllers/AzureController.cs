using Kifa.Azure;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers;

[Route("api/azure")]
public class AzureController : ControllerBase {
    [HttpGet("$update_dns")]
    public KifaApiActionResult UpdateDomainName(string name, string ip) {
        return KifaActionResult.FromAction(() => new DnsClient().ReplaceIp(name, ip));
    }
}
