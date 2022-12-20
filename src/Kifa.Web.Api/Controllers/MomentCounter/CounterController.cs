using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter;

public class CounterController : KifaDataController<Counter, CounterJsonServiceClient> {
}

public class CounterJsonServiceClient : KifaServiceJsonClient<Counter>, CounterServiceClient {
}
