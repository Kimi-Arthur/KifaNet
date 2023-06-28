using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api;

public sealed class LivePlayerRpc : KifaJsonParameterizedRpc<PlayerResponse> {
    protected override string Url
        => "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={live_id}&contentType=8";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => new() {
            { "x-requested-with", "XMLHttpRequest" }
        };

    public LivePlayerRpc(string liveId) {
        Parameters = new Dictionary<string, string> {
            { "live_id", liveId }
        };
    }
}
