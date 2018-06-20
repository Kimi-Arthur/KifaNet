using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("upload", HelpText = "Upload file to a cloud location.")]
    class UploadCommand : FileUtilCommand {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        [Option('f', "folder", HelpText = "Upload the whole folder.")]
        public bool IsFolder { get; set; } = false;

        [Option('r', "remove-source", HelpText = "Remove source if upload is successful.")]
        public bool RemoveSource { get; set; } = false;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var source = new PimixFile(FileUri);

            if (IsFolder) {
                var files = source.List(true).ToList();
                if (files.Count > 0) {
                    foreach (var file in files) {
                        Console.WriteLine(file);
                    }

                    string removalText = RemoveSource ? " and remove them afterwards" : "";
                    Console.Write($"Confirm uploading the {files.Count} files above{removalText}?");
                    Console.ReadLine();

                    return files.Select(f => UploadFile(new PimixFile(f.ToString()))).Max();
                }

                Console.Write($"No files found in {FileUri}.");
                return 0;
            }

            if (source.Exists()) {
                return UploadFile(source);
            }

            logger.Error("Source {0} doesn't exist", FileUri);
            return 1;
        }

        int UploadFile(PimixFile source) {
            // TODO: Better catching.
            try {
                logger.Info("Checking source {0}...", source);
                var sourceCheckResult = source.Add();

                if (sourceCheckResult != FileProperties.None) {
                    logger.Error("Source is wrong! The following fields differ: {0}",
                        sourceCheckResult);
                    return 1;
                }

                var destinationLocation = FileInformation.CreateLocation(source.Id);
                var destination = new PimixFile(destinationLocation);

                if (destination.Exists()) {
                    var destinationCheckResult = destination.Add();

                    if (destinationCheckResult == FileProperties.None) {
                        logger.Info("Already uploaded!");

                        if (RemoveSource) {
                            source.Delete();
                            FileInformation.RemoveLocation(source.Id, source.ToString());
                            logger.Info("Source {0} removed since upload is successful.", source);
                        }

                        return 0;
                    }

                    logger.Warn("Destination exists, but doesn't match.");
                    return 2;
                }

                logger.Info("Copying {0} to {1}...", source, destination);

                source.Copy(destination);

                if (destination.Exists()) {
                    logger.Info("Checking {0}...", destination);
                    var destinationCheckResult = destination.Add();
                    if (destinationCheckResult == FileProperties.None) {
                        logger.Info("Successfully uploaded {0} to {1}!", source, destination);

                        if (RemoveSource) {
                            source.Delete();
                            FileInformation.RemoveLocation(source.Id, source.ToString());
                            logger.Info("Source {0} removed since upload is successful.", source);
                        }

                        return 0;
                    }

                    destination.Delete();
                    logger.Fatal(
                        "Upload failed! The following fields differ (removed): {0}",
                        destinationCheckResult
                    );
                    return 2;
                }

                logger.Fatal("Destination doesn't exist unexpectedly!");
                return 2;
            } catch (Exception ex) {
                logger.Fatal(ex, "Unexpected error");
                return 127;
            }
        }
    }
}
