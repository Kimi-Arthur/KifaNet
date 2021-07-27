using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class User : DataModel<User> {
        public const string ModelId = "moment_counter/users";

        public string Name { get; set; }
        public Settings Settings { get; set; }

        public List<Link<Counter>> Counters { get; set; }
    }

    public interface UserServiceClient : KifaServiceClient<User> {
    }

    public class UserRestServiceClient : KifaServiceRestClient<User>, UserServiceClient {
    }
}
