using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter;

[Route("api/" + Unit.ModelId)]
public class UnitController : KifaDataController<Unit, UnitJsonServiceClient> {
    protected override bool ShouldAutoRefresh => false;
}

public class UnitJsonServiceClient : KifaServiceJsonClient<Unit>, UnitServiceClient {
}
