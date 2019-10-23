using System.Collections.Generic;
using System.Drawing;
using Pimix.Service;

namespace Pimix.Games.BreathOfTheWild {
    public class KorokSeed : DataModel {
        public const string ModelId = "botw/seeds";

        static PimixServiceClient<KorokSeed> client;

        public static PimixServiceClient<KorokSeed> Client => client ??= new PimixServiceRestClient<KorokSeed>();

        public string Page { get; set; }
        public List<Location> Locations { get; set; }
    }
}
