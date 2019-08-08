using Pimix.Service;

namespace Pimix.Infos {
    public class TmdbClient {
        public static string ApiKey { get; set; }
        public static APIList Apis { get; set; }
    }

    public class APIList {
        public API Languages { get; set; }
        public API Season { get; set; }
        public API Series { get; set; }
    }
}
