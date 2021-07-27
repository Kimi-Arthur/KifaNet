using System;
using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.MomentCounter {
    [Route("api/" + Apps.MomentCounter.User.ModelId)]
    public class UserController : KifaDataController<User, UserJsonServiceClient> {
        protected override bool ShouldAutoRefresh => false;

        [HttpPost("$add_counter")]
        public KifaApiActionResult<string> AddCounter(string userId, Counter counter) =>
            Client.AddCounter(Get(userId).Value, counter);
    }

    public class UserJsonServiceClient : KifaServiceJsonClient<User>, UserServiceClient {
        public string AddCounter(User user, Counter counter) {
            counter.Id = $"{user.Id}/{user.Settings.NextCounter++}";
            user.Counters.Add(counter.Id);
            Set(user);
            return counter.Id;
        }

        public string AddEvent(User user, Counter counter, Event @event) {
            throw new NotImplementedException();
        }
    }
}
