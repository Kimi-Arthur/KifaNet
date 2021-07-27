using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter {
    [Route("api/" + Event.ModelId)]
    public class EventController : KifaDataController<Event, EventJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;
    }

    public class EventJsonServiceClient : KifaServiceJsonClient<Event>, EventServiceClient {
    }
}
