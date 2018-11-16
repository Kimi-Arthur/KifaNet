using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Pimix.Cloud.BaiduCloud;
using Pimix.Cloud.GoogleDrive;
using Pimix.Cloud.MegaNz;
using Pimix.IO;
using Pimix.IO.FileFormats;
using Pimix.Service;

namespace Pimix.Api.Files {
    public class PimixFile {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string IgnoredFilesPattern { get; set; } = "$^";

        public static bool PreferBaiduCloud { get; set; } = false;

        static Regex ignoredFiles;

        static Regex IgnoredFiles
            => LazyInitializer.EnsureInitialized(ref ignoredFiles,
                () => new Regex(IgnoredFilesPattern, RegexOptions.Compiled));

        static readonly Dictionary<string, StorageClient> knownClients =
            new Dictionary<string, StorageClient>();

        public string Id { get; set; }

        string ParentPath { get; set; }

        public PimixFile Parent => new PimixFile($"{Host}{ParentPath}");

        public string BaseName { get; set; }

        string Extension { get; set; }

        string Name => string.IsNullOrEmpty(Extension) ? BaseName : $"{BaseName}.{Extension}";

        public string Path => $"{ParentPath}/{Name}";

        public string Host => Client.ToString();

        public StorageClient Client { get; set; }

        PimixFileFormat FileFormat { get; set; }

        readonly FileInformation fileInfo;
        public FileInformation FileInfo => fileInfo ?? PimixService.Get<FileInformation>(Id);

        public PimixFile(string uri = null, string id = null, FileInformation fileInfo = null) {
            if (uri == null) {
                // Infer uri from id.
                uri = GetUri(id ?? fileInfo?.Id);
            }

            // Example uri:
            //   baidu:Pimix_1;v1/a/b/c/d.txt
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
                    if (fullPath.StartsWith(p.Value.Prefix)) {
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

            FileFormat = PimixFileV1Format.Get(uri) ??
                         PimixFileV0Format.Get(uri) ?? RawFileFormat.Instance;
        }

        static string GetUri(string id) {
            var bestRemoteLocation = "";
            var bestScore = 0;
            var info = PimixService.Get<FileInformation>(id);
            foreach (var location in info.Locations) {
                if (location.Value != null) {
                    var file = new PimixFile(location.Key, fileInfo: info);
                    if (file.Client is FileStorageClient && file.Exists()) {
                        return location.Key;
                    }

                    var score = (PreferBaiduCloud
                        ? file.Client is BaiduCloudStorageClient
                        : file.Client is GoogleDriveStorageClient)
                        ? 8
                        : 4;
                    score += file.FileFormat is PimixFileV1Format ? 2 :
                        file.FileFormat is PimixFileV0Format ? 1 : 0;
                    if (score > bestScore) {
                        bestScore = score;
                        bestRemoteLocation = location.Key;
                    }
                }
            }

            return bestRemoteLocation;
        }

        public PimixFile GetFile(string name) => new PimixFile($"{Host}{Path}/{name}");

        public override string ToString() => $"{Host}{Path}";

        public bool Exists() => Client.Exists(Path);

        public FileInformation QuickInfo()
            => FileFormat is RawFileFormat ? Client.QuickInfo(Path) : new FileInformation();

        public void Delete() => Client.Delete(Path);

        public IEnumerable<PimixFile> List(bool recursive = false, bool ignoreFiles = true,
            string pattern = "*")
            => Client.List(Path, recursive, pattern)
                .Where(f => !ignoreFiles || !IgnoredFiles.IsMatch(f.Id))
                .Select(info => new PimixFile(Host + info.Id, fileInfo: info));

        public void Copy(PimixFile destination) {
            if (IsCompatible(destination)) {
                Client.Copy(Path, destination.Path);
            } else {
                destination.Write(OpenRead());
            }
        }

        public void Move(PimixFile destination) {
            if (IsCompatible(destination)) {
                Client.Move(Path, destination.Path);
            } else {
                Copy(destination);
                Delete();
            }
        }

        public Stream OpenRead()
            => new VerifiableStream(
                FileFormat.GetDecodeStream(Client.OpenRead(Path), FileInfo.EncryptionKey),
                FileInfo);

        public void Write(Stream stream)
            => Client.Write(Path, FileFormat.GetEncodeStream(stream, FileInfo));

        public FileInformation CalculateInfo(FileProperties properties) {
            var info = FileInfo;
            info.RemoveProperties((FileProperties.AllVerifiable & properties) |
                                  FileProperties.Locations);

            using (var stream = OpenRead()) {
                info.AddProperties(stream, properties);
            }

            return info;
        }

        public FileProperties Add(bool alwaysCheck = false) {
            if (!Exists()) {
                throw new FileNotFoundException(ToString());
            }

            var oldInfo = FileInfo;
            if (!alwaysCheck &&
                (oldInfo.GetProperties() & FileProperties.All) == FileProperties.All &&
                oldInfo.Locations?.GetValueOrDefault(ToString(), null) != null) {
                logger.Debug("Skipped checking for {0}.", ToString());
                return FileProperties.None;
            }

            // Compare with quick info.
            var quickInfo = QuickInfo();
            logger.Debug("Quick info:\n{0}", JsonConvert.SerializeObject(quickInfo));

            var info = CalculateInfo(FileProperties.AllVerifiable | FileProperties.EncryptionKey);

            var quickCompareResult =
                info.CompareProperties(quickInfo, FileProperties.AllVerifiable);

            if (quickCompareResult != FileProperties.None) {
                logger.Warn(
                    "Quick data:\n{0}",
                    JsonConvert.SerializeObject(
                        quickInfo.RemoveProperties(FileProperties.All ^ quickCompareResult),
                        Formatting.Indented));
                logger.Warn(
                    "Actual data:\n{0}",
                    JsonConvert.SerializeObject(
                        info.RemoveProperties(FileProperties.All ^ quickCompareResult),
                        Formatting.Indented));
                return quickCompareResult;
            }

            var sha256Info = PimixService.Get<FileInformation>($"/$/{info.Sha256}");

            if (FileInfo.Sha256 == null && sha256Info.Sha256 == info.Sha256) {
                PimixService.Link<FileInformation>(sha256Info.Id, info.Id);
            }

            oldInfo = FileInfo;

            var compareResult = info.CompareProperties(oldInfo, FileProperties.AllVerifiable);
            if (compareResult == FileProperties.None) {
                info.EncryptionKey =
                    oldInfo.EncryptionKey ??
                    info.EncryptionKey; // Only happens for unencrypted file.

                PimixService.Patch(info);
                Register(true);
            } else {
                logger.Warn(
                    "Expected data:\n{0}",
                    JsonConvert.SerializeObject(
                        oldInfo.RemoveProperties(FileProperties.All ^ compareResult),
                        Formatting.Indented));
                logger.Warn(
                    "Actual data:\n{0}",
                    JsonConvert.SerializeObject(
                        info.RemoveProperties(FileProperties.All ^ compareResult),
                        Formatting.Indented));
            }

            return compareResult;
        }

        public void Register(bool verified = false)
            => FileInformation.AddLocation(Id, ToString(), verified);

        public bool IsCloud
            => (Client is BaiduCloudStorageClient || Client is GoogleDriveStorageClient ||
                Client is MegaNzStorageClient) && FileFormat is PimixFileV1Format;

        public bool IsCompatible(PimixFile other)
            => Host == other.Host && FileFormat == other.FileFormat;

        static StorageClient GetClient(string spec) {
            if (knownClients.ContainsKey(spec)) {
                return knownClients[spec];
            }

            var specs = spec.Split(':');
            switch (specs[0]) {
                case "baidu":
                    return knownClients[spec] = new BaiduCloudStorageClient {AccountId = specs[1]};
                case "google":
                    return knownClients[spec] = new GoogleDriveStorageClient {AccountId = specs[1]};
                case "mega":
                    return knownClients[spec] = new MegaNzStorageClient {AccountId = specs[1]};
                case "local":
                    var c = new FileStorageClient {ServerId = specs[1]};
                    if (c.Server == null) {
                        c = null;
                    }

                    return knownClients[spec] = c;
            }

            return knownClients[spec];
        }
    }
}
