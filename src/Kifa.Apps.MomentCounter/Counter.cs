using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter;

public class Counter : DataModel<Counter> {
    public const string ModelId = "moment_counter/counters";
    public static CounterServiceClient Client { get; set; } = new CounterRestServiceClient();

    public string? Title { get; set; }

    // Must have this for starting point.
    public Date? FromDate { get; set; }

    // Can be null, meaning going forever. If both dates are set, total can be converted to average.
    public Date? ToDate { get; set; }

    // For now, this should not be changed. Can be added maybe.
    public List<Link<Unit>> Units { get; set; } = new();

    public int TotalTarget { get; set; }

    public int AverageTarget { get; set; }

    public List<Link<Event>> Events { get; set; } = new();
}

public interface CounterServiceClient : KifaServiceClient<Counter> {
}

public class CounterRestServiceClient : KifaServiceRestClient<Counter>, CounterServiceClient {
}
