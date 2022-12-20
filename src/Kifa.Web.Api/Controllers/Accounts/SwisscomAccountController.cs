using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Cloud.Swisscom;
using Kifa.Service;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Accounts;

public class SwisscomAccountController : KifaDataController<SwisscomAccount,
    SwisscomAccountJsonServiceClient> {
}

public class SwisscomAccountJsonServiceClient : KifaServiceJsonClient<SwisscomAccount>,
    SwisscomAccount.ServiceClient {
}
