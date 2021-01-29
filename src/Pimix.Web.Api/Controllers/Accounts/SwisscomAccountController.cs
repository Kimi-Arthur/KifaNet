using Kifa.Cloud.Swisscom;
using Microsoft.AspNetCore.Mvc;

namespace Pimix.Web.Api.Controllers.Accounts {
    [Route("api/" + SwisscomAccount.ModelId)]
    public class SwisscomAccountController : KifaDataController<SwisscomAccount, KifaServiceJsonClient<SwisscomAccount>> {
    }
}
