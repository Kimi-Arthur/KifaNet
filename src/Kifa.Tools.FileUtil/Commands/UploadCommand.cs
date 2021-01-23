using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using Kifa.Api.Files;
using Kifa.IO;

namespace Kifa.Tools.FileUtil.Commands {
    [Verb("upload", HelpText = "Upload file to a cloud location.")]
    class UploadCommand : PimixCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
        public IEnumerable<string> FileNames { get; set; }

        [Option('d', "delete-source",
            HelpText = "Remove source if upload is successful. Won't remove valid cloud version.")]
        public bool DeleteSource { get; set; } = false;

        [Option('q', "quick", HelpText =
            "Finish quickly by not verifying validity of destination.")]
        public bool QuickMode { get; set; } = false;

        [Option('s', "service", HelpText =
            "Type of service to upload to. Default is google. Allowed values: [google, baidu, mega, swiss]")]
        public CloudServiceType ServiceType { get; set; } = CloudServiceType.Google;

        [Option('f', "format", HelpText =
            "Format used to upload file. Default is v1. Allowed values: [v1, v2]")]
        public CloudFormatType FormatType { get; set; } = CloudFormatType.V1;

        [Option('c', "use-cache", HelpText = "Use cache to help upload.")]
        public bool UseCache { get; set; }

        public override int Execute() {
            var (multi, files) = KifaFile.ExpandFiles(FileNames);
            if (multi) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                var removalText = DeleteSource ? " and remove them afterwards" : "";
                Console.Write($"Confirm uploading the {files.Count} files above{removalText}?");
                Console.ReadLine();
            }

            var results = files.Select(f => (f.ToString(), UploadFile(new KifaFile(f.ToString()), true))).ToList();
            return results.Select(r => r.Item2)
                .Concat(results.Where(r => r.Item2 == -1).Select(r => UploadFile(new KifaFile(r.Item1))))
                .Max();
        }

        int UploadFile(KifaFile source, bool skipRegistered = false) {
            source.UseCache = UseCache;
            // TODO: Better catching.
            try {
                var destinationLocation = source.CreateLocation(ServiceType, FormatType);
                if (!DeleteSource && skipRegistered) {
                    if (destinationLocation != null && new KifaFile(destinationLocation).Registered) {
                        logger.Info($"Skipped uploading of {source} to {destinationLocation} for now " +
                                    "as it's supposed to be already uploaded...");
                        return -1;
                    }
                }

                source.Register();
                logger.Info("Checking source {0}...", source);
                try {
                    var sourceCheckResult = source.Add();

                    if (sourceCheckResult != FileProperties.None) {
                        logger.Error("Source is wrong! The following fields differ: {0}",
                            sourceCheckResult);
                        return 1;
                    }
                } catch (FileNotFoundException ex) {
                    source.Unregister();
                    logger.Error(ex, "Source file not found.");
                    return 1;
                }

                destinationLocation ??= source.CreateLocation(ServiceType, FormatType);
                var destination = new KifaFile(destinationLocation);

                if (destination.Exists()) {
                    destination.Register();
                    if (QuickMode) {
                        Console.WriteLine($"Skipped verifying of {destination} as quick mode is enabled.");
                        return 0;
                    }

                    var destinationCheckResult = destination.Add();

                    if (destinationCheckResult == FileProperties.None) {
                        logger.Info("Already uploaded!");

                        if (DeleteSource) {
                            if (source.IsCloud) {
                                logger.Info("Source {0} is not removed as it's in cloud.",
                                    source);
                                source.Register(true);
                                return 0;
                            }

                            source.Delete();
                            FileInformation.Client.RemoveLocation(source.Id, source.ToString());
                            logger.Info("Source {0} removed since upload is successful.",
                                source);
                        }

                        return 0;
                    }

                    logger.Warn("Destination exists, but doesn't match.");
                    return 2;
                }

                destination.Unregister();
                destination.Register();

                logger.Info("Copying {0} to {1}...", source, destination);
                source.Copy(destination);

                if (destination.Exists()) {
                    destination.Register();
                    if (QuickMode) {
                        Console.WriteLine($"Skipped verifying of {destination} as quick mode is enabled.");
                        return 0;
                    }

                    logger.Info("Checking {0}...", destination);
                    var destinationCheckResult = destination.Add();
                    if (destinationCheckResult == FileProperties.None) {
                        logger.Info("Successfully uploaded {0} to {1}!", source, destination);

                        source.RemoveLocalCacheFile();

                        if (DeleteSource) {
                            if (source.IsCloud) {
                                logger.Info("Source {0} is not removed as it's in cloud.",
                                    source);
                                source.Register(true);
                                return 0;
                            }

                            source.Delete();
                            FileInformation.Client.RemoveLocation(source.Id, source.ToString());
                            logger.Info("Source {0} removed since upload is successful.",
                                source);
                        } else {
                            source.Register(true);
                        }

                        return 0;
                    }

                    destination.Delete();
                    logger.Fatal("Upload failed! The following fields differ (removed): {0}",
                        destinationCheckResult);
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
