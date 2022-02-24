using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Apps.MomentCounter; 

public class Event : DataModel<Event> {
    public const string ModelId = "moment_counter/events";

    public DateTime DateTime { get; set; }

    // Should match Counter's units.
    public List<int> Values { get; set; } = new();
}

public interface EventServiceClient : KifaServiceClient<Event> {
}

public class EventRestServiceClient : KifaServiceRestClient<Event>, EventServiceClient {
}