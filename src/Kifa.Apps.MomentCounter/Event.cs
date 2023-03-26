using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter;

public class Event : DataModel, WithModelId<Event> {
    public static string ModelId => "moment_counter/events";

    public static KifaServiceClient<Event> Client { get; set; } =
        new KifaServiceRestClient<Event>();

    public DateTime DateTime { get; set; }

    // Should match Counter's units.
    public List<int> Values { get; set; } = new();
}
