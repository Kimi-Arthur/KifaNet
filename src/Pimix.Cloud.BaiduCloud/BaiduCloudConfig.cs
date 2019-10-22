using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.BaiduCloud {
    public class BaiduCloudConfig : DataModel {
        public const string ModelId = "configs/baidu_cloud";

        static PimixServiceClient<BaiduCloudConfig> client;

        public static PimixServiceClient<BaiduCloudConfig> Client => client ??= new PimixServiceRestClient<BaiduCloudConfig>();

        public Dictionary<string, AccountInfo> Accounts { get; set; }

        [JsonProperty("apis")]
        public APIList APIList { get; set; }

        public string RemotePathPrefix { get; set; }
    }

    public class AccountInfo {
        public string AccessToken { get; set; }
    }

    public class APIList {
        public API CopyFile { get; set; }

        public API MoveFile { get; set; }

        public API DownloadFile { get; set; }

        public API UploadFileRapid { get; set; }

        public API UploadFileDirect { get; set; }

        public API RemovePath { get; set; }

        public API UploadBlock { get; set; }

        public API MergeBlocks { get; set; }

        public API GetFileInfo { get; set; }

        public API DiffFileList { get; set; }

        public API ListFiles { get; set; }
    }
}
