using System;
using System.Collections;
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

namespace Kifa.IO;

public class FileInformation : DataModel, WithModelId {
    public static string ModelId => "files";

    const int SliceLength = 256 << 10;

    public const int BlockSize = 32 << 20;
    static readonly Regex linkIdPattern = new(@"^(http|https|ftp)://([^:#?]*)([#?].*)?$");
    static readonly Regex fileIdPattern = new(@"^[^/]*(/.*?)(\.v\d)?$");

    static readonly Dictionary<FileProperties, PropertyInfo> normalProperties = new();
    static readonly Dictionary<FileProperties, PropertyInfo> collectionProperties = new();

    static FileInformation() {
        foreach (var prop in typeof(FileInformation).GetProperties(BindingFlags.Instance |
                     BindingFlags.Public)) {
            if (Enum.TryParse(typeof(FileProperties), prop.Name, out var propKey)) {
                if (prop.PropertyType.IsAssignableFrom(typeof(List<string>)) ||
                    prop.PropertyType.IsAssignableFrom(typeof(Dictionary<string, DateTime?>))) {
                    collectionProperties[(FileProperties) propKey!] = prop;
                    continue;
                }

                normalProperties[(FileProperties) propKey!] = prop;
            }
        }
    }

    public static FileInformationServiceClient Client { get; set; } =
        new FileInformationRestServiceClient();

    public long? Size { get; set; }

    public string? Md5 { get; set; }

    public string? Sha1 { get; set; }

    public string? Sha256 { get; set; }

    public string? Crc32 { get; set; }

    public string? Adler32 { get; set; }

    public List<string> BlockMd5 { get; set; } = new();

    public List<string> BlockSha1 { get; set; } = new();

    public List<string> BlockSha256 { get; set; } = new();

    public string? SliceMd5 { get; set; }

    public string? EncryptionKey { get; set; }

    public Dictionary<string, DateTime?> Locations { get; set; } = new();

    public override SortedSet<string> GetVirtualItems()
        => Sha256 != null
            ? new SortedSet<string> {
                VirtualItemPrefix + Sha256
            }
            : new SortedSet<string>();

    [JsonIgnore]
    public bool Exists => Size > 0;

    public static string? GetId(string location) {
        var linkMatch = linkIdPattern.Match(location);
        if (linkMatch.Success) {
            return $"/Web/{linkMatch.Groups[1].Value}.{linkMatch.Groups[2].Value}";
        }

        var fileMatch = fileIdPattern.Match(location);
        return fileMatch.Success ? fileMatch.Groups[1].Value : null;
    }

    public FileInformation AddProperties(Stream stream, FileProperties requiredProperties) {
        // Only calculate and populate nonexistent fields.
        requiredProperties -= requiredProperties & GetProperties();

        if (Size == null && (requiredProperties.HasFlag(FileProperties.Size) ||
                             (requiredProperties & FileProperties.AllBlockHashes) !=
                             FileProperties.None ||
                             (requiredProperties & FileProperties.AllHashes) !=
                             FileProperties.None)) {
            Size = stream.Length;
        }

        var readLength = 0;
        var buffer = new byte[BlockSize];

        if (stream.CanSeek) {
            stream.Seek(0, SeekOrigin.Begin);
        }

        if (requiredProperties.HasFlag(FileProperties.SliceMd5)) {
            readLength = stream.Read(buffer, 0, SliceLength);
            SliceMd5 = new MD5CryptoServiceProvider().ComputeHash(buffer, 0, readLength)
                .ToHexString();
        }

        if ((requiredProperties & FileProperties.AllHashes) != FileProperties.None) {
            var transformers = new List<Action<byte[], int>>();
            HashAlgorithm? md5Hasher = null;
            if (requiredProperties.HasFlag(FileProperties.Md5)) {
                md5Hasher = new MD5CryptoServiceProvider();
                transformers.Add((buffer, readLength)
                    => md5Hasher.TransformBlock(buffer, 0, readLength, buffer, 0));
            }

            HashAlgorithm? sha1Hasher = null;
            if (requiredProperties.HasFlag(FileProperties.Sha1)) {
                sha1Hasher = new SHA1CryptoServiceProvider();
                transformers.Add((buffer, readLength)
                    => sha1Hasher.TransformBlock(buffer, 0, readLength, buffer, 0));
            }

            HashAlgorithm? sha256Hasher = null;
            if (requiredProperties.HasFlag(FileProperties.Sha256)) {
                sha256Hasher = new SHA256CryptoServiceProvider();
                transformers.Add((buffer, readLength)
                    => sha256Hasher.TransformBlock(buffer, 0, readLength, buffer, 0));
            }

            HashAlgorithm? blockMd5Hasher;
            if (requiredProperties.HasFlag(FileProperties.BlockMd5)) {
                blockMd5Hasher = new MD5CryptoServiceProvider();
                transformers.Add((buffer, readLength)
                    => BlockMd5.Add(blockMd5Hasher.ComputeHash(buffer, 0, readLength)
                        .ToHexString()));
            }

            HashAlgorithm? blockSha1Hasher;
            if (requiredProperties.HasFlag(FileProperties.BlockSha1)) {
                blockSha1Hasher = new SHA1CryptoServiceProvider();
                transformers.Add((buffer, readLength)
                    => BlockSha1.Add(blockSha1Hasher.ComputeHash(buffer, 0, readLength)
                        .ToHexString()));
            }

            HashAlgorithm? blockSha256Hasher;
            if (requiredProperties.HasFlag(FileProperties.BlockSha256)) {
                blockSha256Hasher = new SHA256CryptoServiceProvider();
                transformers.Add((buffer, readLength)
                    => BlockSha256.Add(blockSha256Hasher.ComputeHash(buffer, 0, readLength)
                        .ToHexString()));
            }

            IHash? crc32Hasher = null;
            if (requiredProperties.HasFlag(FileProperties.Crc32)) {
                crc32Hasher = HashFactory.Checksum.CreateCRC32_IEEE();
                crc32Hasher.Initialize();
                transformers.Add((buffer, readLength)
                    => crc32Hasher.TransformBytes(buffer, 0, readLength));
            }

            IHash? adler32Hasher = null;
            if (requiredProperties.HasFlag(FileProperties.Adler32)) {
                adler32Hasher = HashFactory.Checksum.CreateAdler32();
                adler32Hasher.Initialize();
                transformers.Add((buffer, readLength)
                    => adler32Hasher.TransformBytes(buffer, 0, readLength));
            }

            var threadCount = transformers.Count;
            while ((readLength += stream.Read(buffer, readLength, BlockSize - readLength)) != 0) {
                Parallel.ForEach(transformers, new ParallelOptions {
                    MaxDegreeOfParallelism = threadCount
                }, transformer => transformer(buffer, readLength));

                readLength = 0;
            }

            if (md5Hasher != null) {
                md5Hasher.TransformFinalBlock(buffer, 0, 0);
                Md5 ??= md5Hasher.Hash?.ToHexString();
            }

            if (sha1Hasher != null) {
                sha1Hasher.TransformFinalBlock(buffer, 0, 0);
                Sha1 ??= sha1Hasher.Hash?.ToHexString();
            }

            if (sha256Hasher != null) {
                sha256Hasher.TransformFinalBlock(buffer, 0, 0);
                Sha256 ??= sha256Hasher.Hash?.ToHexString();
            }

            Crc32 ??= crc32Hasher?.TransformFinal().GetBytes().Reverse().ToArray().ToHexString();
            Adler32 ??= adler32Hasher?.TransformFinal().GetBytes().Reverse().ToArray()
                .ToHexString();
        }

        if (requiredProperties.HasFlag(FileProperties.EncryptionKey)) {
            EncryptionKey = GenerateEncryptionKey();
        }

        return this;
    }

    public FileInformation RemoveProperties(FileProperties removedProperties) {
        foreach (var p in normalProperties) {
            if (removedProperties.HasFlag(p.Key)) {
                p.Value.SetValue(this, null);
            }
        }

        foreach (var p in collectionProperties) {
            if (removedProperties.HasFlag(p.Key)) {
                p.Value.SetValue(this, Activator.CreateInstance(p.Value.PropertyType));
            }
        }

        return this;
    }

    IEnumerable<KeyValuePair<FileProperties, PropertyInfo>> ValidProperties
        => normalProperties.Where(x => {
            try {
                return x.Value.GetValue(this) != null;
            } catch (TargetInvocationException ex) {
                if (ex.InnerException is NullReferenceException) {
                    return false;
                }

                throw;
            }
        }).Concat(collectionProperties.Where(x => {
            try {
                return (x.Value.GetValue(this) as ICollection) is {
                    Count: > 0
                };
            } catch (TargetInvocationException ex) {
                if (ex.InnerException is NullReferenceException) {
                    return false;
                }

                throw;
            }
        }));

    public FileProperties GetProperties()
        => ValidProperties.Select(x => x.Key)
            .Aggregate(FileProperties.None, (result, x) => result | x);

    // Compares properties with other. The fields not appearing in other won't count.
    public FileProperties CompareProperties(FileInformation? other,
        FileProperties propertiesToCompare) {
        var result = FileProperties.None;
        if (other == null) {
            return result;
        }

        foreach (var p in ValidProperties) {
            if (propertiesToCompare.HasFlag(p.Key)) {
                if (p.Value.GetValue(other) != null) {
                    if (p.Value.PropertyType.IsAssignableFrom(typeof(List<string>))) {
                        var olist = p.Value.GetValue(other) as List<string>;
                        if (olist.Count > 0) {
                            var tlist = p.Value.GetValue(this) as List<string>;
                            if (!tlist.SequenceEqual(olist)) {
                                result |= p.Key;
                            }
                        }
                    } else if (!Equals(p.Value.GetValue(other), p.Value.GetValue(this))) {
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

    public static FileInformation GetInformation(string basePath, string path,
        FileProperties requiredProperties)
        => GetInformation(File.OpenRead($"{basePath}/{path}"), requiredProperties);

    static string GenerateEncryptionKey() {
        using var aes = new AesCryptoServiceProvider {
            KeySize = 256
        };
        return aes.Key.ToHexString();
    }
}

public interface FileInformationServiceClient : KifaServiceClient<FileInformation> {
    List<string> ListFolder(string folder, bool recursive = false);
    KifaActionResult AddLocation(string id, string location, bool verified = false);
    KifaActionResult RemoveLocation(string id, string location);
}

public class FileInformationRestServiceClient : KifaServiceRestClient<FileInformation>,
    FileInformationServiceClient {
    public List<string> ListFolder(string folder, bool recursive = false)
        => Call<List<string>>("list_folder", new Dictionary<string, object> {
            ["folder"] = folder,
            ["recursive"] = recursive
        });

    public KifaActionResult AddLocation(string id, string location, bool verified = false)
        => Call("add_location", new Dictionary<string, object> {
            ["id"] = id,
            ["location"] = location,
            ["verified"] = verified
        });

    public KifaActionResult RemoveLocation(string id, string location)
        => Call("remove_location", new Dictionary<string, object> {
            ["id"] = id,
            ["location"] = location
        });
}
