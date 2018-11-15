using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using NLog;
using Pimix.Api.Files;
using Pimix.IO;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("get", HelpText = "Get file.")]
    class GetCommand : FileUtilCommand {
        [Value(0, Required = true, MetaName = "File URL")]
        public string FileUri { get; set; }

        [Option('b', "use-baidu-cloud", HelpText = "Prefer baidu cloud storage first.")]
        public bool? PreferBaiduCloud { get; set; }

        [Option('l', "lightweight-only", HelpText = "Only get files that need no download.")]
        public bool LightweightOnly { get; set; } = false;

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public override int Execute() {
            var target = new PimixFile(FileUri);
            if (target.Client == null) {
                Console.WriteLine($"Target {FileUri} not accessible. Wrong server?");
                return 1;
            }

            var files = FileInformation.ListFolder(target.Id, true);
            if (files.Count > 0) {
                foreach (var file in files) {
                    Console.WriteLine(file);
                }

                Console.Write($"Confirm getting the {files.Count} files above?");
                Console.ReadLine();

                return files.Select(f => GetFile(new PimixFile(target.Host + f))).Max();
            }

            return GetFile(target);
        }

        int GetFile(PimixFile target) {
            if (target.Exists()) {
                if (target.CalculateInfo(FileProperties.Size).Size != target.FileInfo.Size) {
                    logger.Info(
                        "Target exists but size is incorrect. Assuming incomplete Get result.");
                } else {
                    var targetCheckResult = target.Add();

                    if (targetCheckResult == FileProperties.None) {
                        logger.Info("Already got!");
                        return 0;
                    }

                    logger.Warn("Target exists, but doesn't match.");
                    return 2;
                }
            }

            var info = target.FileInfo;

            if (info.Locations == null) {
                logger.Error($"No instance exists for {info.Id}!");
                return 1;
            }

            foreach (var location in info.Locations) {
                if (location.Value != null) {
                    var linkSource = new PimixFile(location.Key);
                    if (linkSource.Client is FileStorageClient && linkSource.IsCompatible(target) &&
                        linkSource.Exists()) {
                        linkSource.Copy(target);
                        target.Register(true);
                        logger.Info("Got {0} through hard linking to {1}.", target, linkSource);
                        return 0;
                    }
                }
            }

            if (LightweightOnly) {
                logger.Warn("Not getting {}, which requires downloading.", target);
                return 1;
            }

            if (PreferBaiduCloud.HasValue) {
                PimixFile.PreferBaiduCloud = PreferBaiduCloud.Value;
            }

            var source = new PimixFile(fileInfo: info);
            source.Copy(target);

            if (target.Exists()) {
                logger.Info("Verifying {0}...", target);
                var destinationCheckResult = target.Add();
                if (destinationCheckResult == FileProperties.None) {
                    logger.Info("Successfully got {1} from {0}!", source, target);
                    source.Register(true);
                    return 0;
                }

                logger.Error(
                    "Get failed! The following fields differ (not removed): {0}",
                    destinationCheckResult
                );
                return 2;
            }

            logger.Fatal("Destination doesn't exist unexpectedly!");
            return 2;
        }
    }
}
