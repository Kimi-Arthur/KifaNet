using System.Collections.Generic;
using System.Linq;
using Kifa.Cloud.Swisscom;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers {
    [Route("api/" + SwisscomConfig.ModelId)]
    public class SwisscomConfigController : KifaDataController<SwisscomConfig, SwisscomConfigJsonServiceClient> {
        [HttpPost("$add_accounts")]
        public KifaApiActionResult AddAccounts([FromBody] AddAccountsRequest request) =>
            Client.AddAccounts(request.Id, request.Pattern, request.Accounts);
    }

    public class SwisscomConfigJsonServiceClient : KifaServiceJsonClient<SwisscomConfig>, SwisscomConfigServiceClient {
        public KifaActionResult AddAccounts(string id, string pattern, List<string> accounts) {
            var config = Get(id);
            var mappings = config.StorageMappings;
            foreach (var mapping in mappings) {
                if (mapping.Pattern == pattern) {
                    mapping.Accounts.AddRange(accounts.Where(a => !mapping.Accounts.Contains(a)));
                    Set(config);
                    return KifaActionResult.Success;
                }
            }

            return new KifaActionResult {
                Status = KifaActionStatus.BadRequest,
                Message = $"Cannot find pattern {pattern}"
            };
        }
    }
}
