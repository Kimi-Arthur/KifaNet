using System.Collections.Generic;
using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

public sealed class UploaderInfoRpc : KifaJsonParameterizedRpc<UploaderInfoResponse> {
    protected override string Url => "https://api.bilibili.com/x/space/wbi/acc/info?mid={id}";

    protected override HttpMethod Method => HttpMethod.Get;

    public UploaderInfoRpc(string uploaderId) {
        // Not working due to new verification method. See discussion in
        // https://github.com/SocialSisterYi/bilibili-API-collect/issues/868
        Parameters = new Dictionary<string, string> {
            { "id", uploaderId }
        };
    }
}
