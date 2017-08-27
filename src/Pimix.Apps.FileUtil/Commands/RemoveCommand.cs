using System;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("rm", HelpText = "Remove the FILE. Can be either logic path like: /Software/... or real path like: local:desk/Software....")]
    class RemoveCommand : FileUtilCommand {
        [Value(0, MetaName = "FILE", MetaValue = "STRING", HelpText = "File to be removed.")]
        public string FileUri { get; set; }

        [Option('i', "id", HelpText = "ID for the uri.")]
        public string FileId { get; set; }

        [Option('l', "link", HelpText = "Remove link only.")]
        public bool RemoveLinkOnly { get; set; }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            if (String.IsNullOrEmpty(FileUri)) {
                var info = FileInformation.Get(FileId);

                // Remove logical file.
                if (!RemoveLinkOnly && info.Locations != null) {
                    // Remove real files.
                    foreach (var location in info.Locations) {
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
            } else {
                var file = new PimixFile(FileUri, FileId);
                if (file.FileInfo.Locations == null || !file.FileInfo.Locations.Contains(FileUri)) {
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

                FileInformation.RemoveLocation(file.Id, FileUri);
                logger.Info($"Entry {file} removed.");

                return 0;
            }
        }
    }
}
