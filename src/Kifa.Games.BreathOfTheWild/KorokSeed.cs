using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Games.BreathOfTheWild {
    public class KorokSeed : DataModel<KorokSeed> {
        public const string ModelId = "botw/seeds";

        static KifaServiceClient<KorokSeed> client;

        public static KifaServiceClient<KorokSeed> Client => client ??= new KifaServiceRestClient<KorokSeed>();

        public string Page { get; set; }
        public List<Location> Locations { get; set; }
    }
}
