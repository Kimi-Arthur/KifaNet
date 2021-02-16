using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands {
    [Verb("trash", HelpText = "Move the file to trash.")]
    class TrashCommand : KifaFileCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static readonly FileInformationServiceClient client = FileInformation.Client;

        public override bool ById => true;

        protected override bool IterateOverLogicalFiles => true;

        protected override Func<List<string>, string> FileInformationConfirmText
            => files => $"Confirm trashing the {files.Count} files above?";

        protected override int ExecuteOneFileInformation(string file) {
            var segments = file.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var options = new List<string> {"/"};
            foreach (var segment in segments) {
                options.Add($"{options.Last()}{segment}/");
            }

            var (choice, index) = SelectOne(options, op => op + ".Trash");
            if (choice != null) {
                return Trash(file, choice);
            }

            logger.Info($"File {file} not trashed as a destination is not selected.");
            return 0;
        }

        static int Trash(string file, string choice) {
            var target = choice + ".Trash/" + file.Substring(choice.Length);
            client.Link(file, target);
            logger.Info($"Linked original FileInfo {file} to new FileInfo {target}.");

            var targetInfo = client.Get(target);
            if (targetInfo.Locations != null) {
                foreach (var location in targetInfo.Locations.Keys) {
                    var instance = new KifaFile(location);
                    if (instance.Client == null) {
                        logger.Warn($"{instance} not accessible.");
                        continue;
                    }

                    if (instance.Id == file) {
                        if (instance.Exists()) {
                            instance.Delete();
                            logger.Info($"File {instance} deleted.");
                        } else {
                            logger.Warn($"File {instance} not found.");
                        }

                        client.RemoveLocation(targetInfo.Id, location);
                        logger.Info($"Entry {location} removed.");
                    }
                }

                client.Delete(file);
                logger.Info($"Original FileInfo {file} removed.");
                return 0;
            }

            logger.Warn($"No locations found in {targetInfo}.");
            return 1;
        }
    }
}
