using System.Net.Http;
using Kifa.Cloud.OAuth;

namespace Kifa.Cloud.Google.Rpcs;

public class GoogleOAuthTokenRequest : OAuthTokenRequest {
    public GoogleOAuthTokenRequest(string code, string clientId, string clientSecret,
        string redirectUrl) : base(code, clientId, clientSecret, redirectUrl) {
    }

    protected override string Url
        => "https://oauth2.googleapis.com/token?grant_type=authorization_code&code={code}&client_id={client_id}&client_secret={client_secret}&redirect_uri={redirect_url}";

    protected override HttpMethod Method => HttpMethod.Post;
}
