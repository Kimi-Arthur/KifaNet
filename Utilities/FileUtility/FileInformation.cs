using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pimix.Storage
{
    public class FileInformation
    {
        [JsonProperty("$id")]
        public string Id
            => $"{Path}:{SHA256}";

        [JsonProperty("size")]
        public long? Size { get; set; }

        [JsonProperty("file_path")]
        public string Path { get; set; }

        [JsonProperty]
        public string MD5 { get; set; }

        [JsonProperty]
        public string SHA1 { get; set; }

        [JsonProperty]
        public string SHA256 { get; set; }
    }
}
