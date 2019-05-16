using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.GoogleDrive {
    public class GoogleDriveConfig : DataModel {
        public const string ModelId = "configs/google";

        static GoogleDriveConfigServiceClient client;

        public static GoogleDriveConfigServiceClient Client => client =
            client ?? new GoogleDriveConfigRestServiceClient();

        public Dictionary<string, AccountInfo> Accounts { get; set; }

        public string RootFolder { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        [JsonProperty("apis")]
        public APIList APIList { get; set; }
    }

    public class AccountInfo {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }

    public class APIList {
        public APIInfo CreateFile { get; set; }

        public APIInfo CreateFolder { get; set; }

        public APIInfo DeleteFile { get; set; }

        public APIInfo DownloadFile { get; set; }

        public APIInfo FindFile { get; set; }

        public APIInfo ListFiles { get; set; }

        public APIInfo GetFileInfo { get; set; }

        public APIInfo OauthRefresh { get; set; }
    }

    public class APIInfo {
        public string Method { get; set; }

        public string Url { get; set; }

        public string Data { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }


    public interface GoogleDriveConfigServiceClient : PimixServiceClient<GoogleDriveConfig> {
    }

    public class GoogleDriveConfigRestServiceClient : PimixServiceRestClient<GoogleDriveConfig>,
        GoogleDriveConfigServiceClient {
    }
}
