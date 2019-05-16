using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.BaiduCloud {
    public class BaiduCloudConfig : DataModel {
        public const string ModelId = "configs/baidu_cloud";

        static BaiduCloudConfigServiceClient client;

        public static BaiduCloudConfigServiceClient Client => client =
            client ?? new BaiduCloudConfigRestServiceClient();

        public Dictionary<string, AccountInfo> Accounts { get; set; }

        [JsonProperty("apis")]
        public APIList APIList { get; set; }

        public string RemotePathPrefix { get; set; }
    }

    public class AccountInfo {
        public string AccessToken { get; set; }
    }

    public class APIList {
        public APIInfo CopyFile { get; set; }

        public APIInfo MoveFile { get; set; }

        public APIInfo DownloadFile { get; set; }

        public APIInfo UploadFileRapid { get; set; }

        public APIInfo UploadFileDirect { get; set; }

        public APIInfo RemovePath { get; set; }

        public APIInfo UploadBlock { get; set; }

        public APIInfo MergeBlocks { get; set; }

        public APIInfo GetFileInfo { get; set; }

        public APIInfo DiffFileList { get; set; }

        public APIInfo ListFiles { get; set; }
    }

    public class APIInfo {
        public string Method { get; set; }

        public string Url { get; set; }
    }

    public interface BaiduCloudConfigServiceClient : PimixServiceClient<BaiduCloudConfig> {
    }

    public class BaiduCloudConfigRestServiceClient : PimixServiceRestClient<BaiduCloudConfig>,
        BaiduCloudConfigServiceClient {
    }
}
