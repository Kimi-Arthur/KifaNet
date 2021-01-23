using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;
using Pimix;
using Pimix.Cloud.BaiduCloud;
using Pimix.Cloud.GoogleDrive;
using Pimix.Cloud.MegaNz;
using Pimix.Cloud.Swisscom;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace Kifa.Api.Files {
    public class KifaFile : IComparable<KifaFile>, IEquatable<KifaFile> {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static Regex subPathIgnoredFiles;

        static Regex fullPathIgnoredFiles;

        static readonly System.Collections.Generic.Dictionary<string, StorageClient> knownClients = new();

        FileInformation fileInfo;

        public bool SimpleMode { get; set; }

        public KifaFile(string uri = null, string id = null, FileInformation fileInfo = null, bool simpleMode = false,
            bool useCache = false) {
            SimpleMode = simpleMode;
            if (uri == null) {
                // Infer uri from id.
                uri = GetUri(id ?? fileInfo?.Id);
            }

            // Example uri:
            //   baidu:Pimix_1/a/b/c/d.txt.v1
            //   mega:0z/a/b/c/d.txt
            //   local:cubie/a/b/c/d.txt
            //   local:/a/b/c/d.txt
            //   /a/b/c/d.txt
            //   C:/files/a.txt
            //   ~/a.txt
            //   ../a.txt
            if (!uri.Contains(":") || uri.Contains(":/") || uri.Contains(":\\")) {
                // Local path, convert to canonical one.
                var fullPath = System.IO.Path.GetFullPath(uri).Replace('\\', '/');
                foreach (var p in FileStorageClient.ServerConfigs) {
                    if (p.Value.Prefix != null && fullPath.StartsWith(p.Value.Prefix)) {
                        uri = $"local:{p.Key}{fullPath.Substring(p.Value.Prefix.Length)}";
                        break;
                    }
                }

                if (!uri.Contains(":")) {
                    throw new Exception($"Path {uri} not in registered path.");
                }
            }

            var segments = uri.Split('/');
            var pathSegmentCount = segments.Length - 1;
            ParentPath = "/" + string.Join("/", segments.Skip(1).Take(pathSegmentCount - 1));
            if (!ParentPath.EndsWith("/")) {
                ParentPath += "/";
            }

            var name = segments.Last();
            var lastDot = name.LastIndexOf('.');
            if (lastDot < 0) {
                BaseName = name;
                Extension = "";
            } else {
                BaseName = name.Substring(0, lastDot);
                Extension = name.Substring(lastDot + 1);
            }

            Id = id ?? fileInfo?.Id ?? FileInformation.GetId(uri);
            this.fileInfo = fileInfo;

            Client = GetClient(segments[0]);

            FileFormat = PimixFileV2Format.Get(uri) ?? PimixFileV1Format.Get(uri) ??
                PimixFileV0Format.Get(uri) ?? RawFileFormat.Instance;
            UseCache = useCache;
        }

        public static string CacheLocation { get; set; }

        public static string SubPathIgnorePattern { get; set; } = "$^";

        public static string FullPathIgnorePattern { get; set; } = "$^";

        static Regex SubPathIgnoredFiles =>
            LazyInitializer.EnsureInitialized(ref subPathIgnoredFiles,
                () => new Regex(SubPathIgnorePattern, RegexOptions.Compiled));

        static Regex FullPathIgnoredFiles =>
            LazyInitializer.EnsureInitialized(ref fullPathIgnoredFiles,
                () => new Regex(FullPathIgnorePattern, RegexOptions.Compiled));

        public string Id { get; set; }

        // Ends with a slash.
        string ParentPath { get; }

        public KifaFile Parent => new KifaFile($"{Host}{ParentPath}");

        public KifaFile LocalCacheFile => new KifaFile($"{CacheLocation}{Id}");

        public string BaseName { get; set; }

        public string Extension { get; set; }

        public string Name => string.IsNullOrEmpty(Extension) ? BaseName : $"{BaseName}.{Extension}";

        public string Path => $"{ParentPath}{Name}";

        public string Host => Client?.ToString();

        public StorageClient Client { get; set; }

        PimixFileFormat FileFormat { get; }

        public FileInformation FileInfo =>
            fileInfo ??= SimpleMode ? new FileInformation() : FileInformation.Client.Get(Id);

        public bool UseCache { get; set; }

        public bool IsCloud =>
            (Client is BaiduCloudStorageClient || Client is GoogleDriveStorageClient ||
             Client is MegaNzStorageClient) && FileFormat is PimixFileV1Format;

        public int CompareTo(KifaFile other) {
            if (ReferenceEquals(this, other)) {
                return 0;
            }

            if (ReferenceEquals(null, other)) {
                return 1;
            }

            return string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
        }

        public bool Equals(KifaFile other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return string.Equals(ToString(), other.ToString());
        }

        static string GetUri(string id) {
            string candidate = null;
            var bestScore = 0L;
            var info = FileInformation.Client.Get(id);
            if (info.Locations != null) {
                foreach (var location in info.Locations) {
                    if (location.Value != null) {
                        var file = new KifaFile(location.Key, fileInfo: info);
                        if (file.Client is FileStorageClient && file.Exists()) {
                            return location.Key;
                        }

                        var score = 0;
                        if (file.Client is GoogleDriveStorageClient) {
                            score = 32;
                        }

                        if (file.Client is SwisscomStorageClient) {
                            score = 16;
                        }

                        if (file.Client is BaiduCloudStorageClient) {
                            score = 8;
                        }

                        if (file.FileFormat is PimixFileV2Format) {
                            score += 4;
                        }

                        if (file.FileFormat is PimixFileV1Format) {
                            score += 2;
                        }

                        if (file.FileFormat is PimixFileV0Format) {
                            score += 1;
                        }

                        if (score > bestScore) {
                            bestScore = score;
                            candidate = location.Key;
                        }
                    }
                }
            }

            if (bestScore > 0) {
                return candidate;
            }

            foreach (var p in FileStorageClient.ServerConfigs.Keys) {
                var location = $"local:{p}{id}";
                var score = new KifaFile(location).Length();
                if (score > bestScore) {
                    bestScore = score;
                    candidate = location;
                }
            }

            return candidate;
        }

        public KifaFile GetFile(string name) => new KifaFile($"{Host}{Path}/{name}");

        public KifaFile GetFileSuffixed(string suffix) => new KifaFile($"{Host}{Path}{suffix}");

        public KifaFile GetFilePrefixed(string prefix) => prefix == null ? this : new KifaFile($"{Host}{prefix}{Path}");

        public override string ToString() => $"{Host}{Path}";

        public bool Exists() => Client.Exists(Path);

        public bool ExistsSomewhere() => FileInfo.Locations?.Values.Any(v => v != null) == true;

        public long Length() => Client.Length(Path);

        public bool Registered => FileInfo.Locations?.GetValueOrDefault(ToString(), null) != null;

        public bool HasEntry => FileInfo.Locations?.ContainsKey(ToString()) == true;

        public FileInformation QuickInfo() =>
            FileFormat is RawFileFormat ? Client.QuickInfo(Path) : new FileInformation();

        public void Delete() => Client.Delete(Path);

        public void Touch() => Client.Touch(Path);

        public System.Collections.Generic.IEnumerable<KifaFile>
            List(bool recursive = false, bool ignoreFiles = true, string pattern = "*") =>
            Exists()
                ? Enumerable.Repeat(this, 1)
                : Client.List(Path, recursive)
                    .Where(f => IsMatch(f.Id, pattern) && (!ignoreFiles ||
                                                           !SubPathIgnoredFiles.IsMatch(f.Id.Substring(Path.Length)) &&
                                                           !FullPathIgnoredFiles.IsMatch(f.Id))).Select(info =>
                        new KifaFile(Host + info.Id, fileInfo: info));

        public static (bool isMultiple, System.Collections.Generic.List<KifaFile> files) ExpandFiles(
            System.Collections.Generic.IEnumerable<string> sources, string prefix = null, bool recursive = true,
            bool fullFile = false) {
            var multi = 0;
            var files = new System.Collections.Generic.List<(string sortKey, KifaFile value)>();
            foreach (var fileName in sources) {
                var fileInfo = new KifaFile(fileName);
                if (prefix != null && !fileInfo.Path.StartsWith(prefix)) {
                    fileInfo = fileInfo.GetFilePrefixed(prefix);
                }

                if (fileInfo.Exists()) {
                    multi++;
                    files.Add((fileInfo.ToString().GetNaturalSortKey(), fileInfo));
                } else {
                    var fileInfos = fileInfo.List(recursive).ToList();
                    if (fileInfos.Count > 0) {
                        multi = 2;
                        files.AddRange(fileInfos.Select(f => (f.ToString().GetNaturalSortKey(), f)));
                    } else {
                        multi++;
                        files.Add((fileInfo.ToString().GetNaturalSortKey(), fileInfo));
                    }
                }
            }

            files.Sort();

            return (multi > 1, files.Select(f => fullFile ? new KifaFile(f.value.ToString()) : f.value).ToList());
        }

        public static (bool isMultiple, System.Collections.Generic.List<KifaFile> files) ExpandLogicalFiles(
            System.Collections.Generic.IEnumerable<string> sources, string prefix = null, bool recursive = true,
            bool fullFile = false) {
            var multi = 0;
            var files = new System.Collections.Generic.List<(string sortKey, KifaFile value)>();
            foreach (var fileName in sources) {
                var fileInfo = new KifaFile(fileName);
                if (prefix != null && !fileInfo.Path.StartsWith(prefix)) {
                    fileInfo = fileInfo.GetFilePrefixed(prefix);
                }

                var path = fileInfo.Path;
                var host = fileInfo.Host;

                var thisFolder = FileInformation.Client.ListFolder(path, recursive);
                if (thisFolder.Count > 0) {
                    multi = 2;
                    files.AddRange(thisFolder.Select(f => (f.GetNaturalSortKey(), new KifaFile(host + f))));
                } else {
                    multi++;
                    files.Add((fileInfo.ToString().GetNaturalSortKey(), new KifaFile(host + path)));
                }
            }

            files.Sort();

            return (multi > 1, files.Select(f => fullFile ? new KifaFile(f.value.ToString()) : f.value).ToList());
        }

        static bool IsMatch(string path, string pattern) {
            path = path.Substring(path.LastIndexOf("/", StringComparison.Ordinal) + 1);
            var segments = pattern.Split("*", StringSplitOptions.RemoveEmptyEntries);
            var lastIndex = 0;
            foreach (var segment in segments) {
                lastIndex = path.IndexOf(segment, lastIndex, StringComparison.Ordinal);
                if (lastIndex < 0) {
                    return false;
                }

                lastIndex += segment.Length;
            }

            return true;
        }


        public void Copy(KifaFile destination, bool neverLink = false) {
            if (UseCache) {
                CacheFileToLocal();
                LocalCacheFile.Copy(destination, neverLink);
                return;
            }

            if (IsCompatible(destination)) {
                Client.Copy(Path, destination.Path, neverLink);
            } else {
                destination.Write(OpenRead());
            }
        }

        public void Move(KifaFile destination) {
            if (UseCache) {
                CacheFileToLocal();
                LocalCacheFile.Move(destination);
                return;
            }

            if (IsCompatible(destination)) {
                Client.Move(Path, destination.Path);
            } else {
                Copy(destination);
                Delete();
            }
        }

        public void CacheFileToLocal() {
            if (!LocalCacheFile.Registered) {
                logger.Info($"Caching {this} to {LocalCacheFile}...");
                LocalCacheFile.Write(OpenRead());
                LocalCacheFile.Add();
            }
        }

        public void RemoveLocalCacheFile() {
            if (UseCache) {
                logger.Info($"Remove cache file {LocalCacheFile}...");
                LocalCacheFile.Delete();
                LocalCacheFile.Unregister();
            }
        }

        public Stream OpenRead() =>
            new VerifiableStream(FileFormat.GetDecodeStream(Client.OpenRead(Path), FileInfo.EncryptionKey), FileInfo);

        public void Write(Stream stream) => Client.Write(Path, FileFormat.GetEncodeStream(stream, FileInfo));

        public void Write(byte[] data) => Write(new MemoryStream(data));

        public void Write(string text) => Write(new UTF8Encoding(false).GetBytes(text));

        public FileInformation CalculateInfo(FileProperties properties) {
            var info = FileInfo.Clone();
            info.RemoveProperties((FileProperties.AllVerifiable & properties) | FileProperties.Locations);

            using (var stream = OpenRead()) {
                info.AddProperties(stream, properties);
            }

            return info;
        }

        public FileProperties Add(bool alwaysCheck = false) {
            if (!Exists()) {
                throw new FileNotFoundException(ToString());
            }

            if (!alwaysCheck && (FileInfo.GetProperties() & FileProperties.All) == FileProperties.All && Registered) {
                var partialInfo = CalculateInfo(FileProperties.Size | FileProperties.SliceMd5);
                var compareResults = partialInfo.CompareProperties(FileInfo, FileProperties.AllVerifiable);
                if (compareResults != FileProperties.None) {
                    logger.Error($"Quick check failed for {ToString()} ({compareResults}).");
                    throw new Exception($"Quick check failed ({compareResults}).");
                }

                logger.Info($"Quick check passed for {ToString()}.");
                return FileProperties.None;
            }

            var file = this;
            if (UseCache) {
                CacheFileToLocal();
                file = LocalCacheFile;
            }

            var oldInfo = FileInfo;

            // Compare with quick info.
            var quickInfo = file.QuickInfo();
            logger.Debug("Quick info:\n{0}", JsonConvert.SerializeObject(quickInfo));

            var info = file.CalculateInfo(FileProperties.AllVerifiable | FileProperties.EncryptionKey);

            var quickCompareResult = info.CompareProperties(quickInfo, FileProperties.AllVerifiable);

            if (quickCompareResult != FileProperties.None) {
                logger.Warn("Quick data:\n{0}",
                    JsonConvert.SerializeObject(quickInfo.RemoveProperties(FileProperties.All ^ quickCompareResult),
                        Formatting.Indented));
                logger.Warn("Actual data:\n{0}",
                    JsonConvert.SerializeObject(info.RemoveProperties(FileProperties.All ^ quickCompareResult),
                        Formatting.Indented));
                return quickCompareResult;
            }

            var compareResultWithOld = info.CompareProperties(oldInfo, FileProperties.AllVerifiable);
            if (compareResultWithOld != FileProperties.None) {
                logger.Warn("Expected data:\n{0}",
                    JsonConvert.SerializeObject(oldInfo.RemoveProperties(FileProperties.All ^ compareResultWithOld),
                        Formatting.Indented));
                logger.Warn("Actual data:\n{0}",
                    JsonConvert.SerializeObject(info.RemoveProperties(FileProperties.All ^ compareResultWithOld),
                        Formatting.Indented));
                return compareResultWithOld;
            }

            var client = FileInformation.Client;

            var sha256Info = client.Get($"/$/{info.Sha256}");

            if (oldInfo.Sha256 == null && sha256Info.Sha256 == info.Sha256) {
                client.Link(sha256Info.Id, info.Id);
            }

            var compareResult = info.CompareProperties(sha256Info, FileProperties.AllVerifiable);
            if (compareResult == FileProperties.None) {
                info.EncryptionKey =
                    sha256Info.EncryptionKey ?? info.EncryptionKey; // Only happens for unencrypted file.

                client.Update(info);
                Register(true);
            } else {
                logger.Warn("Expected data:\n{0}",
                    JsonConvert.SerializeObject(sha256Info.RemoveProperties(FileProperties.All ^ compareResult),
                        Formatting.Indented));
                logger.Warn("Actual data:\n{0}",
                    JsonConvert.SerializeObject(info.RemoveProperties(FileProperties.All ^ compareResult),
                        Formatting.Indented));
            }

            return compareResult;
        }

        public void Register(bool verified = false) {
            FileInformation.Client.AddLocation(Id, ToString(), verified);
            fileInfo = null;
        }

        public void Unregister() {
            FileInformation.Client.RemoveLocation(Id, ToString());
            fileInfo = null;
        }

        public bool IsCompatible(KifaFile other) => Host == other.Host && FileFormat == other.FileFormat;

        static StorageClient GetClient(string spec) {
            if (knownClients.ContainsKey(spec)) {
                return knownClients[spec];
            }

            var specs = spec.Split(':');

            if (specs[1].Contains("+")) {
                // Sharded client.
                return knownClients[spec] = new ShardedStorageClient() {
                    Clients = specs[1].Split("+").Select(s => GetClient($"{specs[0]}:{s}")).ToList(),
                    ShardSize = SwisscomStorageClient.ShardSize
                };
            }

            switch (specs[0]) {
                case "baidu":
                    return knownClients[spec] = new BaiduCloudStorageClient {AccountId = specs[1]};
                case "google":
                    return knownClients[spec] = new GoogleDriveStorageClient {AccountId = specs[1]};
                case "mega":
                    return knownClients[spec] = new MegaNzStorageClient {AccountId = specs[1]};
                case "swiss":
                    return knownClients[spec] = new SwisscomStorageClient(specs[1]);
                case "local":
                    var c = new FileStorageClient {ServerId = specs[1]};
                    if (c.Server == null) {
                        c = null;
                    }

                    return knownClients[spec] = c;
            }

            return knownClients[spec];
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj.GetType() == GetType() && Equals((KifaFile) obj);
        }

        public override int GetHashCode() => ToString() != null ? ToString().GetHashCode() : 0;

        public string CreateLocation(CloudServiceType serviceType, CloudFormatType formatType) =>
            FileInfo.Sha256 == null || FileInfo.Size == null
                ? null
                : FileInfo.Locations.Keys.FirstOrDefault(l =>
                    new Regex($@"^{serviceType}:[^/]+/\$/{FileInfo.Sha256}\.{formatType.ToString().ToLower()}$")
                        .Match(l).Success) ?? serviceType switch {
                    CloudServiceType.Google => $"google:good/$/{FileInfo.Sha256}.{formatType.ToString().ToLower()}",
                    CloudServiceType.Swiss =>
                        // TODO: Use format specific header size.
                        $"swiss:{SwisscomStorageClient.FindAccounts(FileInfo.Id, FileInfo.Size.Value + 0x30)}/$/{FileInfo.Sha256}.{formatType.ToString().ToLower()}",
                    _ => ""
                };
    }

    public enum CloudServiceType {
        Google,
        Baidu,
        Mega,
        Swiss
    }

    public enum CloudFormatType {
        V1,
        V2
    }
}
