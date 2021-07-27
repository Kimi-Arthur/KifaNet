using System;
using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Kifa.Web.Api.Controllers.MomentCounter {
    [Route("api/" + Apps.MomentCounter.User.ModelId)]
    public class UserController : KifaDataController<User, UserJsonServiceClient> {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected override bool ShouldAutoRefresh => false;

        [HttpPost("$add_counter")]
        public KifaApiActionResult<string> AddCounter([FromBody] AddCounterRequest request) {
            logger.Trace(request);
            return Client.AddCounter(Client.Get(request.UserId), request.Counter);
        }
    }

    public class AddCounterRequest : KifaRequest {
        public string UserId { get; set; }
        public Counter Counter { get; set; }
    }

    public class UserJsonServiceClient : KifaServiceJsonClient<User>, UserServiceClient {
        public string AddCounter(User user, Counter counter) {
            counter.Id = $"{user.Id}/{user.Settings.NextCounter++}";
            Counter.Client.Set(counter);
            user.Counters.Add(counter.Id);
            Set(user);
            return counter.Id;
        }

        public string AddEvent(User user, Counter counter, Event @event) {
            throw new NotImplementedException();
        }
    }
}
