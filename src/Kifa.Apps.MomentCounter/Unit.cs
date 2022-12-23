using Kifa.Service;

namespace Kifa.Apps.MomentCounter;

public class Unit : DataModel, WithModelId {
    public static string ModelId => "moment_counter/units";

    public static KifaServiceClient<Unit> Client { get; set; } = new KifaServiceRestClient<Unit>();

    public string Name { get; set; }
    public int Ratio { get; set; }
    public Link<Unit>? Next { get; set; }
}
