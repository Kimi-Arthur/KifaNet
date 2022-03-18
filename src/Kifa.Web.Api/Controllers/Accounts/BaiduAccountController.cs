using Kifa.Cloud.BaiduCloud;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts;

[Route("api/" + BaiduAccount.ModelId)]
public class BaiduAccountController : OAuthAccountController<BaiduAccount> {
}
