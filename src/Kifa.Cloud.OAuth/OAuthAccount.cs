using Newtonsoft.Json;
using Pimix.Service;

namespace Kifa.Cloud.OAuth {
    public abstract class OAuthAccount : DataModel {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Scope { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }

        public abstract string GetAuthUrl(string redirectUrl, string state);

        public abstract string GetTokenUrl(string code, string redirectUrl);

        public abstract RestActionResult FillUserInfo();

        public abstract RestActionResult RefreshAccount();
    }
}
