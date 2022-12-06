using Kifa.Service;

namespace Kifa.Apps.MomentCounter;

public class Unit : DataModel {
    public const string ModelId = "moment_counter/units";

    public string Name { get; set; }
    public int Ratio { get; set; }
    public Link<Unit>? Next { get; set; }
}

public interface UnitServiceClient : KifaServiceClient<Unit> {
}

public class UnitRestServiceClient : KifaServiceRestClient<Unit>, UnitServiceClient {
}
