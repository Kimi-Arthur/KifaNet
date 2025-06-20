using System.Net.Http;
using Kifa.Cloud.OAuth;

namespace Kifa.Cloud.BaiduCloud.Rpcs;

public sealed class BaiduOAuthTokenRequest : OAuthTokenRequest {
    public BaiduOAuthTokenRequest(string code, string clientId, string clientSecret,
        string redirectUrl) : base(code, clientId, clientSecret, redirectUrl) {
    }

    protected override string Url
        => "https://openapi.baidu.com/oauth/2.0/authorize?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&scope={scope}&force_login=1";

    protected override HttpMethod Method => HttpMethod.Post;
}
