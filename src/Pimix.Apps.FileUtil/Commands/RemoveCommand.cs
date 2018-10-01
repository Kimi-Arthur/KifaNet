using System;
using System.Linq;
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

        [Option('f', "force", HelpText =
            "Remove all instances of the file, including file with different name and in cloud.")]
        public bool ForceRemove { get; set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            if (string.IsNullOrEmpty(FileUri)) {
                var files = FileInformation.ListFolder(FileId, true);
                if (files.Count > 0) {
                    foreach (var file in files) {
                        Console.WriteLine(file);
                    }

                    var removalText = RemoveLinkOnly ? "" : " and remove them from file system";
                    Console.Write($"Confirm deleting the {files.Count} files above{removalText}?");
                    Console.ReadLine();

                    return files.Select(f => RemoveLogicalFile(FileInformation.Get(f))).Max();
                }

                return RemoveLogicalFile(FileInformation.Get(FileId));
            }

            var source = new PimixFile(FileUri);
            if (source.Client == null) {
                Console.WriteLine($"Source {FileUri} not accessible. Wrong server?");
                return 1;
            }

            var localFiles = source.List(true).ToList();
            if (localFiles.Count > 0) {
                foreach (var file in localFiles) {
                    Console.WriteLine(file);
                }

                var removalText = RemoveLinkOnly ? "" : " and remove them from file system";
                Console.Write($"Confirm deleting the {localFiles.Count} files above{removalText}?");
                Console.ReadLine();

                return localFiles.Select(f => RemoveFileInstance(new PimixFile(f.ToString()))).Max();
            }

            return RemoveFileInstance(source);
        }

        int RemoveLogicalFile(FileInformation info) {
            if (!RemoveLinkOnly && info.Locations != null) {
                foreach (var location in info.Locations.Keys) {
                    var file = new PimixFile(location);
                    var toRemove = file.Id == info.Id;
                    if (!toRemove && ForceRemove) {
                        Console.Write($"Confirm removing instance {file}, not matching file name? [Y/n] ");
                        toRemove = !Console.ReadLine().ToLower().StartsWith("n");
                    }

                    if (toRemove) {
                        if (file.Exists()) {
                            file.Delete();
                            logger.Info($"File {file} deleted.");
                        } else {
                            logger.Warn($"File {file} not found.");
                        }

                        FileInformation.RemoveLocation(info.Id, location);
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
            if (file.FileInfo.Locations?.ContainsKey(file.ToString()) != true) {
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

            FileInformation.RemoveLocation(file.Id, file.ToString());
            logger.Info($"Entry {file} removed.");

            return 0;
        }
    }
}
