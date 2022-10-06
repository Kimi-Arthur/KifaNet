using System;
using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.SkyCh.Api;

public class LivePlayerRequest : ParameterizedRequest {
    public override Dictionary<string, string> Headers { get; } = new() {
        { "x-requested-with", "XMLHttpRequest" }
    };

    public override string UrlPattern { get; } =
        "https://sport.sky.ch/en/SkyPlayerAjax/SkyPlayer?id={live_id}&contentType=8";

    public LivePlayerRequest(string liveId) {
        parameters = new Dictionary<string, string> {
            { "live_id", liveId }
        };
    }
}
