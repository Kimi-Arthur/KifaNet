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
            logger.Trace($"Request: {request.ToPrettyJson()}");
            return Client.AddCounter(Client.Get(request.UserId), request.Counter);
        }

        [HttpPost("$remove_counter")]
        public KifaApiActionResult<string> RemoveCounter([FromBody] RemoveCounterRequest request) {
            logger.Trace($"Request: {request.ToPrettyJson()}");
            return Client.RemoveCounter(Client.Get(request.UserId), request.CounterId);
        }
    }

    public class UserJsonServiceClient : KifaServiceJsonClient<User>, UserServiceClient {
        public string AddCounter(User user, Counter counter) {
            counter.Id = $"{user.Id}/{user.Settings.NextCounter++}";
            Counter.Client.Set(counter);
            user.Counters.Add(counter.Id);
            Set(user);
            return counter.Id;
        }

        public string RemoveCounter(User user, string counterId) {
            Counter.Client.Delete(counterId);
            user.Counters.Remove(counterId);
            Set(user);
            return counterId;
        }

        public string AddEvent(User user, Counter counter, Event @event) {
            throw new NotImplementedException();
        }
    }
}
