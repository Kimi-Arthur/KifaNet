using System.Collections.Generic;
using Newtonsoft.Json;
using Kifa.Service;

namespace Kifa.Cloud.BaiduCloud;

public class BaiduCloudConfig : DataModel<BaiduCloudConfig> {
    public const string ModelId = "configs/baidu_cloud";

    static KifaServiceClient<BaiduCloudConfig> client;

    public static KifaServiceClient<BaiduCloudConfig> Client
        => client ??= new KifaServiceRestClient<BaiduCloudConfig>();

    public Dictionary<string, AccountInfo> Accounts { get; set; }

    [JsonProperty("apis")]
    public APIList APIList { get; set; }

    public string RemotePathPrefix { get; set; }
}

public class AccountInfo {
    public string AccessToken { get; set; }
}

public class APIList {
    public Api CopyFile { get; set; }

    public Api MoveFile { get; set; }

    public Api DownloadFile { get; set; }

    public Api UploadFileRapid { get; set; }

    public Api UploadFileDirect { get; set; }

    public Api RemovePath { get; set; }

    public Api UploadBlock { get; set; }

    public Api MergeBlocks { get; set; }

    public Api GetFileInfo { get; set; }

    public Api DiffFileList { get; set; }

    public Api ListFiles { get; set; }
}
