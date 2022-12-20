using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter;

public class EventController : KifaDataController<Event, EventJsonServiceClient> {
}

public class EventJsonServiceClient : KifaServiceJsonClient<Event>, EventServiceClient {
}
