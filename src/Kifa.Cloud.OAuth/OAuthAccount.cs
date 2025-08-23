using Kifa.Service;

namespace Kifa.Cloud.OAuth;

public abstract class OAuthAccount {
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string Scope { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }

    public abstract string GetAuthUrl(string redirectUrl, string state);

    public abstract OAuthTokenRequest GetTokenRequest(string code, string redirectUrl);

    public abstract KifaActionResult FillUserInfo();
}
