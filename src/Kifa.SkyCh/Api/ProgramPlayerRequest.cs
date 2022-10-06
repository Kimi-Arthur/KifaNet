using System.Collections.Generic;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api;

public sealed class ProgramPlayerRequest : ParameterizedRequest {
    public override Dictionary<string, string> Headers { get; } = new() {
        { "x-requested-with", "XMLHttpRequest" }
    };

    public override string UrlPattern { get; } =
        "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={program_id}&contentType=1&eventId={event_id}";

    public ProgramPlayerRequest(string programId, string eventId) {
        parameters = new Dictionary<string, string> {
            { "program_id", programId },
            { "event_id", eventId }
        };
    }
}
