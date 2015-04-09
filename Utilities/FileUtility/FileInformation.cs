using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.Storage
{
    public class FileInformation : DataModel
    {
        [JsonProperty("$id")]
        public override string Id { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("size")]
        public long? Size { get; set; }

        [JsonProperty("md5")]
        public string MD5 { get; set; }

        [JsonProperty("sha1")]
        public string SHA1 { get; set; }

        [JsonProperty("sha256")]
        public string SHA256 { get; set; }

        [JsonProperty("block_size")]
        public int? BlockSize { get; set; }

        [JsonProperty("block_md5")]
        public List<string> BlockMD5 { get; set; }

        [JsonProperty("block_block_sha1")]
        public List<string> BlockSHA1 { get; set; }

        [JsonProperty("block_sha256")]
        public List<string> BlockSHA256 { get; set; }
    }
}
