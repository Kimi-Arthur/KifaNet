using Kifa.Rpc;

namespace Kifa.Cloud.OAuth;

public abstract class OAuthTokenRequest : KifaJsonParameterizedRpc<OAuthTokenRequest.Response> {
    #region OAuthTokenRequest.Response

    public class Response {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? Scope { get; set; }
    }

    #endregion

    public OAuthTokenRequest(string code, string clientId, string clientSecret,
        string redirectUrl) {
        Parameters = new() {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_url", redirectUrl }
        };
    }
}
