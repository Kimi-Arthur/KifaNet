using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.GoogleDrive {
    [DataModel("configs/google")]
    public class GoogleDriveConfig {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("accounts")]
        public Dictionary<string, AccountInfo> Accounts { get; private set; }

        [JsonProperty("root_folder")]
        public string RootFolder { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("apis")]
        public APIList APIList { get; private set; }

        public static string PimixServerApiAddress {
            get => PimixService.PimixServerApiAddress;
            set => PimixService.PimixServerApiAddress = value;
        }

        public static GoogleDriveConfig Get(string id) => PimixService.Get<GoogleDriveConfig>(id);
    }

    public class AccountInfo {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class APIList {
        [JsonProperty("create_file")]
        public APIInfo CreateFile { get; set; }

        [JsonProperty("create_folder")]
        public APIInfo CreateFolder { get; set; }

        [JsonProperty("delete_file")]
        public APIInfo DeleteFile { get; set; }

        [JsonProperty("download_file")]
        public APIInfo DownloadFile { get; set; }

        [JsonProperty("find_file")]
        public APIInfo FindFile { get; set; }

        [JsonProperty("get_file_info")]
        public APIInfo GetFileInfo { get; set; }

        [JsonProperty("oauth_refresh")]
        public APIInfo OauthRefresh { get; set; }
    }

    public class APIInfo {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
