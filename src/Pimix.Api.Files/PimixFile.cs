using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using Pimix.Cloud.BaiduCloud;
using Pimix.Cloud.GoogleDrive;
using Pimix.Cloud.MegaNz;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace Pimix.Api.Files {
    public class PimixFile {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string IgnoredFilesPattern { get; set; } = "$^";

        static Regex ignoredFiles;

        static Regex IgnoredFiles => ignoredFiles = ignoredFiles ?? new Regex(IgnoredFilesPattern);

        static readonly Dictionary<string, StorageClient> KnownClients =
            new Dictionary<string, StorageClient>();

        public string Id { get; set; }

        public string Path { get; set; }

        public string Host => Client.ToString();

        public StorageClient Client { get; set; }

        public PimixFileFormat FileFormat { get; set; }

        readonly FileInformation _fileInfo;
        public FileInformation FileInfo => _fileInfo ?? FileInformation.Get(Id);

        public PimixFile(string uri, string id = null, FileInformation fileInfo = null) {
            // Example uri:
            //   baidu:Pimix_1;v1/a/b/c/d.txt
            //   mega:0z/a/b/c/d.txt
            //   local:cubie/a/b/c/d.txt
            //   local:/a/b/c/d.txt
            //   /a/b/c/d.txt
            //   C:/files/a.txt
            //   ~/a.txt
            //   ../a.txt
            if (!uri.Contains(":") || uri.Contains(":/")) {
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

            var segments = uri.Split(new[] {'/'}, 2);
            Path = "/" + segments[1];
            Id = id ?? fileInfo?.Id ?? FileInformation.GetId(uri);
            _fileInfo = fileInfo;

            var spec = segments[0];

            if (!KnownClients.ContainsKey(spec)) {
                KnownClients[spec] = BaiduCloudStorageClient.Get(spec) ??
                                     GoogleDriveStorageClient.Get(spec) ??
                                     MegaNzStorageClient.Get(spec) ?? FileStorageClient.Get(spec);
            }

            Client = KnownClients[spec];

            FileFormat = PimixFileV1Format.Get(uri) ??
                         PimixFileV0Format.Get(uri) ?? RawFileFormat.Instance;
        }

        public override string ToString() => $"{Host}{Path}";

        public bool Exists() => Client.Exists(Path);

        public FileInformation QuickInfo()
            => FileFormat is RawFileFormat ? Client.QuickInfo(Path) : new FileInformation();

        public void Delete() => Client.Delete(Path);

        public IEnumerable<PimixFile> List(bool recursive = false)
            => Client.List(Path, recursive: recursive).Where(f => !IgnoredFiles.IsMatch(f.Id))
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

            var sha256Info = FileInformation.Get($"/$/{info.SHA256}");

            if (FileInfo.SHA256 == null && sha256Info.SHA256 == info.SHA256) {
                FileInformation.Link(sha256Info.Id, info.Id);
            }

            oldInfo = FileInfo;

            var compareResult = info.CompareProperties(oldInfo, FileProperties.AllVerifiable);
            if (compareResult == FileProperties.None) {
                info.EncryptionKey =
                    oldInfo.EncryptionKey ??
                    info.EncryptionKey; // Only happens for unencrypted file.

                FileInformation.Patch(info);
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
    }
}
