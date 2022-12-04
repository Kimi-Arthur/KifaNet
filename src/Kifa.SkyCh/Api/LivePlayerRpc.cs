using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api;

public sealed class LivePlayerRpc : KifaJsonParameterizedRpc<PlayerResponse> {
    public override string UrlPattern { get; } =
        "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={live_id}&contentType=8";

    public override HttpMethod Method { get; } = HttpMethod.Get;

    public override Dictionary<string, string> Headers { get; } = new() {
        { "x-requested-with", "XMLHttpRequest" }
    };

    public LivePlayerRpc(string liveId) {
        parameters = new Dictionary<string, string> {
            { "live_id", liveId }
        };
    }
}
