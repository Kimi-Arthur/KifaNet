using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.BaiduCloud
{
    public class BaiduCloudConfig : DataModel
    {
        public override string ModelId
            => "cloud";

        [JsonProperty("accounts")]
        public Dictionary<string, AccountInfo> Accounts { get; private set; }

        [JsonProperty("apis")]
        public APIList APIList { get; private set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("remote_path_prefix")]
        public string RemotePathPrefix { get; set; }
    }

    public class AccountInfo
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    public class APIList
    {
        [JsonProperty("download_file")]
        public APIInfo DownloadFile { get; set; }

        [JsonProperty("upload_file_rapid")]
        public APIInfo UploadFileRapid { get; set; }

        [JsonProperty("remove_path")]
        public APIInfo RemovePath { get; set; }

        [JsonProperty("upload_block")]
        public APIInfo UploadBlock { get; set; }

        [JsonProperty("merge_blocks")]
        public APIInfo MergeBlocks { get; set; }
    }

    public class APIInfo
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
