using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class User : DataModel<User> {
        public const string ModelId = "moment_counter/users";

        public string Name { get; set; }

        // This also includes things like next ids.
        public Settings Settings { get; set; } = new();

        public List<Link<Counter>> Counters { get; set; } = new();
    }

    public interface UserServiceClient : KifaServiceClient<User> {
        string AddCounter(User user, Counter counter);
        string RemoveCounter(User user, Counter counter);
        string AddEvent(User user, Counter counter, Event @event);
    }

    public class AddCounterRequest {
        public string UserId { get; set; }
        public Counter Counter { get; set; }
    }

    public class UserRestServiceClient : KifaServiceRestClient<User>, UserServiceClient {
        public string AddCounter(User user, Counter counter) =>
            Call<string>("add_counter", new AddCounterRequest {
                UserId = user.Id,
                Counter = counter
            });

        public string RemoveCounter(User user, Counter counter) =>
            Call<string>("add_counter", new Dictionary<string, object> {
                ["user_id"] = user.Id,
                ["counter"] = counter
            });

        public string AddEvent(User user, Counter counter, Event @event) {
            throw new NotImplementedException();
        }
    }
}
