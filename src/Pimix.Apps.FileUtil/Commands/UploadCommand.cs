using System;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("upload", HelpText = "Upload file to a cloud location.")]
    class UploadCommand : PimixCommand {
        public static string TempLocation { get; set; }

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true)]
        public string FileUri { get; set; }

        [Option('d', "delete-source", HelpText = "Remove source if upload is successful.")]
        public bool DeleteSource { get; set; } = false;

        [Option('q', "quick", HelpText =
            "Finish quickly by not verifying validity of destination.")]
        public bool QuickMode { get; set; } = false;

        [Option('s', "service", HelpText =
            "Type of service to upload to. Default is google. Allowed values: [google, baidu, mega]")]
        public CloudServiceType ServiceType { get; set; } = CloudServiceType.google;

        [Option('f', "format", HelpText =
            "Format used to upload file. Default is v1. Allowed values: [v1, v2]")]
        public CloudFormatType FormatType { get; set; } = CloudFormatType.v1;

        [Option('c', "use-cache", HelpText = "Use cache to help upload.")]
        public bool UseCache { get; set; }

        public override int Execute() {
            var source = new PimixFile(FileUri);
            if (source.Client == null) {
                Console.WriteLine($"Source {FileUri} not accessible. Wrong server?");
                return 1;
            }

            var files = source.List(true).ToList();
            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                var removalText = DeleteSource ? " and remove them afterwards" : "";
                Console.Write($"Confirm uploading the {files.Count} files above{removalText}?");
                Console.ReadLine();

                return files.Select(f => UploadFile(new PimixFile(f.ToString()))).Max();
            }

            if (source.Exists()) {
                return UploadFile(source);
            }

            logger.Error("Source {0} doesn't exist or folder contains no files.", source);
            return 1;
        }

        int UploadFile(PimixFile source) {
            source.UseCache = UseCache;
            // TODO: Better catching.
            try {
                source.Register();
                logger.Info("Checking source {0}...", source);
                var sourceCheckResult = source.Add();

                if (sourceCheckResult != FileProperties.None) {
                    logger.Error("Source is wrong! The following fields differ: {0}",
                        sourceCheckResult);
                    return 1;
                }

                var destinationLocation =
                    FileInformation.Client.CreateLocation(source.Id, ServiceType.ToString(), FormatType.ToString());
                var destination = new PimixFile(destinationLocation);

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

                        destination.RemoveLocalCacheFile();

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
