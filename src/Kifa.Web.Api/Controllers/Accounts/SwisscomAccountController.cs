using Kifa.Cloud.Swisscom;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts {
    [Route("api/" + SwisscomAccount.ModelId)]
    public class
        SwisscomAccountController : KifaDataController<SwisscomAccount, KifaServiceJsonClient<SwisscomAccount>> {
        protected override bool AlwaysAutoRefresh => true;
    }
}
