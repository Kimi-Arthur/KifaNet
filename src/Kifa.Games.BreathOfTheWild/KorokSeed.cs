using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Games.BreathOfTheWild;

public class KorokSeed : DataModel, WithModelId {
    public static string ModelId => "botw/seeds";

    public static KifaServiceClient<KorokSeed> Client { get; set; } =
        new KifaServiceRestClient<KorokSeed>();

    public string Page { get; set; }
    public List<Location> Locations { get; set; }
}
