using Kifa.Service;

namespace Kifa.Soccer;

public class Team : DataModel, WithModelId {
    public static string ModelId => "soccer/teams";

    public static KifaServiceClient<Team> Client { get; set; } = new KifaServiceRestClient<Team>();

    // Full names used in English wiki, like "FC Bayern Munich" or "Borussia Mönchengladbach"
    public string Name { get; set; }

    // Short id, normally 3 capitalized characters, found in Twitter tag or scoreboard, like "BAY" for bayern and
    // "BMG" for M'gladbach.
    public string Short { get; set; }
}
