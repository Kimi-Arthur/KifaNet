using Kifa.Service;

namespace Kifa.Apps.MomentCounter {
    public class Unit : DataModel<Unit> {
        public const string ModelId = "moment_counter/units";

        public string Name { get; set; }
        public double Ratio { get; set; }
        public Unit Next { get; set; }
    }

    public interface UnitServiceClient : KifaServiceClient<Unit> {
    }

    public class UnitRestServiceClient : KifaServiceRestClient<Unit>, UnitServiceClient {
    }
}
