using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.GoogleDrive {
    public class GoogleDriveConfig : DataModel {
        public const string ModelId = "configs/google";

        static KifaServiceClient<GoogleDriveConfig> client;

        public static KifaServiceClient<GoogleDriveConfig> Client => client ??= new KifaServiceRestClient<GoogleDriveConfig>();

        public string RootFolder { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        [JsonProperty("apis")]
        public APIList APIList { get; set; }
    }

    public class APIList {
        public API CreateFile { get; set; }

        public API CreateFolder { get; set; }

        public API DeleteFile { get; set; }

        public API DownloadFile { get; set; }

        public API FindFile { get; set; }

        public API ListFiles { get; set; }

        public API GetFileInfo { get; set; }

        public API OauthRefresh { get; set; }
    }
}
