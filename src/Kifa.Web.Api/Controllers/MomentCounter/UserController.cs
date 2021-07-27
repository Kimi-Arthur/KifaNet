using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter {
    [Route("api/" + Apps.MomentCounter.User.ModelId)]
    public class UserController : KifaDataController<User, UserJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class UserJsonServiceClient : KifaServiceJsonClient<User>, UserServiceClient {
    }
}
