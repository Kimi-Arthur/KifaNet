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

        [Option('t', "use-temp", HelpText = "Use local temp file to help upload.")]
        public bool UseTemp { get; set; }

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

                PimixFile temp = null;

                if (UseTemp) {
                    if (TempLocation == null) {
                        return 1;
                    }

                    temp = new PimixFile(TempLocation + destination.Id);
                    if (!CopyToTemp(source, temp)) {
                        return 2;
                    }

                    logger.Info("Copying from temp {0} to {1}...", temp, destination);
                    temp.Copy(destination);
                } else {
                    logger.Info("Copying from source {0} to {1}...", source, destination);
                    source.Copy(destination);
                }

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

                        if (UseTemp && temp != null) {
                            temp.Delete();
                            FileInformation.Client.RemoveLocation(temp.Id, temp.ToString());
                            logger.Info("Temp {0} removed as upload is successful.", temp);
                        }

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

        static bool CopyToTemp(PimixFile source, PimixFile temp) {
            if (temp.Exists()) {
                if (temp.CalculateInfo(FileProperties.Size).Size != temp.FileInfo.Size) {
                    logger.Info("Temp file exists but size is incorrect. Assuming incomplete Get result.");
                } else {
                    var tempCheckResult = temp.Add();

                    if (tempCheckResult == FileProperties.None) {
                        logger.Info("Temp file already got!");
                        return true;
                    }

                    logger.Warn("Temp exists, but doesn't match.");
                    return false;
                }
            }

            logger.Info("Copying from source {0} to temp {1}...", source, temp);
            source.Copy(temp);

            if (temp.Exists()) {
                logger.Info("Verifying temp {0}...", temp);
                var destinationCheckResult = temp.Add();
                if (destinationCheckResult == FileProperties.None) {
                    logger.Info("Successfully got temp {1} from {0}!", source, temp);
                    source.Register(true);
                    return true;
                }

                logger.Error("Get temp failed! The following fields differ (not removed): {0}",
                    destinationCheckResult);
                return false;
            }

            logger.Fatal("Temp doesn't exist unexpectedly!");
            return false;
        }
    }
}
