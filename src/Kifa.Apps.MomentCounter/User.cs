using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter;

public class User : DataModel, WithModelId {
    public static string ModelId => "moment_counter/users";

    public string Name { get; set; }

    // This also includes things like next ids.
    public Settings Settings { get; set; } = new();

    public List<Link<Counter>> Counters { get; set; } = new();
}

public interface UserServiceClient : KifaServiceClient<User> {
    string AddCounter(User user, Counter counter);
    string RemoveCounter(User user, string counterId);
    string AddEvent(User user, Counter counter, Event @event);
}

public class AddCounterRequest {
    public string UserId { get; set; }
    public Counter Counter { get; set; }
}

public class RemoveCounterRequest {
    public string UserId { get; set; }
    public string CounterId { get; set; }
}

public class UserRestServiceClient : KifaServiceRestClient<User>, UserServiceClient {
    public string AddCounter(User user, Counter counter)
        => Call<string>("add_counter", new AddCounterRequest {
            UserId = user.Id,
            Counter = counter
        });

    public string RemoveCounter(User user, string counterId)
        => Call<string>("remove_counter", new RemoveCounterRequest {
            UserId = user.Id,
            CounterId = counterId
        });

    public string AddEvent(User user, Counter counter, Event @event)
        => throw new NotImplementedException();
}
