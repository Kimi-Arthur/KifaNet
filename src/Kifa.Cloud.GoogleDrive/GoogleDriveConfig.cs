using System.Collections.Generic;
using Newtonsoft.Json;
using Kifa.Service;

namespace Kifa.Cloud.GoogleDrive {
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
        public Api CreateFile { get; set; }

        public Api CreateFolder { get; set; }

        public Api DeleteFile { get; set; }

        public Api DownloadFile { get; set; }

        public Api FindFile { get; set; }

        public Api ListFiles { get; set; }

        public Api GetFileInfo { get; set; }

        public Api OauthRefresh { get; set; }
    }
}
