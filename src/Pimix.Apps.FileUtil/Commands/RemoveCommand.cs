using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("rm", HelpText =
        "Remove the FILE. Can be either logic path like: /Software/... or real path like: local:desk/Software....")]
    class RemoveCommand : FileUtilCommand {
        [Value(0, MetaName = "FILE", MetaValue = "STRING", HelpText = "File to be removed.")]
        public string FileUri { get; set; }

        [Option('i', "id", HelpText = "ID for the uri.")]
        public string FileId { get; set; }

        [Option('l', "link", HelpText = "Remove link only.")]
        public bool RemoveLinkOnly { get; set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            if (string.IsNullOrEmpty(FileUri)) {
                return RemoveLogicalFile(FileInformation.Get(FileId));
            }

            return RemoveFileInstance(new PimixFile(FileUri, FileId));
        }

        int RemoveLogicalFile(FileInformation info) {
            if (!RemoveLinkOnly && info.Locations != null) {
                foreach (var location in info.Locations.Keys) {
                    var file = new PimixFile(location);
                    if (file.Id == FileId) {
                        if (file.Exists()) {
                            file.Delete();
                            logger.Info($"File {file} deleted.");
                        } else {
                            logger.Warn($"File {file} not found.");
                        }

                        FileInformation.RemoveLocation(FileId, location);
                        logger.Info($"Entry {location} removed.");
                    }
                }
            }

            // Logical removal.
            FileInformation.Delete(info.Id);
            logger.Info($"FileInfo {info.Id} removed.");
            return 0;
        }

        int RemoveFileInstance(PimixFile file) {
            var f = new PimixFile(FileUri);
            if (file.FileInfo.Locations?.ContainsKey(f.ToString()) != true) {
                if (file.Exists()) {
                    file.Delete();
                    logger.Warn($"File {file} deleted, no entry found though.");
                } else {
                    logger.Warn($"File {file} not found.");
                }

                return 0;
            }

            // Remove specific location item.
            if (!RemoveLinkOnly) {
                if (file.Exists()) {
                    file.Delete();
                    logger.Info($"File {file} deleted.");
                } else {
                    logger.Warn($"File {file} not found.");
                }
            }

            FileInformation.RemoveLocation(file.Id, f.ToString());
            logger.Info($"Entry {file} removed.");

            return 0;
        }
    }
}
