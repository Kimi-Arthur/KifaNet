using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api;

public sealed class ProgramPlayerRpc : KifaJsonParameterizedRpc<PlayerResponse> {
    protected override string Url
        => "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={program_id}&contentType=1&eventId={event_id}";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "x-requested-with", "XMLHttpRequest" }
        };

    public ProgramPlayerRpc(string programId, string eventId) {
        Parameters = new () {
            { "program_id", programId },
            { "event_id", eventId }
        };
    }
}
