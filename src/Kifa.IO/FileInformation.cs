﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HashLib;
using Kifa.Service;
using Newtonsoft.Json;
using Pimix;

namespace Kifa.IO {
    public class FileInformation : DataModel {
        public const string ModelId = "files";

        const int SliceLength = 256 << 10;

        public const int BlockSize = 32 << 20;
        static FileInformationServiceClient client;
        static readonly Regex linkIdPattern = new Regex(@"^(http|https|ftp)://([^:#?]*)([#?].*)?$");
        static readonly Regex fileIdPattern = new Regex(@"^[^/]*(/.*?)(\.v\d)?$");

        static readonly Dictionary<FileProperties, PropertyInfo> properties;

        static FileInformation() {
            properties = new Dictionary<FileProperties, PropertyInfo>();
            foreach (var prop in typeof(FileInformation).GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                if (Enum.TryParse(typeof(FileProperties), prop.Name, out var propKey)) {
                    properties[(FileProperties) propKey] = prop;
                }
            }
        }

        public static FileInformationServiceClient Client => client ??= new FileInformationRestServiceClient();

        public long? Size { get; set; }

        public string Md5 { get; set; }

        public string Sha1 { get; set; }

        public string Sha256 { get; set; }

        public string Crc32 { get; set; }

        public string Adler32 { get; set; }

        public List<string> BlockMd5 { get; set; }

        public List<string> BlockSha1 { get; set; }

        public List<string> BlockSha256 { get; set; }

        public string SliceMd5 { get; set; }

        public string EncryptionKey { get; set; }

        public Dictionary<string, DateTime?> Locations { get; set; }

        [JsonIgnore]
        public bool Exists => Size > 0;
        
        public static string GetId(string location) {
            var linkMatch = linkIdPattern.Match(location);
            if (linkMatch.Success) {
                return $"/Web/{linkMatch.Groups[1].Value}.{linkMatch.Groups[2].Value}";
            }

            var fileMatch = fileIdPattern.Match(location);
            return fileMatch.Success ? fileMatch.Groups[1].Value : null;
        }

        public FileInformation AddProperties(Stream stream, FileProperties requiredProperties) {
            requiredProperties -= requiredProperties & GetProperties();

            if (Size == null && (requiredProperties.HasFlag(FileProperties.Size) ||
                                 (requiredProperties & FileProperties.AllBlockHashes) != FileProperties.None ||
                                 (requiredProperties & FileProperties.AllHashes) != FileProperties.None)) {
                Size = stream.Length;
            }

            var readLength = 0;
            var buffer = new byte[BlockSize];

            if (stream != null && stream.CanSeek) {
                stream.Seek(0, SeekOrigin.Begin);
            }

            if (requiredProperties.HasFlag(FileProperties.SliceMd5)) {
                readLength = stream.Read(buffer, 0, SliceLength);
                SliceMd5 = new MD5CryptoServiceProvider().ComputeHash(buffer, 0, readLength).ToHexString();
            }

            if ((requiredProperties & FileProperties.AllHashes) != FileProperties.None) {
                var hashers = new List<HashAlgorithm> {
                    requiredProperties.HasFlag(FileProperties.Md5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.Sha1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.Sha256) ? new SHA256CryptoServiceProvider() : null
                };

                BlockMd5 = requiredProperties.HasFlag(FileProperties.BlockMd5) ? new List<string>() : BlockMd5;
                BlockSha1 = requiredProperties.HasFlag(FileProperties.BlockSha1) ? new List<string>() : BlockSha1;
                BlockSha256 = requiredProperties.HasFlag(FileProperties.BlockSha256) ? new List<string>() : BlockSha256;

                var blockHashers = new List<HashAlgorithm> {
                    requiredProperties.HasFlag(FileProperties.BlockMd5) ? new MD5CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.BlockSha1) ? new SHA1CryptoServiceProvider() : null,
                    requiredProperties.HasFlag(FileProperties.BlockSha256) ? new SHA256CryptoServiceProvider() : null
                };

                var additionalHashers = new List<IHash> {
                    requiredProperties.HasFlag(FileProperties.Crc32) ? HashFactory.Checksum.CreateCRC32_IEEE() : null,
                    requiredProperties.HasFlag(FileProperties.Adler32) ? HashFactory.Checksum.CreateAdler32() : null
                };

                foreach (var hasher in additionalHashers) {
                    hasher?.Initialize();
                }

                while ((readLength += stream.Read(buffer, readLength, BlockSize - readLength)) != 0) {
                    Parallel.ForEach(hashers, hasher => { hasher?.TransformBlock(buffer, 0, readLength, buffer, 0); });

                    Parallel.ForEach(additionalHashers, hasher => { hasher?.TransformBytes(buffer, 0, readLength); });

                    if (requiredProperties.HasFlag(FileProperties.BlockMd5)) {
                        BlockMd5.Add(blockHashers[0].ComputeHash(buffer, 0, readLength).ToHexString());
                    }

                    if (requiredProperties.HasFlag(FileProperties.BlockSha1)) {
                        BlockSha1.Add(blockHashers[1].ComputeHash(buffer, 0, readLength).ToHexString());
                    }

                    if (requiredProperties.HasFlag(FileProperties.BlockSha256)) {
                        BlockSha256.Add(blockHashers[2].ComputeHash(buffer, 0, readLength).ToHexString());
                    }

                    readLength = 0;
                }

                foreach (var hasher in hashers) {
                    hasher?.TransformFinalBlock(buffer, 0, 0);
                }

                Md5 ??= hashers[0]?.Hash.ToHexString();
                Sha1 ??= hashers[1]?.Hash.ToHexString();
                Sha256 ??= hashers[2]?.Hash.ToHexString();
                Crc32 ??= additionalHashers[0]?.TransformFinal().GetBytes().Reverse().ToArray().ToHexString();
                Adler32 ??= additionalHashers[1]?.TransformFinal().GetBytes().Reverse().ToArray().ToHexString();
            }

            if (requiredProperties.HasFlag(FileProperties.EncryptionKey)) {
                EncryptionKey = GenerateEncryptionKey();
            }

            return this;
        }

        public FileInformation RemoveProperties(FileProperties removedProperties) {
            foreach (var p in properties) {
                if (removedProperties.HasFlag(p.Key)) {
                    p.Value.SetValue(this, null);
                }
            }

            return this;
        }

        IEnumerable<KeyValuePair<FileProperties, PropertyInfo>> ValidProperties =>
            properties.Where(x => x.Value.GetValue(this) != null);

        public FileProperties GetProperties() =>
            properties.Where(x => x.Value.GetValue(this) != null).Select(x => x.Key)
                .Aggregate(FileProperties.None, (result, x) => result | x);

        public FileProperties CompareProperties(FileInformation other, FileProperties propertiesToCompare) {
            var result = FileProperties.None;
            foreach (var p in ValidProperties) {
                if (propertiesToCompare.HasFlag(p.Key)) {
                    if (p.Value.GetValue(other) != null) {
                        if (p.Value.PropertyType.IsAssignableFrom(typeof(List<string>))) {
                            var olist = p.Value.GetValue(other) as List<string>;
                            var tlist = p.Value.GetValue(this) as List<string>;
                            if (!tlist.SequenceEqual(olist)) {
                                result |= p.Key;
                            }
                        } else if (!Equals(p.Value.GetValue(other), p.Value.GetValue(this))) {
                            result |= p.Key;
                        }
                    }
                }
            }

            return result;
        }

        public static FileInformation GetInformation(Stream stream, FileProperties requiredProperties) =>
            new FileInformation().AddProperties(stream, requiredProperties);

        public static FileInformation GetInformation(string path, FileProperties requiredProperties) =>
            GetInformation(File.OpenRead(path), requiredProperties);

        public static FileInformation GetInformation(string basePath, string path, FileProperties requiredProperties) =>
            GetInformation(File.OpenRead($"{basePath}/{path}"), requiredProperties);

        static string GenerateEncryptionKey() {
            using var aes = new AesCryptoServiceProvider {KeySize = 256};
            return aes.Key.ToHexString();
        }
    }

    public interface FileInformationServiceClient : KifaServiceClient<FileInformation> {
        List<string> ListFolder(string folder, bool recursive = false);
        void AddLocation(string id, string location, bool verified = false);
        void RemoveLocation(string id, string location);

        string CreateLocation(string id, string type = null, string format = null);

        string GetLocation(string id, List<string> types = null);
    }

    public class FileInformationRestServiceClient : KifaServiceRestClient<FileInformation>,
        FileInformationServiceClient {
        public List<string> ListFolder(string folder, bool recursive = false) =>
            Call<List<string>>("list_folder",
                parameters: new Dictionary<string, object> {["folder"] = folder, ["recursive"] = recursive ? "1" : ""});

        public void AddLocation(string id, string location, bool verified = false) =>
            Call("add_location", id, new Dictionary<string, object> {["location"] = location, ["verified"] = verified});

        public void RemoveLocation(string id, string location) =>
            Call("remove_location", id, new Dictionary<string, object> {["location"] = location});

        public string CreateLocation(string id, string type = null, string format = null) =>
            Call<string>("create_location", id, new Dictionary<string, object> {["type"] = type, ["format"] = format});

        public string GetLocation(string id, List<string> types = null) =>
            Call<string>("get_location", id, new Dictionary<string, object> {["types"] = types});
    }
}