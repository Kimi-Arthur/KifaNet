using Kifa.Languages.Cambridge;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Cambridge;

public class
    CambridgePagesController : KifaDataController<CambridgePage, CambridgePageJsonServiceClient> {
}

public class CambridgePageJsonServiceClient : KifaServiceJsonClient<CambridgePage>,
    CambridgePageServiceClient {
}
