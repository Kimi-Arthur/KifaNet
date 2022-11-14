using Kifa.Languages.Dwds;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.German;

[Route("api/" + DwdsPage.ModelId)]
public class DwdsPagesController : KifaDataController<DwdsPage, DwdsPageJsonServiceClient> {
}

public class DwdsPageJsonServiceClient : KifaServiceJsonClient<DwdsPage>, DwdsPage.ServiceClient {
}
