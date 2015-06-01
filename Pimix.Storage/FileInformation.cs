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
        static Dictionary<FileProperties, Func<FileInformation, object>> mapping
            = new Dictionary<FileProperties, Func<FileInformation, object>>
            {
                [FileProperties.Path] = x => x.Path,
                [FileProperties.Size] = x => x.Size,
                [FileProperties.BlockSize] = x => x.BlockSize,
                [FileProperties.MD5] = x => x.MD5,
                [FileProperties.SHA1] = x => x.SHA1,
                [FileProperties.SHA256] = x => x.SHA256,
                [FileProperties.CRC32] = x => x.CRC32,
                [FileProperties.BlockMD5] = x => x.BlockMD5,
                [FileProperties.BlockSHA1] = x => x.BlockSHA1,
                [FileProperties.BlockSHA256] = x => x.BlockSHA256,
                [FileProperties.SliceMD5] = x => x.SliceMD5
            };

        public override string ModelId
            => "universal";

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

        [JsonProperty("crc32")]
        public string CRC32 { get; set; }

        [JsonProperty("block_size")]
        public int? BlockSize { get; set; }

        [JsonProperty("block_md5")]
        public List<string> BlockMD5 { get; set; }

        [JsonProperty("block_sha1")]
        public List<string> BlockSHA1 { get; set; }

        [JsonProperty("block_sha256")]
        public List<string> BlockSHA256 { get; set; }

        [JsonProperty("slice_md5")]
        public string SliceMD5 { get; set; }

        [JsonProperty("encryption_key")]
        public string EncryptionKey { get; set; }

        [JsonProperty("locations")]
        public List<string> Locations { get; set; }

        public FileProperties GetProperties()
            => mapping
            .Where(x => x.Value(this) != null)
            .Select(x => x.Key)
            .Aggregate(FileProperties.None, (result, x) => result | x);
    }
}
