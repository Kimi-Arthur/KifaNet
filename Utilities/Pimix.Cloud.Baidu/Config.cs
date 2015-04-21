using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Cloud.Baidu
{
    public class Config : DataModel
    {
        public override string ModelId
            => "cloud";

        [JsonProperty("$id")]
        public override string Id { get; set; }

        [JsonProperty("accounts")]
        public Dictionary<string, AccountInfo> Accounts { get; private set; }

        [JsonProperty("apis")]
        public Dictionary<string, APIInfo> APIList { get; private set; }

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

    public class APIInfo
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
