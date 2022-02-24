using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter; 

[Route("api/" + Counter.ModelId)]
public class CounterController : KifaDataController<Counter, CounterJsonServiceClient> {
    protected override bool ShouldAutoRefresh => false;
}

public class CounterJsonServiceClient : KifaServiceJsonClient<Counter>, CounterServiceClient {
}