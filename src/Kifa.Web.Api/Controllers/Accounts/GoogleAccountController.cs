using Kifa.Cloud.GooglePhotos;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts {
    [Route("api/" + GoogleAccount.ModelId)]
    public class GoogleAccountController : OAuthAccountController<GoogleAccount> {
    }
}