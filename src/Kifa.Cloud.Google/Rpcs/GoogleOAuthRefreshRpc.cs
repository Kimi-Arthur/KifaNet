using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.Cloud.Google.Rpcs;

class GoogleOAuthRefreshRpc : KifaJsonParameterizedRpc<GoogleOAuthRefreshRpc.Response> {
    public class Response {
        public string? AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Scope { get; set; }
        public string? TokenType { get; set; }
        public string? IdToken { get; set; }
    }

    protected override string Url
        => "https://oauth2.googleapis.com/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}";

    protected override HttpMethod Method => HttpMethod.Post;

    public GoogleOAuthRefreshRpc(string clientId, string clientSecret, string refreshToken) {
        Parameters = new() {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "refresh_token", refreshToken }
        };
    }
}
