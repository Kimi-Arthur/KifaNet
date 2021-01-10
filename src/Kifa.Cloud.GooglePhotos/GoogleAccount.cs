using Pimix.Service;

namespace Kifa.Cloud.GooglePhotos {
    public class GoogleAccount : DataModel {
        public const string ModelId = "accounts/google";

        static PimixServiceClient<GoogleAccount> client;

        public static PimixServiceClient<GoogleAccount> Client =>
            client ??= new PimixServiceRestClient<GoogleAccount>();

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Scope { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }
}
