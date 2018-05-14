using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using Pimix.Cloud.BaiduCloud;
using Pimix.Cloud.MegaNz;
using Pimix.IO;
using Pimix.IO.FileFormats;

namespace Pimix.Api.Files {
    public class PimixFile {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public string Id { get; set; }

        public string Path { get; set; }

        public string Spec
            => string.Join(";", new string[]
            {
            Client.ToString(),
            FileFormat.ToString()
            }.Where((x) => x != null));

        public StorageClient Client { get; set; }

        public PimixFileFormat FileFormat { get; set; }

        public FileInformation FileInfo => FileInformation.Get(Id);

        public PimixFile(string uri, string id = null) {
            // Example uri:
            //   baidu:Pimix_1;v1/a/b/c/d.txt
            //   mega:0z/a/b/c/d.txt
            //   local:cubie/a/b/c/d.txt
            //   local:/a/b/c/d.txt
            //   /a/b/c/d.txt
            var segments = uri.Split(new char[] { '/' }, 2);
            Path = "/" + segments[1];
            Id = id ?? FileInformation.GetId(uri);

            var spec = string.IsNullOrEmpty(segments[0]) ? GetSpec(Path) : segments[0];

            Client = BaiduCloudStorageClient.Get(spec) ?? MegaNzStorageClient.Get(spec) ?? FileStorageClient.Get(spec);

            FileFormat = PimixFileV1Format.Get(spec) ?? PimixFileV0Format.Get(spec) ?? RawFileFormat.Get(spec);
        }

        public override string ToString()
            => $"{Spec}{Path}";

        string GetSpec(string Path) {
            var info = FileInformation.Get(Path);
            if (info == null) {
                return "";
            }

            foreach (var location in info.Locations) {
                // TODO: Will add selection logic here.
                return location;
            }

            return "";
        }

        public bool Exists()
            => Client.Exists(Path);

        public FileInformation QuickInfo()
            => FileFormat is RawFileFormat ? Client.QuickInfo(Path) : new FileInformation();

        public void Delete()
            => Client.Delete(Path);

        public void Copy(PimixFile destination) {
            if (Spec == destination.Spec) {
                Client.Copy(Path, destination.Path);
            } else {
                destination.Write(OpenRead());
            }
        }

        public void Move(PimixFile destination) {
            if (Spec == destination.Spec) {
                Client.Move(Path, destination.Path);
            } else {
                Copy(destination);
                Delete();
            }
        }

        Stream OpenRead() {
            if (FileInfo.Size != null) {
                return new VerfiableStream(FileFormat.GetDecodeStream(Client.OpenRead(Path), FileInfo.EncryptionKey), FileInfo);
            } else {
                return FileFormat.GetDecodeStream(Client.OpenRead(Path), FileInfo.EncryptionKey);
            }
        }

        void Write(Stream stream)
            => Client.Write(Path, FileFormat.GetEncodeStream(stream, FileInfo));

        public FileInformation CalculateInfo(FileProperties properties) {
            if (!Exists()) {
                throw new FileNotFoundException(ToString());
            }

            var info = FileInfo;
            info.RemoveProperties(FileProperties.AllVerifiable & properties | FileProperties.Locations);

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
            if ((oldInfo.GetProperties() & FileProperties.All) == FileProperties.All && !alwaysCheck && oldInfo.Locations != null && oldInfo.Locations.Contains(ToString())) {
                logger.Debug("Skipped checking for {0}.", ToString());
                return FileProperties.None;
            }

            // Compare with quick info.
            var quickInfo = QuickInfo();
            logger.Debug("Quick info:\n{0}", JsonConvert.SerializeObject(quickInfo));

            var info = CalculateInfo(FileProperties.AllVerifiable | FileProperties.EncryptionKey);

            var quickCompareResult = info.CompareProperties(quickInfo, FileProperties.AllVerifiable);

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
            }

            var sha256Info = FileInformation.Get($"/$/{info.SHA256}");

            if (FileInfo.SHA256 == null && sha256Info.SHA256 == info.SHA256) {
                // One same file already exists.
                FileInformation.Link(sha256Info.Id, info.Id);
            }

            oldInfo = FileInfo;

            var compareResult = info.CompareProperties(oldInfo, FileProperties.AllVerifiable);
            if (compareResult == FileProperties.None) {
                info.EncryptionKey = oldInfo.EncryptionKey ?? info.EncryptionKey;  // Only happens for unencrypted file.

                FileInformation.Patch(info);
                FileInformation.AddLocation(Id, ToString());
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
    }
}