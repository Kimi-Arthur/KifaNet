using System.Web;
using Kifa.Cloud.OAuth;
using Kifa.Service;

namespace Kifa.Twitter;

public class TwitterAccount : OAuthAccount, WithModelId<TwitterAccount> {
    public static string ModelId => "twitter/accounts";

    const string DefaultScope = "tweet.read%20offline.access";

    // See https://docs.x.com/resources/fundamentals/authentication/oauth-2-0/user-access-token#working-with-confidential-clients

    const string AuthUrlPattern =
        "https://x.com/i/oauth2/authorize?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&scope={scope}&state={state}&code_challenge=challenge&code_challenge_method=plain";

    public override string GetAuthUrl(string redirectUrl, string state)
        => AuthUrlPattern.Format(("client_id", TwitterConfig.ClientId),
            ("redirect_url", HttpUtility.UrlEncode(redirectUrl)),
            ("scope", HttpUtility.UrlEncode(DefaultScope)), ("state", state));

    const string GetTokenUrlPattern = "https://api.x.com/2/oauth2/token";

    public override OAuthTokenRequest GetTokenRequest(string code, string redirectUrl)
        => throw new NotImplementedException();

    public override KifaActionResult FillUserInfo() => throw new NotImplementedException();
}
