using System;
using Kifa.Apps.MomentCounter;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Kifa.Web.Api.Controllers.MomentCounter;

public class UserController : KifaDataController<User, UserJsonServiceClient> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpPost("$add_counter")]
    public KifaApiActionResult<string> AddCounter([FromBody] AddCounterRequest request) {
        Logger.Trace($"Request: {request.ToPrettyJson()}");
        return Client.AddCounter(Client.Get(request.UserId), request.Counter);
    }

    [HttpPost("$remove_counter")]
    public KifaApiActionResult<string> RemoveCounter([FromBody] RemoveCounterRequest request) {
        Logger.Trace($"Request: {request.ToPrettyJson()}");
        return Client.RemoveCounter(Client.Get(request.UserId), request.CounterId);
    }
}

public class UserJsonServiceClient : KifaServiceJsonClient<User>, User.ServiceClient {
    public string AddCounter(User user, Counter counter) {
        counter.Id = $"{user.Id}/{user.Settings.NextCounter++}";
        Counter.Client.Set(counter);
        user.Counters.Add(counter.Id);
        Update(user);
        return counter.Id;
    }

    public string RemoveCounter(User user, string counterId) {
        Counter.Client.Delete(counterId);
        user.Counters.Remove(counterId);
        Update(user);
        return counterId;
    }

    public string AddEvent(User user, Counter counter, Event @event)
        => throw new NotImplementedException();
}
