using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.BaiduCloud {
    [DataModel("configs/baidu_cloud")]
    public partial class BaiduCloudConfig {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("accounts")]
        public Dictionary<string, AccountInfo> Accounts { get; private set; }

        [JsonProperty("apis")]
        public APIList APIList { get; private set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("remote_path_prefix")]
        public string RemotePathPrefix { get; set; }
    }

    public class AccountInfo {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    public class APIList {
        [JsonProperty("copy_file")]
        public APIInfo CopyFile { get; set; }

        [JsonProperty("move_file")]
        public APIInfo MoveFile { get; set; }

        [JsonProperty("download_file")]
        public APIInfo DownloadFile { get; set; }

        [JsonProperty("upload_file_rapid")]
        public APIInfo UploadFileRapid { get; set; }

        [JsonProperty("upload_file_direct")]
        public APIInfo UploadFileDirect { get; set; }

        [JsonProperty("remove_path")]
        public APIInfo RemovePath { get; set; }

        [JsonProperty("upload_block")]
        public APIInfo UploadBlock { get; set; }

        [JsonProperty("merge_blocks")]
        public APIInfo MergeBlocks { get; set; }

        [JsonProperty("get_file_info")]
        public APIInfo GetFileInfo { get; set; }

        [JsonProperty("diff_file_list")]
        public APIInfo DiffFileList { get; set; }

        [JsonProperty("list_files")]
        public APIInfo ListFiles { get; set; }
    }

    public class APIInfo {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
