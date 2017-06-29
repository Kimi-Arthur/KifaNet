using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HashLib;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.IO
{
    [DataModel("files")]
    public partial class FileInformation
    {
        const long MaxBlockCount = 1L << 10;
        const long MaxBlockSize = 2L << 30;
        const long MinBlockSize = 32L << 20;
        const int SliceLength = 256 << 10;

        static Dictionary<FileProperties, PropertyInfo> Properties;

        [JsonProperty("id")]
        public string Id { get; set; }

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

        [JsonProperty("adler32")]
        public string Adler32 { get; set; }

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
        public Dictionary<string, string> Locations { get; set; }

        public static List<FileInformation> GetFolderView(string path)
            => PimixService.Call<FileInformation, List<FileInformation>>("get_folder_view", parameters: new Dictionary<string, string> { ["location"] = path });

        static FileInformation()
        {
            Properties = new Dictionary<FileProperties, PropertyInfo>();
            foreach (var prop in typeof(FileInformation).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Properties[(FileProperties)Enum.Parse(typeof(FileProperties), prop.Name)] = prop;
            }
        }

        public FileInformation AddProperties(Stream stream, FileProperties requiredProperties)
        {
            requiredProperties -= requiredProperties & GetProperties();

            if (Size == null
                && (requiredProperties.HasFlag(FileProperties.Size)
                || requiredProperties.HasFlag(FileProperties.BlockSize)
                || (requiredProperties & FileProperties.AllBlockHashes) != FileProperties.None
                || (requiredProperties & FileProperties.AllHashes) != FileProperties.None))
            {
                Size = stream.Length;
            }

            if (BlockSize == null
                && (requiredProperties & (FileProperties.BlockSize | FileProperties.AllHashes | FileProperties.AllBlockHashes)) != FileProperties.None)
            {
                BlockSize = GetBlockSize(Size.Value);
            }

            int readLength = 0;
            byte[] buffer = new byte[BlockSize == null ? SliceLength : BlockSize.Value];

            if (stream != null && stream.CanSeek)
            {
                // Assume at head if stream is not seekable.
                stream.Seek(0, SeekOrigin.Begin);
            }

            if (requiredProperties.HasFlag(FileProperties.SliceMD5))
            {
                readLength = stream.Read(buffer, 0, SliceLength);
                SliceMD5 = (readLength == SliceLength)
                    ? new MD5CryptoServiceProvider().ComputeHash(buffer, 0, SliceLength).ToHexString()
                    : null;
            }

            if ((requiredProperties & FileProperties.AllHashes) != FileProperties.None)
            {
                List<HashAlgorithm> hashers = new List<HashAlgorithm>
                {
                    requiredProperties.HasFlag(FileProperties.MD5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.SHA1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.SHA256) ? new SHA256CryptoServiceProvider() : null
                };

                BlockMD5 = requiredProperties.HasFlag(FileProperties.BlockMD5) ? new List<string>() : BlockMD5;
                BlockSHA1 = requiredProperties.HasFlag(FileProperties.BlockSHA1) ? new List<string>() : BlockSHA1;
                BlockSHA256 = requiredProperties.HasFlag(FileProperties.BlockSHA256) ? new List<string>() : BlockSHA256;

                List<HashAlgorithm> blockHashers = new List<HashAlgorithm>
                {
                    requiredProperties.HasFlag(FileProperties.BlockMD5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.BlockSHA1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.BlockSHA256) ? new SHA256CryptoServiceProvider() : null
                };

                List<IHash> additionalHashers = new List<IHash>
                {
                    requiredProperties.HasFlag(FileProperties.CRC32) ? HashFactory.Checksum.CreateCRC32a() : null,
                    requiredProperties.HasFlag(FileProperties.Adler32) ? HashFactory.Checksum.CreateAdler32() : null
                };

                foreach (var hasher in additionalHashers)
                {
                    hasher?.Initialize();
                }

                while ((readLength += stream.Read(buffer, readLength, BlockSize.Value - readLength)) != 0)
                {
                    Parallel.ForEach(
                        hashers,
                        hasher =>
                        {
                            hasher?.TransformBlock(buffer, 0, readLength, buffer, 0);
                        }
                    );

                    Parallel.ForEach(
                        additionalHashers,
                        hasher =>
                        {
                            hasher?.TransformBytes(buffer, 0, readLength);
                        }
                    );

                    if (requiredProperties.HasFlag(FileProperties.BlockMD5))
                    {
                        BlockMD5.Add(blockHashers[0].ComputeHash(buffer, 0, readLength).ToHexString());
                    }

                    if (requiredProperties.HasFlag(FileProperties.BlockSHA1))
                    {
                        BlockSHA1.Add(blockHashers[1].ComputeHash(buffer, 0, readLength).ToHexString());
                    }

                    if (requiredProperties.HasFlag(FileProperties.BlockSHA256))
                    {
                        BlockSHA256.Add(blockHashers[2].ComputeHash(buffer, 0, readLength).ToHexString());
                    }

                    readLength = 0;
                }

                foreach (var hasher in hashers)
                {
                    hasher?.TransformFinalBlock(buffer, 0, 0);
                }

                MD5 = MD5 ?? hashers[0]?.Hash.ToHexString();
                SHA1 = SHA1 ?? hashers[1]?.Hash.ToHexString();
                SHA256 = SHA256 ?? hashers[2]?.Hash.ToHexString();
                CRC32 = CRC32 ?? additionalHashers[0]?.TransformFinal().GetBytes().Reverse().ToArray().ToHexString();
                Adler32 = Adler32 ?? additionalHashers[1]?.TransformFinal().GetBytes().Reverse().ToArray().ToHexString();
            }

            if (requiredProperties.HasFlag(FileProperties.EncryptionKey))
            {
                EncryptionKey = GenerateEncryptionKey();
            }

            return this;
        }

        public FileInformation RemoveProperties(FileProperties removedProperties)
        {
            foreach (var p in Properties)
            {
                if (removedProperties.HasFlag(p.Key))
                {
                    p.Value.SetValue(this, null);
                }
            }

            return this;
        }

        public FileProperties GetProperties()
            => Properties
            .Where(x => x.Value.GetValue(this) != null)
            .Select(x => x.Key)
            .Aggregate(FileProperties.None, (result, x) => result | x);

        public FileProperties CompareProperties(FileInformation other, FileProperties propertiesToCompare)
        {
            FileProperties result = FileProperties.None;
            foreach (var p in Properties)
            {
                if (propertiesToCompare.HasFlag(p.Key))
                {
                    if (p.Value.GetValue(other) != null)
                    {
                        if (p.Value.PropertyType.IsAssignableFrom(typeof(List<string>)))
                        {
                            var olist = p.Value.GetValue(other) as List<string>;
                            var tlist = p.Value.GetValue(this) as List<string>;
                            if (!tlist.SequenceEqual(olist))
                            {
                                result |= p.Key;
                            }
                        }
                        else if (!object.Equals(p.Value.GetValue(other), p.Value.GetValue(this)))
                        {
                            result |= p.Key;
                        }
                    }
                }
            }

            return result;
        }

        public static FileInformation GetInformation(Stream stream, FileProperties requiredProperties)
            => new FileInformation().AddProperties(stream, requiredProperties);

        public static FileInformation GetInformation(string path, FileProperties requiredProperties)
            => GetInformation(File.OpenRead(path), requiredProperties);

        public static FileInformation GetInformation(string basePath, string path, FileProperties requiredProperties)
            => GetInformation(File.OpenRead($"{basePath}/{path}"), requiredProperties);

        static int GetBlockSize(long size)
        {
            long blockSize = MinBlockSize;

            // Special logic:
            //   1. Reserve one block for header
            //   2. Not stop when equals for the 'padding' logic

            while (blockSize <= MaxBlockSize && blockSize * (MaxBlockCount - 1) <= size)
            {
                blockSize <<= 1;
            }

            return (int)blockSize;
        }

        static string GenerateEncryptionKey()
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                return aes.Key.ToHexString();
            }
        }
    }
}
